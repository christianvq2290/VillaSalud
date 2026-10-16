using VillaSalud.Application.Dtos;

namespace VillaSalud.Web.Models;

public class TableroVm
{
    public bool VerIndicadores { get; set; }
    public IndicadoresDto? Indicadores { get; set; }
}

public class PacientesIndexVm
{
    public string? Busqueda { get; set; }
    public List<PacienteDto> Pacientes { get; set; } = new();
}

public class HorariosVm
{
    public MedicoDto Medico { get; set; } = new();
    public List<HorarioDto> Horarios { get; set; } = new();
    public HorarioDto Nuevo { get; set; } = new();
    public List<ConsultorioDto> Consultorios { get; set; } = new();
}

public class CitasIndexVm
{
    public FiltroCitasDto Filtro { get; set; } = new();
    public List<CitaDto> Citas { get; set; } = new();
    public List<MedicoDto> Medicos { get; set; } = new();
}

public class ProgramarCitaVm
{
    /// <summary>DNI con el que se busca al paciente.</summary>
    public string? Dni { get; set; }
    public PacienteDto? Paciente { get; set; }
    public ProgramarCitaDto Cita { get; set; } = new();
    public List<MedicoDto> Medicos { get; set; } = new();
    public List<IntervaloDto> Intervalos { get; set; } = new();
    /// <summary>Verdadero cuando ya se eligió paciente, médico y fecha, y se muestran los intervalos.</summary>
    public bool MostrarIntervalos { get; set; }
}

public class ReprogramarVm
{
    public CitaDetalleDto Cita { get; set; } = new();
    public ReprogramarCitaDto Datos { get; set; } = new();
    public List<IntervaloDto> Intervalos { get; set; } = new();
}

public class AgendaVm
{
    public DateOnly Fecha { get; set; }
    public int? IdMedico { get; set; }
    public bool EsMedico { get; set; }
    public List<MedicoDto> Medicos { get; set; } = new();
    public List<CitaDto> Citas { get; set; } = new();
}

public class ReportesVm
{
    public FiltroCitasDto Filtro { get; set; } = new();
    public ReporteCitasDto? Reporte { get; set; }
    public List<MedicoDto> Medicos { get; set; } = new();
}

public class UsuarioEditarVm
{
    public int IdUsuario { get; set; }
    public UsuarioGuardarDto Datos { get; set; } = new();
    public List<RolDto> Roles { get; set; } = new();
    public List<SedeDto> Sedes { get; set; } = new();
    public List<MedicoDto> Medicos { get; set; } = new();
}

public class AuditoriaVm
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public List<AuditoriaDto> Registros { get; set; } = new();
}
