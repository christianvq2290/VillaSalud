using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Policy = Politicas.SoloAdmin)]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarios;
    public UsuariosController(UsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(CancellationToken ct) => Ok(await _usuarios.ListarAsync(ct));

    [HttpGet("roles")]
    public async Task<ActionResult<List<RolDto>>> Roles(CancellationToken ct) => Ok(await _usuarios.ListarRolesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id, CancellationToken ct) => Ok(await _usuarios.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(UsuarioGuardarDto dto, CancellationToken ct)
    {
        var creado = await _usuarios.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdUsuario }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Actualizar(int id, UsuarioGuardarDto dto, CancellationToken ct) =>
        Ok(await _usuarios.ActualizarAsync(id, dto, ct));

    [HttpPost("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id, CancellationToken ct)
    {
        await _usuarios.DesbloquearAsync(id, ct);
        return NoContent();
    }
}
