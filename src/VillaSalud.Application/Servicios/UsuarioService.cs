using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

public class UsuarioService
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly ICatalogoRepositorio _catalogo;
    private readonly IMedicoRepositorio _medicos;
    private readonly IHasherClaves _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;
    private readonly IAuditoriaService _auditoria;

    public UsuarioService(IUsuarioRepositorio usuarios, ICatalogoRepositorio catalogo, IMedicoRepositorio medicos,
        IHasherClaves hasher, IUnitOfWork uow, IReloj reloj, IAuditoriaService auditoria)
    {
        _usuarios = usuarios;
        _catalogo = catalogo;
        _medicos = medicos;
        _hasher = hasher;
        _uow = uow;
        _reloj = reloj;
        _auditoria = auditoria;
    }

    public async Task<List<UsuarioDto>> ListarAsync(CancellationToken ct = default) =>
        (await _usuarios.ListarAsync(ct)).Select(u => u.ADto(_reloj.Ahora)).ToList();

    public async Task<UsuarioDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await _usuarios.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Usuario no encontrado.")).ADto(_reloj.Ahora);

    public async Task<List<RolDto>> ListarRolesAsync(CancellationToken ct = default) =>
        (await _usuarios.ListarRolesAsync(ct)).Select(r => new RolDto { IdRol = r.IdRol, Nombre = r.Nombre }).ToList();

    public async Task<UsuarioDto> CrearAsync(UsuarioGuardarDto dto, CancellationToken ct = default)
    {
        PoliticaClaves.Validar(dto.Password);
        var usuario = new Usuario { PasswordHash = _hasher.Hash(dto.Password!), DebeCambiarClave = true };
        await AplicarAsync(usuario, dto, ct);
        _usuarios.Agregar(usuario);
        await _uow.GuardarAsync(ct);
        _auditoria.Registrar("CREAR", "USUARIO", usuario.IdUsuario.ToString(), new { usuario.Username, usuario.IdRol, usuario.IdSede });
        await _uow.GuardarAsync(ct);
        return await ObtenerAsync(usuario.IdUsuario, ct);
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, UsuarioGuardarDto dto, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Usuario no encontrado.");
        await AplicarAsync(usuario, dto, ct);
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            PoliticaClaves.Validar(dto.Password);
            usuario.PasswordHash = _hasher.Hash(dto.Password);
            usuario.DebeCambiarClave = true;
            await _usuarios.RevocarRefreshTokensAsync(id, ct);
        }
        _auditoria.Registrar("MODIFICAR", "USUARIO", id.ToString(), new { usuario.Username, usuario.IdRol, usuario.IdSede, usuario.Estado });
        await _uow.GuardarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task DesbloquearAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Usuario no encontrado.");
        usuario.RegistrarIngresoCorrecto();
        _auditoria.Registrar("DESBLOQUEAR", "USUARIO", id.ToString());
        await _uow.GuardarAsync(ct);
    }

    private async Task AplicarAsync(Usuario usuario, UsuarioGuardarDto dto, CancellationToken ct)
    {
        var username = dto.Username.Trim();
        if (await _usuarios.ExisteUsernameAsync(username, usuario.IdUsuario == 0 ? null : usuario.IdUsuario, ct))
            throw new ReglaNegocioException("El nombre de usuario ya está en uso.");

        var rol = (await _usuarios.ListarRolesAsync(ct)).FirstOrDefault(r => r.IdRol == dto.IdRol)
                  ?? throw new ReglaNegocioException("El rol seleccionado no existe.");

        if (dto.IdSede.HasValue && await _catalogo.ObtenerSedeAsync(dto.IdSede.Value, ct) is null)
            throw new ReglaNegocioException("La sede seleccionada no existe.");

        if (rol.Nombre == Roles.Medico)
        {
            if (!dto.IdMedico.HasValue || await _medicos.ObtenerAsync(dto.IdMedico.Value, ct) is null)
                throw new ReglaNegocioException("Un usuario con rol Médico debe estar asociado a un médico registrado.");
        }
        else if (rol.Nombre != Roles.Administrador && !dto.IdSede.HasValue)
        {
            // RN01: solo el administrador de la red puede no tener sede
            throw new ReglaNegocioException("Debe asignar una sede al usuario.");
        }

        usuario.Username = username;
        usuario.IdRol = dto.IdRol;
        usuario.Rol = rol;
        usuario.IdSede = dto.IdSede;
        usuario.IdMedico = rol.Nombre == Roles.Medico ? dto.IdMedico : null;
        usuario.Estado = dto.Estado;
    }
}
