namespace VillaSalud.Domain.Entidades;

public class EstadoCita
{
    public int IdEstadoCita { get; set; }
    public string Nombre { get; set; } = "";
}

/// <summary>Identificadores fijos del catálogo ESTADO_CITA (los crea el script de base de datos).</summary>
public static class EstadosCita
{
    public const int Pendiente = 1;
    public const int Reprogramada = 2;
    public const int Atendida = 3;
    public const int Cancelada = 4;
    public const int NoAsistio = 5;

    public static string Nombre(int id) => id switch
    {
        Pendiente => "Pendiente",
        Reprogramada => "Reprogramada",
        Atendida => "Atendida",
        Cancelada => "Cancelada",
        NoAsistio => "No asistió",
        _ => "Desconocido"
    };

    /// <summary>RN07: solo las citas Pendiente o Reprogramada admiten cambios.</summary>
    public static bool EsActivo(int id) => id is Pendiente or Reprogramada;
}
