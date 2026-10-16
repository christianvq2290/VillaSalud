using VillaSalud.Application.Abstracciones;

namespace VillaSalud.Api.Seguridad;

/// <summary>Lee del token JWT quién ejecuta la petición. Las reglas de sede (RN01) se aplican con estos datos, no con lo que envíe el cliente.</summary>
public class ContextoUsuarioHttp : IContextoUsuario
{
    private readonly IHttpContextAccessor _accessor;

    public ContextoUsuarioHttp(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? IdUsuario => Entero(ClaimsToken.Id);
    public string? Rol => _accessor.HttpContext?.User.FindFirst(ClaimsToken.Rol)?.Value;
    public int? IdSede => Entero(ClaimsToken.Sede);
    public int? IdMedico => Entero(ClaimsToken.Medico);

    public string? Ip
    {
        get
        {
            var contexto = _accessor.HttpContext;
            if (contexto is null) return null;
            // El cliente web reenvía la IP del navegador en X-Forwarded-For; se usa solo como dato de auditoría
            var reenviada = contexto.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
            var ip = string.IsNullOrEmpty(reenviada) ? contexto.Connection.RemoteIpAddress?.ToString() : reenviada;
            return ip is { Length: > 45 } ? ip[..45] : ip;
        }
    }

    private int? Entero(string claim) =>
        int.TryParse(_accessor.HttpContext?.User.FindFirst(claim)?.Value, out var valor) ? valor : null;
}
