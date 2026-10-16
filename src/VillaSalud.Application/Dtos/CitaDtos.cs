using System.ComponentModel.DataAnnotations;

namespace VillaSalud.Application.Dtos;

public class ProgramarCitaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un paciente")]
    public int IdPaciente { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un médico")]
    public int IdMedico { get; set; }

    public DateOnly Fecha { get; set; }

    /// <summary>Inicio del intervalo elegido. El fin y el consultorio se obtienen del horario del médico.</summary>
    public TimeOnly HoraInicio { get; set; }

    [StringLength(500)]
    public string? MotivoConsulta { get; set; }
}

public class ReprogramarCitaDto
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }

    [Required(ErrorMessage = "Indique el motivo de la reprogramación")]
    [StringLength(250)]
    public string Motivo { get; set; } = "";

    /// <summary>Versión de la cita que el usuario tenía en pantalla (RN09).</summary>
    public string? RowVersion { get; set; }
}

public class CancelarCitaDto
{
    [Required(ErrorMessage = "Indique el motivo de la cancelación")]
    [StringLength(250)]
    public string Motivo { get; set; } = "";

    public string? RowVersion { get; set; }
}

public class AtenderCitaDto
{
    /// <summary>Verdadero = atendida; falso = el paciente no asistió.</summary>
    public bool Asistio { get; set; } = true;

    [StringLength(250)]
    public string? Observacion { get; set; }
}

public class FiltroCitasDto
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? IdSede { get; set; }
    public int? IdMedico { get; set; }
    public int? IdEspecialidad { get; set; }
    public int? IdEstadoCita { get; set; }
    public int? IdPaciente { get; set; }
}

public class CitaDto
{
    public int IdCita { get; set; }
    public string Codigo { get; set; } = "";
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public int IdPaciente { get; set; }
    public string Paciente { get; set; } = "";
    public string DniPaciente { get; set; } = "";
    public int IdMedico { get; set; }
    public string Medico { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public int IdConsultorio { get; set; }
    public string Consultorio { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? MotivoConsulta { get; set; }
    public int IdEstadoCita { get; set; }
    public string Estado { get; set; } = "";
    public string? Observacion { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string RowVersion { get; set; } = "";
    public bool Activa { get; set; }
}

public class HistorialCitaDto
{
    public DateTime Fecha { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = "";
    public string? Motivo { get; set; }
    public string? Usuario { get; set; }
}

public class CitaDetalleDto : CitaDto
{
    public List<HistorialCitaDto> Historial { get; set; } = new();
}

/// <summary>Respuesta del canal público (asistente virtual). RN10: solo datos mínimos, sin motivo ni datos clínicos.</summary>
public class EstadoCitaPublicoDto
{
    public string Codigo { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public string Medico { get; set; } = "";
    public string Consultorio { get; set; } = "";
    public string Sede { get; set; } = "";
    public string Estado { get; set; } = "";
}

public class EspecialidadPublicaDto
{
    public string Especialidad { get; set; } = "";
    public List<string> Medicos { get; set; } = new();
}

public class SedePublicaDto
{
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Distrito { get; set; } = "";
}
