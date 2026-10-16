using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IContextoUsuario _contexto;

    public AuthController(AuthService auth, IContextoUsuario contexto)
    {
        _auth = auth;
        _contexto = contexto;
    }

    /// <summary>Autentica al usuario y entrega el token de acceso (JWT) y el de renovación.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    [ProducesResponseType(typeof(SesionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SesionDto>> Login(LoginDto dto, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(dto, ct));

    /// <summary>Canjea el token de renovación por una sesión nueva.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<ActionResult<SesionDto>> Refrescar(RefrescarDto dto, CancellationToken ct) =>
        Ok(await _auth.RefrescarAsync(dto.RefreshToken, ct));

    [HttpPost("cambiar-clave")]
    [Authorize]
    public async Task<IActionResult> CambiarClave(CambiarClaveDto dto, CancellationToken ct)
    {
        await _auth.CambiarClaveAsync(_contexto.IdUsuario!.Value, dto, ct);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _auth.CerrarSesionAsync(_contexto.IdUsuario!.Value, ct);
        return NoContent();
    }
}
