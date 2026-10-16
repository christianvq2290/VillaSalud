using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/medicos")]
[Authorize]
public class MedicosController : ControllerBase
{
    private readonly MedicoService _medicos;
    public MedicosController(MedicoService medicos) => _medicos = medicos;

    [HttpGet]
    public async Task<ActionResult<List<MedicoDto>>> Listar([FromQuery] bool soloActivos = false, CancellationToken ct = default) =>
        Ok(await _medicos.ListarAsync(soloActivos, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedicoDto>> Obtener(int id, CancellationToken ct) => Ok(await _medicos.ObtenerAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<MedicoDto>> Crear(MedicoDto dto, CancellationToken ct)
    {
        var creado = await _medicos.GuardarAsync(0, dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.IdMedico }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<MedicoDto>> Actualizar(int id, MedicoDto dto, CancellationToken ct) =>
        Ok(await _medicos.GuardarAsync(id, dto, ct));

    // ---------- Horarios de atención ----------
    [HttpGet("{id:int}/horarios")]
    public async Task<ActionResult<List<HorarioDto>>> Horarios(int id, CancellationToken ct) => Ok(await _medicos.ListarHorariosAsync(id, ct));

    [HttpPost("{id:int}/horarios")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<HorarioDto>> AgregarHorario(int id, HorarioDto dto, CancellationToken ct)
    {
        var creado = await _medicos.AgregarHorarioAsync(id, dto, ct);
        return CreatedAtAction(nameof(Horarios), new { id }, creado);
    }

    [HttpDelete("{id:int}/horarios/{idHorario:int}")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<IActionResult> QuitarHorario(int id, int idHorario, CancellationToken ct)
    {
        await _medicos.QuitarHorarioAsync(id, idHorario, ct);
        return NoContent();
    }

    /// <summary>Intervalos del médico en la fecha indicada, con su disponibilidad.</summary>
    [HttpGet("{id:int}/disponibilidad")]
    public async Task<ActionResult<List<IntervaloDto>>> Disponibilidad(int id, [FromQuery] DateOnly fecha, CancellationToken ct) =>
        Ok(await _medicos.DisponibilidadAsync(id, fecha, ct));
}
