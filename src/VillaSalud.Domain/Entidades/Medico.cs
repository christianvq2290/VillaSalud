namespace VillaSalud.Domain.Entidades;

public class Medico
{
    public int IdMedico { get; set; }
    public string CodigoColegiatura { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public int IdEspecialidad { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; } = true;

    public Especialidad? Especialidad { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
