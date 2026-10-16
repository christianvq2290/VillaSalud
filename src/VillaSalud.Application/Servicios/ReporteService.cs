using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

/// <summary>Indicadores del tablero y reportes exportables.</summary>
public class ReporteService
{
    public const int MaximoDias = 366;

    private readonly ICitaRepositorio _citas;
    private readonly CitaService _citaService;
    private readonly IEnumerable<IExportadorReporte> _exportadores;
    private readonly IReloj _reloj;
    private readonly IAuditoriaService _auditoria;
    private readonly IUnitOfWork _uow;

    public ReporteService(ICitaRepositorio citas, CitaService citaService, IEnumerable<IExportadorReporte> exportadores,
        IReloj reloj, IAuditoriaService auditoria, IUnitOfWork uow)
    {
        _citas = citas;
        _citaService = citaService;
        _exportadores = exportadores;
        _reloj = reloj;
        _auditoria = auditoria;
        _uow = uow;
    }

    public async Task<IndicadoresDto> IndicadoresAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(_reloj.Ahora);
        var (inicio, fin) = NormalizarPeriodo(desde, hasta, hoy);
        var citas = await _citas.ListarAsync(_citaService.AplicarAlcance(new FiltroCitasDto { Desde = inicio, Hasta = fin }), ct);

        var atendidas = citas.Count(c => c.IdEstadoCita == EstadosCita.Atendida);
        var inasistencias = citas.Count(c => c.IdEstadoCita == EstadosCita.NoAsistio);
        var cerradas = atendidas + inasistencias;

        return new IndicadoresDto
        {
            Desde = inicio,
            Hasta = fin,
            TotalCitas = citas.Count,
            Pendientes = citas.Count(c => c.EstaActiva),
            Atendidas = atendidas,
            Canceladas = citas.Count(c => c.IdEstadoCita == EstadosCita.Cancelada),
            NoAsistio = inasistencias,
            CitasHoy = citas.Count(c => c.Fecha == hoy && c.IdEstadoCita != EstadosCita.Cancelada),
            TasaInasistencia = cerradas == 0 ? 0 : Math.Round(inasistencias * 100m / cerradas, 1),
            PorEstado = citas.GroupBy(c => c.IdEstadoCita).OrderBy(g => g.Key)
                .Select(g => new SerieDto { Etiqueta = EstadosCita.Nombre(g.Key), Valor = g.Count() }).ToList(),
            PorEspecialidad = citas.GroupBy(c => c.Medico?.Especialidad?.Nombre ?? "Sin especialidad")
                .OrderByDescending(g => g.Count()).Take(8)
                .Select(g => new SerieDto { Etiqueta = g.Key, Valor = g.Count() }).ToList(),
            PorDia = citas.GroupBy(c => c.Fecha).OrderBy(g => g.Key)
                .Select(g => new SerieDto { Etiqueta = g.Key.ToString("dd/MM"), Valor = g.Count() }).ToList(),
            PorConsultorio = citas.Where(c => c.IdEstadoCita != EstadosCita.Cancelada)
                .GroupBy(c => $"{c.Consultorio?.Numero} ({c.Sede?.Nombre})").OrderByDescending(g => g.Count()).Take(8)
                .Select(g => new SerieDto { Etiqueta = g.Key, Valor = g.Count() }).ToList()
        };
    }

    public async Task<ReporteCitasDto> CitasPorPeriodoAsync(FiltroCitasDto filtro, CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(_reloj.Ahora);
        var (inicio, fin) = NormalizarPeriodo(filtro.Desde, filtro.Hasta, hoy);
        filtro.Desde = inicio;
        filtro.Hasta = fin;

        var citas = (await _citas.ListarAsync(_citaService.AplicarAlcance(filtro), ct))
            .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).ToList();

        return new ReporteCitasDto
        {
            Titulo = "Reporte de citas médicas – Clínica Villa Salud",
            Desde = inicio,
            Hasta = fin,
            GeneradoEl = _reloj.Ahora,
            Filas = citas.Select(c => new FilaReporteDto
            {
                Fecha = c.Fecha, HoraInicio = c.HoraInicio, HoraFin = c.HoraFin, Codigo = c.Codigo,
                Paciente = c.Paciente?.NombreCompleto ?? "", Dni = c.Paciente?.Dni ?? "",
                Medico = c.Medico?.NombreCompleto ?? "", Especialidad = c.Medico?.Especialidad?.Nombre ?? "",
                Consultorio = c.Consultorio?.Numero ?? "", Sede = c.Sede?.Nombre ?? "", Estado = EstadosCita.Nombre(c.IdEstadoCita)
            }).ToList(),
            Resumen = citas.GroupBy(c => new { Medico = c.Medico?.NombreCompleto ?? "", Especialidad = c.Medico?.Especialidad?.Nombre ?? "" })
                .OrderBy(g => g.Key.Medico)
                .Select(g => new ResumenMedicoDto
                {
                    Medico = g.Key.Medico, Especialidad = g.Key.Especialidad, Total = g.Count(),
                    Atendidas = g.Count(c => c.IdEstadoCita == EstadosCita.Atendida),
                    Canceladas = g.Count(c => c.IdEstadoCita == EstadosCita.Cancelada),
                    NoAsistio = g.Count(c => c.IdEstadoCita == EstadosCita.NoAsistio)
                }).ToList()
        };
    }

    public async Task<ArchivoDto> ExportarAsync(FiltroCitasDto filtro, string? formato, CancellationToken ct = default)
    {
        var clave = (formato ?? "xlsx").Trim().ToLowerInvariant();
        var exportador = _exportadores.FirstOrDefault(e => e.Formato == clave)
                         ?? throw new ReglaNegocioException("Formato no soportado. Use pdf, xlsx o csv.");

        var reporte = await CitasPorPeriodoAsync(filtro, ct);
        var contenido = exportador.Generar(reporte);

        _auditoria.Registrar("EXPORTAR", "REPORTE_CITAS", null, new { formato = clave, reporte.Desde, reporte.Hasta, filas = reporte.Filas.Count });
        await _uow.GuardarAsync(ct);

        return new ArchivoDto
        {
            Contenido = contenido,
            Mime = exportador.Mime,
            Nombre = $"citas_{reporte.Desde:yyyyMMdd}_{reporte.Hasta:yyyyMMdd}.{clave}"
        };
    }

    private static (DateOnly Inicio, DateOnly Fin) NormalizarPeriodo(DateOnly? desde, DateOnly? hasta, DateOnly hoy)
    {
        var inicio = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = hasta ?? inicio.AddMonths(1).AddDays(-1);
        if (fin < inicio) throw new ReglaNegocioException("La fecha final no puede ser anterior a la inicial.");
        if (fin.DayNumber - inicio.DayNumber > MaximoDias) throw new ReglaNegocioException("El periodo no puede superar los 12 meses.");
        return (inicio, fin);
    }
}
