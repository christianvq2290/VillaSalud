namespace VillaSalud.Domain.Entidades;

public class Usuario
{
    public const int MaximoIntentosFallidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int IdRol { get; set; }
    /// <summary>Sede a la que pertenece. Nulo = administrador de la red (ve todas las sedes).</summary>
    public int? IdSede { get; set; }
    /// <summary>Médico asociado cuando el rol es Medico.</summary>
    public int? IdMedico { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public bool DebeCambiarClave { get; set; }
    public bool Estado { get; set; } = true;

    public Rol? Rol { get; set; }
    public Sede? Sede { get; set; }
    public Medico? Medico { get; set; }

    public bool EstaBloqueado(DateTime ahora) => BloqueadoHasta.HasValue && BloqueadoHasta.Value > ahora;

    /// <summary>Suma un intento fallido y bloquea la cuenta al llegar al máximo.</summary>
    public void RegistrarIntentoFallido(DateTime ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos >= MaximoIntentosFallidos)
        {
            BloqueadoHasta = ahora.Add(DuracionBloqueo);
            IntentosFallidos = 0;
        }
    }

    public void RegistrarIngresoCorrecto()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }
}
