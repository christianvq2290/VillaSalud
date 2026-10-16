namespace VillaSalud.Domain.Entidades;

/// <summary>Cada cambio de estado de una cita, con motivo, usuario y fecha (RN08).</summary>
public class HistorialCita
{
    public int IdHistorial { get; set; }
    public int IdCita { get; set; }
    public int? IdEstadoAnterior { get; set; }
    public int IdEstadoNuevo { get; set; }
    public string? Motivo { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime Fecha { get; set; }

    public Usuario? Usuario { get; set; }
}
