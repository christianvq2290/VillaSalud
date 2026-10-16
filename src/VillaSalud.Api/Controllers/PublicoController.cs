using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

/// <summary>
/// Servicios de consulta para el asistente virtual (IBM watsonx Assistant). No usan JWT: se protegen con
/// una clave de API y con límite de peticiones, y solo devuelven datos mínimos (RN10).
/// </summary>
[ApiController]
[Route("api/v1/publico")]
[AllowAnonymous]
[ApiKey]
[EnableRateLimiting(Politicas.LimitePublico)]
public class PublicoController : ControllerBase
{
    private readonly CatalogoService _catalogo;
    private readonly MedicoService _medicos;
    private readonly CitaService _citas;

    public PublicoController(CatalogoService catalogo, MedicoService medicos, CitaService citas)
    {
        _catalogo = catalogo;
        _medicos = medicos;
        _citas = citas;
    }

    [HttpGet("sedes")]
    public async Task<ActionResult<List<SedePublicaDto>>> Sedes(CancellationToken ct) =>
        Ok((await _catalogo.ListarSedesAsync(ct)).Where(s => s.Estado)
            .Select(s => new SedePublicaDto { Nombre = s.Nombre, Direccion = s.Direccion, Distrito = s.Distrito }).ToList());

    [HttpGet("especialidades")]
    public async Task<ActionResult<List<EspecialidadPublicaDto>>> Especialidades(CancellationToken ct) =>
        Ok((await _medicos.ListarAsync(soloActivos: true, ct))
            .GroupBy(m => m.Especialidad ?? "General").OrderBy(g => g.Key)
            .Select(g => new EspecialidadPublicaDto { Especialidad = g.Key, Medicos = g.Select(m => m.NombreCompleto).OrderBy(n => n).ToList() })
            .ToList());

    /// <summary>Estado de una cita. Responde 404 con el mismo mensaje tanto si el código no existe como si el DNI no coincide.</summary>
    [HttpGet("citas/estado")]
    [ProducesResponseType(typeof(EstadoCitaPublicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoCitaPublicoDto>> EstadoCita([FromQuery] string dni, [FromQuery] string codigo, CancellationToken ct)
    {
        var estado = await _citas.EstadoPublicoAsync(dni, codigo, ct);
        if (estado is null)
            return NotFound(new ProblemDetails { Status = 404, Title = "No encontrado", Detail = "No se encontró una cita con esos datos." });
        return Ok(estado);
    }
}
