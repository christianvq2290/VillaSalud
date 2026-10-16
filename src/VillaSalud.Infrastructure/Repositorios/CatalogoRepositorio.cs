using Microsoft.EntityFrameworkCore;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Persistencia;

namespace VillaSalud.Infrastructure.Repositorios;

public class CatalogoRepositorio : ICatalogoRepositorio
{
    private readonly VillaSaludDbContext _db;
    public CatalogoRepositorio(VillaSaludDbContext db) => _db = db;

    public Task<List<Sede>> ListarSedesAsync(CancellationToken ct = default) =>
        _db.Sedes.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(ct);

    public Task<Sede?> ObtenerSedeAsync(int id, CancellationToken ct = default) =>
        _db.Sedes.FirstOrDefaultAsync(s => s.IdSede == id, ct);

    public Task<bool> ExisteSedeAsync(string nombre, int? excluirId, CancellationToken ct = default) =>
        _db.Sedes.AnyAsync(s => s.Nombre == nombre && (excluirId == null || s.IdSede != excluirId), ct);

    public void AgregarSede(Sede sede) => _db.Sedes.Add(sede);

    public Task<List<Especialidad>> ListarEspecialidadesAsync(CancellationToken ct = default) =>
        _db.Especialidades.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync(ct);

    public Task<Especialidad?> ObtenerEspecialidadAsync(int id, CancellationToken ct = default) =>
        _db.Especialidades.FirstOrDefaultAsync(e => e.IdEspecialidad == id, ct);

    public Task<bool> ExisteEspecialidadAsync(string nombre, int? excluirId, CancellationToken ct = default) =>
        _db.Especialidades.AnyAsync(e => e.Nombre == nombre && (excluirId == null || e.IdEspecialidad != excluirId), ct);

    public Task<bool> EspecialidadTieneMedicosAsync(int id, CancellationToken ct = default) =>
        _db.Medicos.AnyAsync(m => m.IdEspecialidad == id, ct);

    public void AgregarEspecialidad(Especialidad especialidad) => _db.Especialidades.Add(especialidad);
    public void EliminarEspecialidad(Especialidad especialidad) => _db.Especialidades.Remove(especialidad);

    public Task<List<Consultorio>> ListarConsultoriosAsync(int? idSede, CancellationToken ct = default) =>
        _db.Consultorios.AsNoTracking().Include(c => c.Sede)
            .Where(c => idSede == null || c.IdSede == idSede)
            .OrderBy(c => c.IdSede).ThenBy(c => c.Numero).ToListAsync(ct);

    public Task<Consultorio?> ObtenerConsultorioAsync(int id, CancellationToken ct = default) =>
        _db.Consultorios.Include(c => c.Sede).FirstOrDefaultAsync(c => c.IdConsultorio == id, ct);

    public Task<bool> ExisteConsultorioAsync(int idSede, string numero, int? excluirId, CancellationToken ct = default) =>
        _db.Consultorios.AnyAsync(c => c.IdSede == idSede && c.Numero == numero && (excluirId == null || c.IdConsultorio != excluirId), ct);

    public void AgregarConsultorio(Consultorio consultorio) => _db.Consultorios.Add(consultorio);
}
