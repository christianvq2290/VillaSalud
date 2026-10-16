namespace VillaSalud.Web.Models;

/// <summary>Apoyo de presentación para las vistas.</summary>
public static class Vista
{
    /// <summary>Color de la etiqueta según el estado de la cita (1 Pendiente, 2 Reprogramada, 3 Atendida, 4 Cancelada, 5 No asistió).</summary>
    public static string ClaseEstado(int idEstadoCita) => idEstadoCita switch
    {
        1 => "estado estado-pendiente",
        2 => "estado estado-reprogramada",
        3 => "estado estado-atendida",
        4 => "estado estado-cancelada",
        5 => "estado estado-inasistencia",
        _ => "estado"
    };

    public static readonly (int Id, string Nombre)[] EstadosCita =
    {
        (1, "Pendiente"), (2, "Reprogramada"), (3, "Atendida"), (4, "Cancelada"), (5, "No asistió")
    };

    public static readonly (byte Id, string Nombre)[] Dias =
    {
        (1, "Lunes"), (2, "Martes"), (3, "Miércoles"), (4, "Jueves"), (5, "Viernes"), (6, "Sábado"), (0, "Domingo")
    };

    public static string Hora(TimeOnly hora) => hora.ToString("HH:mm");
    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy");
    public static string FechaIso(DateOnly? fecha) => fecha?.ToString("yyyy-MM-dd") ?? "";
}
