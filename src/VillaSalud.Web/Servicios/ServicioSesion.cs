using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VillaSalud.Application.Dtos;

namespace VillaSalud.Web.Servicios;

/// <summary>
/// Sesión del cliente web. Los tokens de la API viajan dentro de la cookie de autenticación
/// (cifrada y HttpOnly): el navegador nunca los ve ni los puede leer un script.
/// </summary>
public class ServicioSesion
{
    public const string ClienteSinToken = "api-sin-token";
    public const string ClaimSede = "sede";
    public const string ClaimSedeNombre = "sedeNombre";
    public const string ClaimMedico = "medico";
    public const string ClaimCambiarClave = "debeCambiarClave";

    private const string TokenAcceso = "access_token";
    private const string TokenRenovacion = "refresh_token";
    private const string TokenExpira = "expires_at";
    private const string ItemSesionRenovada = "villasalud.sesion.renovada";

    private readonly IHttpClientFactory _fabrica;
    private readonly ILogger<ServicioSesion> _logger;

    public ServicioSesion(IHttpClientFactory fabrica, ILogger<ServicioSesion> logger)
    {
        _fabrica = fabrica;
        _logger = logger;
    }

    public async Task IniciarAsync(HttpContext contexto, SesionDto sesion)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.IdUsuario.ToString()),
            new(ClaimTypes.Name, sesion.Username),
            new(ClaimTypes.Role, sesion.Rol),
            new(ClaimCambiarClave, sesion.DebeCambiarClave ? "1" : "0")
        };
        if (sesion.IdSede.HasValue) claims.Add(new Claim(ClaimSede, sesion.IdSede.Value.ToString()));
        if (!string.IsNullOrEmpty(sesion.Sede)) claims.Add(new Claim(ClaimSedeNombre, sesion.Sede));
        if (sesion.IdMedico.HasValue) claims.Add(new Claim(ClaimMedico, sesion.IdMedico.Value.ToString()));

        var propiedades = new AuthenticationProperties { AllowRefresh = true };
        propiedades.StoreTokens(new[]
        {
            new AuthenticationToken { Name = TokenAcceso, Value = sesion.TokenAcceso },
            new AuthenticationToken { Name = TokenRenovacion, Value = sesion.RefreshToken },
            new AuthenticationToken { Name = TokenExpira, Value = sesion.ExpiraUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) }
        });

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad), propiedades);
        contexto.Items[ItemSesionRenovada] = sesion;
    }

    public Task CerrarAsync(HttpContext contexto) => contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>
    /// Devuelve el token de acceso de la sesión. Si está por vencer, lo renueva con el token de renovación
    /// y actualiza la cookie, de modo que el usuario no tenga que volver a ingresar cada 30 minutos.
    /// </summary>
    public async Task<string?> ObtenerTokenVigenteAsync(HttpContext contexto, CancellationToken ct)
    {
        if (contexto.Items[ItemSesionRenovada] is SesionDto renovada) return renovada.TokenAcceso;

        var token = await contexto.GetTokenAsync(TokenAcceso);
        if (string.IsNullOrEmpty(token)) return null;

        var expira = await contexto.GetTokenAsync(TokenExpira);
        var vigente = DateTime.TryParse(expira, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fecha)
                      && fecha.ToUniversalTime() > DateTime.UtcNow.AddSeconds(60);
        if (vigente) return token;

        var renovacion = await contexto.GetTokenAsync(TokenRenovacion);
        if (string.IsNullOrEmpty(renovacion) || contexto.Response.HasStarted) return token;

        try
        {
            var cliente = _fabrica.CreateClient(ClienteSinToken);
            using var respuesta = await cliente.PostAsJsonAsync("api/v1/auth/refresh", new RefrescarDto { RefreshToken = renovacion }, ct);
            if (!respuesta.IsSuccessStatusCode) return token;       // la API responderá 401 y se pedirá ingresar de nuevo
            var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>(ApiCliente.OpcionesJson, ct);
            if (sesion is null) return token;
            await IniciarAsync(contexto, sesion);
            return sesion.TokenAcceso;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo renovar la sesión contra la API");
            return token;
        }
    }
}
