namespace VillaSalud.Domain.Entidades;

/// <summary>Sede de la red. Es la unidad de aislamiento de la información (RN01).</summary>
public class Sede
{
    public int IdSede { get; set; }
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Distrito { get; set; } = "";
    public bool Estado { get; set; } = true;
}
