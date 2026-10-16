namespace VillaSalud.Domain.Entidades;

public class Notificacion
{
    public const string TipoConfirmacion = "Confirmacion";
    public const string TipoRecordatorio = "Recordatorio";
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoEnviada = "Enviada";
    public const string EstadoError = "Error";

    public int IdNotificacion { get; set; }
    public int IdCita { get; set; }
    public string Tipo { get; set; } = TipoConfirmacion;
    public string Destino { get; set; } = "";
    public string EstadoEnvio { get; set; } = EstadoPendiente;
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? Detalle { get; set; }

    public Cita? Cita { get; set; }
}
