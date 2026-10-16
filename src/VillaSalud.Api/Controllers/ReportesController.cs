using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VillaSalud.Api.Seguridad;
using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Controllers;

[ApiController]
[Authorize(Policy = Politicas.GestionCitas)]
public class ReportesController : ControllerBase
{
    private readonly ReporteService _reportes;
    private readonly IAuditoriaService _auditoria;

    public ReportesController(ReporteService reportes, IAuditoriaService auditoria)
    {
        _reportes = reportes;
        _auditoria = auditoria;
    }

    /// <summary>Indicadores del tablero para el periodo (por defecto, el mes en curso).</summary>
    [HttpGet("api/v1/dashboard/indicadores")]
    public async Task<ActionResult<IndicadoresDto>> Indicadores([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Ok(await _reportes.IndicadoresAsync(desde, hasta, ct));

    [HttpGet("api/v1/reportes/citas")]
    public async Task<ActionResult<ReporteCitasDto>> Citas([FromQuery] FiltroCitasDto filtro, CancellationToken ct) =>
        Ok(await _reportes.CitasPorPeriodoAsync(filtro, ct));

    /// <summary>Descarga el reporte de citas. formato: pdf, xlsx o csv.</summary>
    [HttpGet("api/v1/reportes/citas/exportar")]
    public async Task<IActionResult> Exportar([FromQuery] FiltroCitasDto filtro, [FromQuery] string formato = "xlsx", CancellationToken ct = default)
    {
        var archivo = await _reportes.ExportarAsync(filtro, formato, ct);
        return File(archivo.Contenido, archivo.Mime, archivo.Nombre);
    }

    [HttpGet("api/v1/auditoria")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    public async Task<ActionResult<List<AuditoriaDto>>> Auditoria([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Ok(await _auditoria.ListarAsync(desde, hasta, ct));
}
