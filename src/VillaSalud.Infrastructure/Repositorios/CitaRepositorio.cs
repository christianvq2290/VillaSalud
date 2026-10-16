using Microsoft.EntityFrameworkCore;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Persistencia;

namespace VillaSalud.Infrastructure.Repositorios;

public class CitaRepositorio : ICitaRepositorio
{
    private readonly VillaSaludDbContext _db;
    public CitaRepositorio(VillaSaludDbContext db) => _db = db;

    private IQueryable<Cita> ConDatos() =>
        _db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Medico!).ThenInclude(m => m.Especialidad)
            .Include(c => c.Consultorio)
            .Include(c => c.Sede);

    public Task<Cita?> ObtenerAsync(int id, CancellationToken ct = default) =>
        ConDatos().Include(c => c.Historial).ThenInclude(h => h.Usuario).FirstOrDefaultAsync(c => c.IdCita == id, ct);

    public Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        ConDatos().AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo, ct);

    public Task<List<Cita>> ListarAsync(FiltroCitas f, CancellationToken ct = default)
    {
        var consulta = ConDatos().AsNoTracking();
        if (f.Desde.HasValue) consulta = consulta.Where(c => c.Fecha >= f.Desde.Value);
        if (f.Hasta.HasValue) consulta = consulta.Where(c => c.Fecha <= f.Hasta.Value);
        if (f.IdSede.HasValue) consulta = consulta.Where(c => c.IdSede == f.IdSede.Value);
        if (f.IdMedico.HasValue) consulta = consulta.Where(c => c.IdMedico == f.IdMedico.Value);
        if (f.IdEspecialidad.HasValue) consulta = consulta.Where(c => c.Medico!.IdEspecialidad == f.IdEspecialidad.Value);
        if (f.IdEstadoCita.HasValue) consulta = consulta.Where(c => c.IdEstadoCita == f.IdEstadoCita.Value);
        if (f.IdPaciente.HasValue) consulta = consulta.Where(c => c.IdPaciente == f.IdPaciente.Value);
        return consulta.OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).ToListAsync(ct);
    }

    public Task<bool> ExisteCruceAsync(int idMedico, int idConsultorio, int idPaciente, DateOnly fecha, TimeOnly inicio, TimeOnly fin, int? excluirIdCita, CancellationToken ct = default)
    {
        var excluir = excluirIdCita ?? 0;
        return _db.Citas.AnyAsync(c =>
            c.IdCita != excluir &&
            c.Fecha == fecha &&
            c.IdEstadoCita != EstadosCita.Cancelada &&
            (c.IdMedico == idMedico || c.IdConsultorio == idConsultorio || c.IdPaciente == idPaciente) &&
            c.HoraInicio < fin && inicio < c.HoraFin, ct);
    }

    public async Task<List<Cita>> ListarOcupadasAsync(int idMedico, IReadOnlyCollection<int> idsConsultorio, DateOnly fecha, CancellationToken ct = default)
    {
        // Se filtra por fecha en SQL y por médico o consultorio en memoria: son pocas filas por día
        var delDia = await _db.Citas.AsNoTracking()
            .Where(c => c.Fecha == fecha && c.IdEstadoCita != EstadosCita.Cancelada).ToListAsync(ct);
        return delDia.Where(c => c.IdMedico == idMedico || idsConsultorio.Contains(c.IdConsultorio)).ToList();
    }

    public void Agregar(Cita cita) => _db.Citas.Add(cita);

    public void AgregarNotificacion(Notificacion notificacion) => _db.Notificaciones.Add(notificacion);

    public Task<List<Notificacion>> ListarNotificacionesPendientesAsync(DateTime hasta, int maximo, CancellationToken ct = default) =>
        _db.Notificaciones
            .Include(n => n.Cita!).ThenInclude(c => c.Paciente)
            .Include(n => n.Cita!).ThenInclude(c => c.Medico)
            .Include(n => n.Cita!).ThenInclude(c => c.Consultorio)
            .Include(n => n.Cita!).ThenInclude(c => c.Sede)
            .Where(n => n.EstadoEnvio == Notificacion.EstadoPendiente && n.FechaProgramada <= hasta)
            .OrderBy(n => n.FechaProgramada).Take(maximo).ToListAsync(ct);

    public Task<List<Cita>> ListarCitasSinRecordatorioAsync(DateOnly fecha, CancellationToken ct = default) =>
        _db.Citas.AsNoTracking().Include(c => c.Paciente)
            .Where(c => c.Fecha == fecha
                        && (c.IdEstadoCita == EstadosCita.Pendiente || c.IdEstadoCita == EstadosCita.Reprogramada)
                        && c.Paciente!.Email != null && c.Paciente.Email != ""
                        && !_db.Notificaciones.Any(n => n.IdCita == c.IdCita && n.Tipo == Notificacion.TipoRecordatorio))
            .ToListAsync(ct);
}

public class AuditoriaRepositorio : IAuditoriaRepositorio
{
    private readonly VillaSaludDbContext _db;
    public AuditoriaRepositorio(VillaSaludDbContext db) => _db = db;

    public void Agregar(Auditoria registro) => _db.Auditorias.Add(registro);

    public Task<List<Auditoria>> ListarAsync(DateTime desde, DateTime hasta, int maximo, CancellationToken ct = default) =>
        _db.Auditorias.AsNoTracking().Include(a => a.Usuario)
            .Where(a => a.Fecha >= desde && a.Fecha < hasta)
            .OrderByDescending(a => a.Fecha).Take(maximo).ToListAsync(ct);
}
