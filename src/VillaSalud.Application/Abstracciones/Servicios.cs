using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Application.Abstracciones;

/// <summary>Fecha y hora de trabajo del sistema (hora de Perú). Se abstrae para poder probar con una hora fija.</summary>
public interface IReloj
{
    DateTime Ahora { get; }
}

/// <summary>Datos del usuario que ejecuta la operación; la API los obtiene del token JWT.</summary>
public interface IContextoUsuario
{
    int? IdUsuario { get; }
    string? Rol { get; }
    /// <summary>Sede del usuario. Nulo = sin restricción de sede (RN01).</summary>
    int? IdSede { get; }
    int? IdMedico { get; }
    string? Ip { get; }
}

public interface IHasherClaves
{
    string Hash(string clave);
    bool Verificar(string hash, string clave);
}

public record TokenAcceso(string Token, DateTime ExpiraUtc);

public interface IGeneradorTokens
{
    TokenAcceso CrearTokenAcceso(Usuario usuario);
}

public interface IEnviadorCorreo
{
    /// <summary>Falso cuando no hay servidor SMTP configurado; en ese caso las notificaciones quedan pendientes.</summary>
    bool Configurado { get; }
    Task EnviarAsync(string destino, string asunto, string cuerpo, CancellationToken ct = default);
}

/// <summary>Genera el reporte de citas en un formato concreto. Agregar un formato nuevo no modifica ReporteService.</summary>
public interface IExportadorReporte
{
    /// <summary>pdf, xlsx o csv.</summary>
    string Formato { get; }
    string Mime { get; }
    byte[] Generar(ReporteCitasDto reporte);
}

public interface ITransaccion : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    /// <summary>Abre una transacción. Con <paramref name="serializable"/> se evita que dos usuarios reserven el mismo intervalo a la vez.</summary>
    Task<ITransaccion> IniciarTransaccionAsync(bool serializable = false, CancellationToken ct = default);
    Task GuardarAsync(CancellationToken ct = default);
}
