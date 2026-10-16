namespace VillaSalud.Domain.Entidades;

public class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
}

/// <summary>Nombres de rol usados en las políticas de autorización.</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Medico = "Medico";
    public const string Recepcionista = "Recepcionista";
}
