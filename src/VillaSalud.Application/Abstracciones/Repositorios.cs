using VillaSalud.Domain.Entidades;

namespace VillaSalud.Application.Abstracciones;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default);
    Task<List<Usuario>> ListarAsync(CancellationToken ct = default);
    Task<bool> ExisteUsernameAsync(string username, int? excluirId, CancellationToken ct = default);
    void Agregar(Usuario usuario);
    Task<List<Rol>> ListarRolesAsync(CancellationToken ct = default);

    Task<RefreshToken?> ObtenerRefreshTokenAsync(string tokenHash, CancellationToken ct = default);
    void AgregarRefreshToken(RefreshToken token);
    Task RevocarRefreshTokensAsync(int idUsuario, CancellationToken ct = default);
}

public interface ICatalogoRepositorio
{
    Task<List<Sede>> ListarSedesAsync(CancellationToken ct = default);
    Task<Sede?> ObtenerSedeAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteSedeAsync(string nombre, int? excluirId, CancellationToken ct = default);
    void AgregarSede(Sede sede);

    Task<List<Especialidad>> ListarEspecialidadesAsync(CancellationToken ct = default);
    Task<Especialidad?> ObtenerEspecialidadAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteEspecialidadAsync(string nombre, int? excluirId, CancellationToken ct = default);
    Task<bool> EspecialidadTieneMedicosAsync(int id, CancellationToken ct = default);
    void AgregarEspecialidad(Especialidad especialidad);
    void EliminarEspecialidad(Especialidad especialidad);

    Task<List<Consultorio>> ListarConsultoriosAsync(int? idSede, CancellationToken ct = default);
    Task<Consultorio?> ObtenerConsultorioAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteConsultorioAsync(int idSede, string numero, int? excluirId, CancellationToken ct = default);
    void AgregarConsultorio(Consultorio consultorio);
}

public interface IMedicoRepositorio
{
    Task<List<Medico>> ListarAsync(bool soloActivos, CancellationToken ct = default);
    Task<Medico?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteColegiaturaAsync(string codigo, int? excluirId, CancellationToken ct = default);
    void Agregar(Medico medico);

    Task<List<HorarioMedico>> ListarHorariosAsync(int idMedico, CancellationToken ct = default);
    /// <summary>Horarios vigentes de otros médicos en el mismo consultorio y día (para evitar doble asignación).</summary>
    Task<List<HorarioMedico>> ListarHorariosDeConsultorioAsync(int idConsultorio, byte diaSemana, CancellationToken ct = default);
    Task<HorarioMedico?> ObtenerHorarioAsync(int idHorario, CancellationToken ct = default);
    void AgregarHorario(HorarioMedico horario);
}

public interface IPacienteRepositorio
{
    Task<List<Paciente>> ListarAsync(string? busqueda, CancellationToken ct = default);
    Task<Paciente?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Paciente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default);
    Task<bool> ExisteDniAsync(string dni, int? excluirId, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default);
    void Agregar(Paciente paciente);
}

public class FiltroCitas
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? IdSede { get; set; }
    public int? IdMedico { get; set; }
    public int? IdEspecialidad { get; set; }
    public int? IdEstadoCita { get; set; }
    public int? IdPaciente { get; set; }
}

public interface ICitaRepositorio
{
    /// <summary>Devuelve la cita con paciente, médico, especialidad, consultorio, sede e historial.</summary>
    Task<Cita?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<List<Cita>> ListarAsync(FiltroCitas filtro, CancellationToken ct = default);
    /// <summary>RN04 y RN05: indica si el médico, el consultorio o el paciente ya tienen una cita no cancelada que se cruza.</summary>
    Task<bool> ExisteCruceAsync(int idMedico, int idConsultorio, int idPaciente, DateOnly fecha, TimeOnly inicio, TimeOnly fin, int? excluirIdCita, CancellationToken ct = default);
    /// <summary>Citas no canceladas del médico o del consultorio en la fecha (para calcular la disponibilidad).</summary>
    Task<List<Cita>> ListarOcupadasAsync(int idMedico, IReadOnlyCollection<int> idsConsultorio, DateOnly fecha, CancellationToken ct = default);
    void Agregar(Cita cita);

    void AgregarNotificacion(Notificacion notificacion);
    Task<List<Notificacion>> ListarNotificacionesPendientesAsync(DateTime hasta, int maximo, CancellationToken ct = default);
    /// <summary>Citas activas de la fecha que aún no tienen recordatorio y cuyo paciente tiene correo.</summary>
    Task<List<Cita>> ListarCitasSinRecordatorioAsync(DateOnly fecha, CancellationToken ct = default);
}

public interface IAuditoriaRepositorio
{
    void Agregar(Auditoria registro);
    Task<List<Auditoria>> ListarAsync(DateTime desde, DateTime hasta, int maximo, CancellationToken ct = default);
}
