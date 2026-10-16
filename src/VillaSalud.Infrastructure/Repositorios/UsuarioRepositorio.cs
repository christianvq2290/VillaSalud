using Microsoft.EntityFrameworkCore;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Persistencia;

namespace VillaSalud.Infrastructure.Repositorios;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly VillaSaludDbContext _db;
    public UsuarioRepositorio(VillaSaludDbContext db) => _db = db;

    private IQueryable<Usuario> ConDatos() => _db.Usuarios.Include(u => u.Rol).Include(u => u.Sede).Include(u => u.Medico);

    public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct = default) =>
        ConDatos().FirstOrDefaultAsync(u => u.IdUsuario == id, ct);

    public Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default) =>
        ConDatos().FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default) =>
        ConDatos().AsNoTracking().OrderBy(u => u.Username).ToListAsync(ct);

    public Task<bool> ExisteUsernameAsync(string username, int? excluirId, CancellationToken ct = default) =>
        _db.Usuarios.AnyAsync(u => u.Username == username && (excluirId == null || u.IdUsuario != excluirId), ct);

    public void Agregar(Usuario usuario) => _db.Usuarios.Add(usuario);

    public Task<List<Rol>> ListarRolesAsync(CancellationToken ct = default) =>
        _db.Roles.OrderBy(r => r.Nombre).ToListAsync(ct);

    public Task<RefreshToken?> ObtenerRefreshTokenAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Include(t => t.Usuario!).ThenInclude(u => u.Rol)
            .Include(t => t.Usuario!).ThenInclude(u => u.Sede)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public void AgregarRefreshToken(RefreshToken token) => _db.RefreshTokens.Add(token);

    public async Task RevocarRefreshTokensAsync(int idUsuario, CancellationToken ct = default)
    {
        var vigentes = await _db.RefreshTokens.Where(t => t.IdUsuario == idUsuario && !t.Revocado).ToListAsync(ct);
        foreach (var token in vigentes) token.Revocado = true;
    }
}
