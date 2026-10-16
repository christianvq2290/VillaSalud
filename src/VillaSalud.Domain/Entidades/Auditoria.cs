namespace VillaSalud.Domain.Entidades;

/// <summary>Registro de quién hizo qué, sobre qué dato, cuándo y desde dónde.</summary>
public class Auditoria
{
    public long IdAuditoria { get; set; }
    public int? IdUsuario { get; set; }
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string? IdEntidad { get; set; }
    public string? ValoresJson { get; set; }
    public string? Ip { get; set; }
    public DateTime Fecha { get; set; }

    public Usuario? Usuario { get; set; }
}
