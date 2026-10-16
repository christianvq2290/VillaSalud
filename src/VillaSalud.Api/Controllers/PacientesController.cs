using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/pacientes")]
[Authorize(Policy = Politicas.GestionCitas)]
public class PacientesController : ControllerBase
{
    private readonly PacienteService _pacientes;
    public PacientesController(PacienteService pacientes) => _pacientes = pacientes;

    [HttpGet]
    public async Task<ActionResult<List<PacienteDto>>> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await _pacientes.ListarAsync(busqueda, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PacienteDto>> Obtener(int id, CancellationToken ct) => Ok(await _pacientes.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<PacienteDto>> Crear(PacienteDto dto, CancellationToken ct)
    {
        var creado = await _pacientes.GuardarAsync(0, dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdPaciente }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PacienteDto>> Actualizar(int id, PacienteDto dto, CancellationToken ct) =>
        Ok(await _pacientes.GuardarAsync(id, dto, ct));
}
