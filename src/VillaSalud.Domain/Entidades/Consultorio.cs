namespace VillaSalud.Domain.Entidades;

public class Consultorio
{
    public int IdConsultorio { get; set; }
    public int IdSede { get; set; }
    public string Numero { get; set; } = "";
    public string? Piso { get; set; }
    public bool Estado { get; set; } = true;

    public Sede? Sede { get; set; }
}
