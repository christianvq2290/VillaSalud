using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Route("api/v1/citas")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly CitaService _citas;
    public CitasController(CitaService citas) => _citas = citas;

    /// <summary>Consulta de citas. El resultado se limita a la sede del usuario y, si es médico, a sus propias citas.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> Listar([FromQuery] FiltroCitasDto filtro, CancellationToken ct) =>
        Ok(await _citas.ListarAsync(filtro, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CitaDetalleDto>> Obtener(int id, CancellationToken ct) => Ok(await _citas.ObtenerAsync(id, ct));

    /// <summary>Programa una cita. Responde 409 si el intervalo ya fue tomado.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.GestionCitas)]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CitaDto>> Programar(ProgramarCitaDto dto, CancellationToken ct)
    {
        var cita = await _citas.ProgramarAsync(dto, ct);
        return CreatedAtAction(nameof(Obtener), new { id = cita.IdCita }, cita);
    }

    [HttpPut("{id:int}/reprogramar")]
    [Authorize(Policy = Politicas.GestionCitas)]
    public async Task<ActionResult<CitaDto>> Reprogramar(int id, ReprogramarCitaDto dto, CancellationToken ct) =>
        Ok(await _citas.ReprogramarAsync(id, dto, ct));

    [HttpPut("{id:int}/cancelar")]
    [Authorize(Policy = Politicas.GestionCitas)]
    public async Task<ActionResult<CitaDto>> Cancelar(int id, CancelarCitaDto dto, CancellationToken ct) =>
        Ok(await _citas.CancelarAsync(id, dto, ct));

    /// <summary>Registra la atención o la inasistencia.</summary>
    [HttpPut("{id:int}/atender")]
    [Authorize(Policy = Politicas.Agenda)]
    public async Task<ActionResult<CitaDto>> Atender(int id, AtenderCitaDto dto, CancellationToken ct) =>
        Ok(await _citas.RegistrarAtencionAsync(id, dto, ct));

    /// <summary>Agenda de un médico para una fecha. Un usuario médico siempre recibe la suya.</summary>
    [HttpGet("/api/v1/agenda")]
    [Authorize(Policy = Politicas.Agenda)]
    public async Task<ActionResult<List<CitaDto>>> Agenda([FromQuery] DateOnly fecha, [FromQuery] int? idMedico, CancellationToken ct) =>
        Ok(await _citas.AgendaAsync(fecha, idMedico, ct));
}
