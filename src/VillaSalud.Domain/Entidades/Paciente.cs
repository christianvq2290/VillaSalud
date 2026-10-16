namespace VillaSalud.Domain.Entidades;

public class Paciente
{
    public int IdPaciente { get; set; }
    public string Dni { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? TipoSangre { get; set; }
    public string? Alergias { get; set; }

    public List<Consentimiento> Consentimientos { get; set; } = new();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public bool TieneConsentimientoVigente => Consentimientos.Any(c => !c.Revocado);
}
