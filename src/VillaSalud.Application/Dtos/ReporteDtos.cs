namespace VillaSalud.Application.Dtos;

public class SerieDto
{
    public string Etiqueta { get; set; } = "";
    public int Valor { get; set; }
}

public class IndicadoresDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int TotalCitas { get; set; }
    public int Pendientes { get; set; }
    public int Atendidas { get; set; }
    public int Canceladas { get; set; }
    public int NoAsistio { get; set; }
    public int CitasHoy { get; set; }
    /// <summary>Inasistencias / (atendidas + inasistencias) × 100.</summary>
    public decimal TasaInasistencia { get; set; }
    public List<SerieDto> PorEstado { get; set; } = new();
    public List<SerieDto> PorEspecialidad { get; set; } = new();
    public List<SerieDto> PorDia { get; set; } = new();
    public List<SerieDto> PorConsultorio { get; set; } = new();
}

public class FilaReporteDto
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Codigo { get; set; } = "";
    public string Paciente { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Medico { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Consultorio { get; set; } = "";
    public string Sede { get; set; } = "";
    public string Estado { get; set; } = "";
}

public class ResumenMedicoDto
{
    public string Medico { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public int Total { get; set; }
    public int Atendidas { get; set; }
    public int Canceladas { get; set; }
    public int NoAsistio { get; set; }
}

public class ReporteCitasDto
{
    public string Titulo { get; set; } = "Reporte de citas";
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public DateTime GeneradoEl { get; set; }
    public List<FilaReporteDto> Filas { get; set; } = new();
    public List<ResumenMedicoDto> Resumen { get; set; } = new();
}

public class ArchivoDto
{
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public string Mime { get; set; } = "application/octet-stream";
    public string Nombre { get; set; } = "";
}

public class AuditoriaDto
{
    public DateTime Fecha { get; set; }
    public string? Usuario { get; set; }
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string? IdEntidad { get; set; }
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
}
