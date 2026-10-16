namespace VillaSalud.Domain.Entidades;

/// <summary>Token de renovación. Solo se guarda su hash SHA-256, nunca el valor original.</summary>
public class RefreshToken
{
    public int IdToken { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime Expira { get; set; }
    public bool Revocado { get; set; }

    public Usuario? Usuario { get; set; }

    public bool EsValido(DateTime ahora) => !Revocado && Expira > ahora;
}
