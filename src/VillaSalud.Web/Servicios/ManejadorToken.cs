using System.Net.Http.Headers;

namespace VillaSalud.Web.Servicios;

/// <summary>Agrega a cada llamada a la API el token JWT del usuario y la IP del navegador (para la auditoría).</summary>
public class ManejadorToken : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ServicioSesion _sesion;

    public ManejadorToken(IHttpContextAccessor accessor, ServicioSesion sesion)
    {
        _accessor = accessor;
        _sesion = sesion;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var contexto = _accessor.HttpContext;
        if (contexto is not null)
        {
            var token = await _sesion.ObtenerTokenVigenteAsync(contexto, cancellationToken);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ip = contexto.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ip)) request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
