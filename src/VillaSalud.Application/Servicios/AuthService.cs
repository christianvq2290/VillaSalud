using System.Security.Cryptography;
using System.Text;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

/// <summary>Política de contraseñas: mínimo 8 caracteres, al menos una letra y un dígito.</summary>
public static class PoliticaClaves
{
    public static void Validar(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 8)
            throw new ReglaNegocioException("La contraseña debe tener al menos 8 caracteres.");
        if (!clave.Any(char.IsLetter) || !clave.Any(char.IsDigit))
            throw new ReglaNegocioException("La contraseña debe incluir al menos una letra y un número.");
    }
}

public class AuthService
{
    public const int DiasVigenciaRefresh = 7;
    private const string MensajeGenerico = "Usuario o contraseña incorrectos.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IHasherClaves _hasher;
    private readonly IGeneradorTokens _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;
    private readonly IAuditoriaService _auditoria;

    public AuthService(IUsuarioRepositorio usuarios, IHasherClaves hasher, IGeneradorTokens tokens,
        IUnitOfWork uow, IReloj reloj, IAuditoriaService auditoria)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
        _uow = uow;
        _reloj = reloj;
        _auditoria = auditoria;
    }

    public async Task<SesionDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;
        var usuario = await _usuarios.ObtenerPorUsernameAsync(dto.Username.Trim(), ct);

        // Mismo mensaje si el usuario no existe o está inactivo: no se revela cuál es el caso.
        if (usuario is null || !usuario.Estado)
            throw new CredencialesInvalidasException(MensajeGenerico);

        if (usuario.EstaBloqueado(ahora))
            throw new CredencialesInvalidasException(MensajeBloqueo(usuario, ahora));

        if (!_hasher.Verificar(usuario.PasswordHash, dto.Password))
        {
            usuario.RegistrarIntentoFallido(ahora);
            _auditoria.Registrar("LOGIN_FALLIDO", "USUARIO", usuario.IdUsuario.ToString(), idUsuario: usuario.IdUsuario);
            await _uow.GuardarAsync(ct);
            throw new CredencialesInvalidasException(usuario.EstaBloqueado(ahora) ? MensajeBloqueo(usuario, ahora) : MensajeGenerico);
        }

        usuario.RegistrarIngresoCorrecto();
        var sesion = EmitirSesion(usuario, ahora);
        _auditoria.Registrar("LOGIN", "USUARIO", usuario.IdUsuario.ToString(), idUsuario: usuario.IdUsuario);
        await _uow.GuardarAsync(ct);
        return sesion;
    }

    /// <summary>Canjea un token de renovación válido por una sesión nueva. El token usado queda revocado (rotación).</summary>
    public async Task<SesionDto> RefrescarAsync(string refreshToken, CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;
        var registro = await _usuarios.ObtenerRefreshTokenAsync(HashToken(refreshToken), ct);
        if (registro is null || !registro.EsValido(ahora) || registro.Usuario is null || !registro.Usuario.Estado)
            throw new CredencialesInvalidasException("La sesión expiró. Inicie sesión nuevamente.");

        registro.Revocado = true;
        var sesion = EmitirSesion(registro.Usuario, ahora);
        await _uow.GuardarAsync(ct);
        return sesion;
    }

    public async Task CambiarClaveAsync(int idUsuario, CambiarClaveDto dto, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerAsync(idUsuario, ct) ?? throw new NoEncontradoException("Usuario no encontrado.");
        if (!_hasher.Verificar(usuario.PasswordHash, dto.ClaveActual))
            throw new ReglaNegocioException("La contraseña actual no es correcta.");
        PoliticaClaves.Validar(dto.ClaveNueva);
        if (dto.ClaveNueva == dto.ClaveActual)
            throw new ReglaNegocioException("La contraseña nueva debe ser distinta de la actual.");

        usuario.PasswordHash = _hasher.Hash(dto.ClaveNueva);
        usuario.DebeCambiarClave = false;
        await _usuarios.RevocarRefreshTokensAsync(idUsuario, ct);
        _auditoria.Registrar("CAMBIO_CLAVE", "USUARIO", idUsuario.ToString());
        await _uow.GuardarAsync(ct);
    }

    public async Task CerrarSesionAsync(int idUsuario, CancellationToken ct = default)
    {
        await _usuarios.RevocarRefreshTokensAsync(idUsuario, ct);
        _auditoria.Registrar("LOGOUT", "USUARIO", idUsuario.ToString());
        await _uow.GuardarAsync(ct);
    }

    private SesionDto EmitirSesion(Usuario usuario, DateTime ahora)
    {
        var acceso = _tokens.CrearTokenAcceso(usuario);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        _usuarios.AgregarRefreshToken(new RefreshToken
        {
            IdUsuario = usuario.IdUsuario,
            Usuario = usuario,
            TokenHash = HashToken(refresh),
            Expira = ahora.AddDays(DiasVigenciaRefresh)
        });
        return new SesionDto
        {
            TokenAcceso = acceso.Token,
            ExpiraUtc = acceso.ExpiraUtc,
            RefreshToken = refresh,
            IdUsuario = usuario.IdUsuario,
            Username = usuario.Username,
            Rol = usuario.Rol?.Nombre ?? "",
            IdSede = usuario.IdSede,
            Sede = usuario.Sede?.Nombre,
            IdMedico = usuario.IdMedico,
            DebeCambiarClave = usuario.DebeCambiarClave
        };
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string MensajeBloqueo(Usuario usuario, DateTime ahora)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling((usuario.BloqueadoHasta!.Value - ahora).TotalMinutes));
        return $"Cuenta bloqueada por intentos fallidos. Intente nuevamente en {minutos} minuto(s).";
    }
}
