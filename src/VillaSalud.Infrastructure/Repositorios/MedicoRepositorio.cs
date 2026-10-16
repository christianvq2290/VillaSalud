using Microsoft.EntityFrameworkCore;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Persistencia;

namespace VillaSalud.Infrastructure.Repositorios;

public class MedicoRepositorio : IMedicoRepositorio
{
    private readonly VillaSaludDbContext _db;
    public MedicoRepositorio(VillaSaludDbContext db) => _db = db;

    public Task<List<Medico>> ListarAsync(bool soloActivos, CancellationToken ct = default) =>
        _db.Medicos.AsNoTracking().Include(m => m.Especialidad)
            .Where(m => !soloActivos || m.Estado)
            .OrderBy(m => m.Apellidos).ThenBy(m => m.Nombres).ToListAsync(ct);

    public Task<Medico?> ObtenerAsync(int id, CancellationToken ct = default) =>
        _db.Medicos.Include(m => m.Especialidad).FirstOrDefaultAsync(m => m.IdMedico == id, ct);

    public Task<bool> ExisteColegiaturaAsync(string codigo, int? excluirId, CancellationToken ct = default) =>
        _db.Medicos.AnyAsync(m => m.CodigoColegiatura == codigo && (excluirId == null || m.IdMedico != excluirId), ct);

    public void Agregar(Medico medico) => _db.Medicos.Add(medico);

    public Task<List<HorarioMedico>> ListarHorariosAsync(int idMedico, CancellationToken ct = default) =>
        _db.Horarios.Include(h => h.Consultorio!).ThenInclude(c => c.Sede)
            .Where(h => h.IdMedico == idMedico && h.Vigente).ToListAsync(ct);

    public Task<List<HorarioMedico>> ListarHorariosDeConsultorioAsync(int idConsultorio, byte diaSemana, CancellationToken ct = default) =>
        _db.Horarios.AsNoTracking()
            .Where(h => h.IdConsultorio == idConsultorio && h.DiaSemana == diaSemana && h.Vigente).ToListAsync(ct);

    public Task<HorarioMedico?> ObtenerHorarioAsync(int idHorario, CancellationToken ct = default) =>
        _db.Horarios.FirstOrDefaultAsync(h => h.IdHorario == idHorario, ct);

    public void AgregarHorario(HorarioMedico horario) => _db.Horarios.Add(horario);
}
