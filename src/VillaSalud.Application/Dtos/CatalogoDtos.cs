using System.ComponentModel.DataAnnotations;

namespace VillaSalud.Application.Dtos;

public class SedeDto
{
    public int IdSede { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre")]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la dirección")]
    [StringLength(150)]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el distrito")]
    [StringLength(60)]
    public string Distrito { get; set; } = "";

    public bool Estado { get; set; } = true;
}

public class EspecialidadDto
{
    public int IdEspecialidad { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre")]
    [StringLength(150)]
    public string Nombre { get; set; } = "";
}

public class ConsultorioDto
{
    public int IdConsultorio { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sede")]
    public int IdSede { get; set; }
    public string? Sede { get; set; }

    [Required(ErrorMessage = "Ingrese el número")]
    [StringLength(10)]
    public string Numero { get; set; } = "";

    [StringLength(10)]
    public string? Piso { get; set; }

    public bool Estado { get; set; } = true;
}

public class MedicoDto
{
    public int IdMedico { get; set; }

    [Required(ErrorMessage = "Ingrese el código de colegiatura")]
    [StringLength(50)]
    public string CodigoColegiatura { get; set; } = "";

    [Required(ErrorMessage = "Ingrese los nombres")]
    [StringLength(100)]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "Ingrese los apellidos")]
    [StringLength(100)]
    public string Apellidos { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una especialidad")]
    public int IdEspecialidad { get; set; }
    public string? Especialidad { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido")]
    [StringLength(100)]
    public string? Email { get; set; }

    public bool Estado { get; set; } = true;

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}

public class HorarioDto
{
    public int IdHorario { get; set; }
    public int IdMedico { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un consultorio")]
    public int IdConsultorio { get; set; }
    public string? Consultorio { get; set; }
    public string? Sede { get; set; }

    [Range(0, 6, ErrorMessage = "Día no válido")]
    public byte DiaSemana { get; set; } = 1;
    public string? Dia { get; set; }

    public TimeOnly HoraInicio { get; set; } = new(8, 0);
    public TimeOnly HoraFin { get; set; } = new(13, 0);

    [Range(10, 120, ErrorMessage = "La duración debe estar entre 10 y 120 minutos")]
    public byte DuracionCitaMin { get; set; } = 30;
}

/// <summary>Intervalo de atención de un médico en una fecha, con su disponibilidad.</summary>
public class IntervaloDto
{
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int IdConsultorio { get; set; }
    public string Consultorio { get; set; } = "";
    public bool Disponible { get; set; }
}

public class PacienteDto
{
    public int IdPaciente { get; set; }

    [Required(ErrorMessage = "Ingrese el DNI")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos")]
    public string Dni { get; set; } = "";

    [Required(ErrorMessage = "Ingrese los nombres")]
    [StringLength(100)]
    public string Nombres { get; set; } = "";

    [Required(ErrorMessage = "Ingrese los apellidos")]
    [StringLength(100)]
    public string Apellidos { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido")]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    [StringLength(5)]
    public string? TipoSangre { get; set; }

    [StringLength(500)]
    public string? Alergias { get; set; }

    /// <summary>Entrada: el paciente acepta el tratamiento de sus datos (se registra un consentimiento).</summary>
    public bool AceptaTratamientoDatos { get; set; }

    /// <summary>Salida: tiene un consentimiento no revocado.</summary>
    public bool ConsentimientoVigente { get; set; }
    public DateTime? FechaConsentimiento { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
