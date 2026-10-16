using System.ComponentModel.DataAnnotations;

namespace VillaSalud.Application.Dtos;

public class LoginDto
{
    [Required(ErrorMessage = "Ingrese el usuario")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la contraseña")]
    public string Password { get; set; } = "";
}

public class RefrescarDto
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

public class CambiarClaveDto
{
    [Required(ErrorMessage = "Ingrese la contraseña actual")]
    public string ClaveActual { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la contraseña nueva")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string ClaveNueva { get; set; } = "";

    [Compare(nameof(ClaveNueva), ErrorMessage = "La confirmación no coincide")]
    public string ConfirmarClave { get; set; } = "";
}

public class SesionDto
{
    public string TokenAcceso { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public string RefreshToken { get; set; } = "";
    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public string Rol { get; set; } = "";
    public int? IdSede { get; set; }
    public string? Sede { get; set; }
    public int? IdMedico { get; set; }
    public bool DebeCambiarClave { get; set; }
}

public class RolDto
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
}

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public int IdRol { get; set; }
    public string Rol { get; set; } = "";
    public int? IdSede { get; set; }
    public string? Sede { get; set; }
    public int? IdMedico { get; set; }
    public string? Medico { get; set; }
    public bool Estado { get; set; }
    public bool Bloqueado { get; set; }
    public bool DebeCambiarClave { get; set; }
}

public class UsuarioGuardarDto
{
    [Required(ErrorMessage = "Ingrese el nombre de usuario")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 30 caracteres")]
    public string Username { get; set; } = "";

    /// <summary>Obligatoria al crear. Al editar, vacío significa conservar la clave actual.</summary>
    [StringLength(100)]
    public string? Password { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol")]
    public int IdRol { get; set; }

    public int? IdSede { get; set; }
    public int? IdMedico { get; set; }
    public bool Estado { get; set; } = true;
}
