using Microsoft.EntityFrameworkCore;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Persistencia;

namespace VillaSalud.Infrastructure.Repositorios;

public class PacienteRepositorio : IPacienteRepositorio
{
    private const int MaximoResultados = 300;

    private readonly VillaSaludDbContext _db;
    public PacienteRepositorio(VillaSaludDbContext db) => _db = db;

    public Task<List<Paciente>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        _db.Pacientes.AsNoTracking().Include(p => p.Consentimientos)
            .Where(p => busqueda == null || p.Dni.Contains(busqueda) || p.Nombres.Contains(busqueda) || p.Apellidos.Contains(busqueda))
            .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).Take(MaximoResultados).ToListAsync(ct);

    public Task<Paciente?> ObtenerAsync(int id, CancellationToken ct = default) =>
        _db.Pacientes.Include(p => p.Consentimientos).FirstOrDefaultAsync(p => p.IdPaciente == id, ct);

    public Task<Paciente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default) =>
        _db.Pacientes.Include(p => p.Consentimientos).FirstOrDefaultAsync(p => p.Dni == dni, ct);

    public Task<bool> ExisteDniAsync(string dni, int? excluirId, CancellationToken ct = default) =>
        _db.Pacientes.AnyAsync(p => p.Dni == dni && (excluirId == null || p.IdPaciente != excluirId), ct);

    public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default) =>
        _db.Pacientes.AnyAsync(p => p.Email == email && (excluirId == null || p.IdPaciente != excluirId), ct);

    public void Agregar(Paciente paciente) => _db.Pacientes.Add(paciente);
}
