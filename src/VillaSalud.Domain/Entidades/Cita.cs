using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Domain.Entidades;

public class Cita
{
    public int IdCita { get; set; }
    public string Codigo { get; set; } = "";
    public int IdSede { get; set; }
    public int IdPaciente { get; set; }
    public int IdMedico { get; set; }
    public int IdConsultorio { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? MotivoConsulta { get; set; }
    public int IdEstadoCita { get; set; } = EstadosCita.Pendiente;
    public string? Observacion { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? IdUsuarioRegistro { get; set; }
    /// <summary>Versión de fila (ROWVERSION) para el control de concurrencia optimista (RN09).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Sede? Sede { get; set; }
    public Paciente? Paciente { get; set; }
    public Medico? Medico { get; set; }
    public Consultorio? Consultorio { get; set; }
    public EstadoCita? EstadoCita { get; set; }
    public List<HistorialCita> Historial { get; set; } = new();

    public bool EstaActiva => EstadosCita.EsActivo(IdEstadoCita);

    public bool SeCruzaCon(DateOnly fecha, TimeOnly inicio, TimeOnly fin) =>
        Fecha == fecha && HoraInicio < fin && inicio < HoraFin;

    public void RegistrarCreacion(int? idUsuario, DateTime ahora)
    {
        IdEstadoCita = EstadosCita.Pendiente;
        FechaRegistro = ahora;
        IdUsuarioRegistro = idUsuario;
        Historial.Add(new HistorialCita { IdEstadoNuevo = EstadosCita.Pendiente, Motivo = "Registro de la cita", IdUsuario = idUsuario, Fecha = ahora });
    }

    public void Reprogramar(DateOnly fecha, TimeOnly inicio, TimeOnly fin, int idConsultorio, string motivo, int? idUsuario, DateTime ahora)
    {
        ExigirActiva("reprogramar");
        ExigirMotivo(motivo, "reprogramación");
        Fecha = fecha;
        HoraInicio = inicio;
        HoraFin = fin;
        IdConsultorio = idConsultorio;
        CambiarEstado(EstadosCita.Reprogramada, motivo, idUsuario, ahora);
    }

    public void Cancelar(string motivo, int? idUsuario, DateTime ahora)
    {
        ExigirActiva("cancelar");
        ExigirMotivo(motivo, "cancelación");
        CambiarEstado(EstadosCita.Cancelada, motivo, idUsuario, ahora);
    }

    public void MarcarAtendida(string? observacion, int? idUsuario, DateTime ahora)
    {
        ExigirActiva("atender");
        if (!string.IsNullOrWhiteSpace(observacion)) Observacion = observacion.Trim();
        CambiarEstado(EstadosCita.Atendida, "Atención registrada", idUsuario, ahora);
    }

    public void MarcarNoAsistio(int? idUsuario, DateTime ahora)
    {
        ExigirActiva("marcar como inasistencia");
        CambiarEstado(EstadosCita.NoAsistio, "El paciente no asistió", idUsuario, ahora);
    }

    private void CambiarEstado(int nuevo, string motivo, int? idUsuario, DateTime ahora)
    {
        Historial.Add(new HistorialCita { IdCita = IdCita, IdEstadoAnterior = IdEstadoCita, IdEstadoNuevo = nuevo, Motivo = motivo.Trim(), IdUsuario = idUsuario, Fecha = ahora });
        IdEstadoCita = nuevo;
    }

    // RN07
    private void ExigirActiva(string accion)
    {
        if (!EstaActiva)
            throw new ReglaNegocioException($"No se puede {accion} una cita en estado {EstadosCita.Nombre(IdEstadoCita)}.");
    }

    // RN08
    private static void ExigirMotivo(string motivo, string operacion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaNegocioException($"Debe indicar el motivo de la {operacion}.");
    }
}
