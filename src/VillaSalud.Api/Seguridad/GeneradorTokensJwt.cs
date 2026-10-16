using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Api.Seguridad;

public class OpcionesJwt
{
    public const int LongitudMinimaClave = 32;

    public string Issuer { get; set; } = "villasalud-api";
    public string Audience { get; set; } = "villasalud-clientes";
    public int MinutosVigencia { get; set; } = 30;
    /// <summary>Clave de firma HMAC-SHA256. Nunca va en el repositorio: user-secrets en desarrollo, Key Vault en producción.</summary>
    public string Key { get; set; } = "";

    public byte[] ClaveEnBytes()
    {
        if (string.IsNullOrWhiteSpace(Key) || Key.Length < LongitudMinimaClave)
            throw new InvalidOperationException(
                $"Falta la clave de firma JWT (Jwt:Key) o tiene menos de {LongitudMinimaClave} caracteres. " +
                "En desarrollo: dotnet user-secrets set \"Jwt:Key\" \"<clave aleatoria>\" --project src/VillaSalud.Api");
        return Encoding.UTF8.GetBytes(Key);
    }
}

public class GeneradorTokensJwt : IGeneradorTokens
{
    private readonly OpcionesJwt _opciones;

    public GeneradorTokensJwt(IOptions<OpcionesJwt> opciones)
        => _opciones = opciones.Value;

    public TokenAcceso CrearTokenAcceso(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimsToken.Id, usuario.IdUsuario.ToString()),
            new(ClaimsToken.Nombre, usuario.Username),
            new(ClaimsToken.Rol, usuario.Rol?.Nombre ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (usuario.IdSede.HasValue) claims.Add(new Claim(ClaimsToken.Sede, usuario.IdSede.Value.ToString()));
        if (usuario.IdMedico.HasValue) claims.Add(new Claim(ClaimsToken.Medico, usuario.IdMedico.Value.ToString()));

        var ahora = DateTime.UtcNow;
        var expira = ahora.AddMinutes(_opciones.MinutosVigencia);
        var credenciales = new SigningCredentials(new SymmetricSecurityKey(_opciones.ClaveEnBytes()), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opciones.Issuer, _opciones.Audience, claims, ahora, expira, credenciales);
        return new TokenAcceso(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
