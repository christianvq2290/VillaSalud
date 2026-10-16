namespace VillaSalud.Domain.Entidades;

/// <summary>Aceptación del paciente para el tratamiento de sus datos personales (Ley N.° 29733).</summary>
public class Consentimiento
{
    public const string VersionVigente = "1.0";
    public const string FinalidadCitas = "Gestión de citas médicas y comunicaciones asociadas";

    public int IdConsentimiento { get; set; }
    public int IdPaciente { get; set; }
    public string Finalidad { get; set; } = FinalidadCitas;
    public string VersionTexto { get; set; } = VersionVigente;
    public DateTime FechaAceptacion { get; set; }
    /// <summary>Presencial, Web o Telefónico.</summary>
    public string Canal { get; set; } = "Presencial";
    public bool Revocado { get; set; }
}
