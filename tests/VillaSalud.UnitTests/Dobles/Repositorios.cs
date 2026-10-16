using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.UnitTests.Dobles;

public class UnidadDeTrabajoEnMemoria : IUnitOfWork
{
    private readonly AlmacenEnMemoria _db;
    public UnidadDeTrabajoEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task GuardarAsync(CancellationToken ct = default)
    {
        _db.Guardar();
        return Task.CompletedTask;
    }

    public Task<ITransaccion> IniciarTransaccionAsync(bool serializable = false, CancellationToken ct = default) =>
        Task.FromResult<ITransaccion>(new Transaccion(_db));

    private sealed class Transaccion : ITransaccion
    {
        private readonly AlmacenEnMemoria _db;
        public Transaccion(AlmacenEnMemoria db) => _db = db;
        public Task ConfirmarAsync(CancellationToken ct = default) { _db.TransaccionesConfirmadas++; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public class UsuarioRepositorioEnMemoria : IUsuarioRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public UsuarioRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Usuarios.FirstOrDefault(u => u.IdUsuario == id)); }
    public Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Usuarios.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))); }
    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Usuarios.OrderBy(u => u.Username).ToList()); }
    public Task<bool> ExisteUsernameAsync(string username, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Usuarios.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.IdUsuario != excluirId));
    public void Agregar(Usuario usuario) => _db.Usuarios.Add(usuario);
    public Task<List<Rol>> ListarRolesAsync(CancellationToken ct = default) => Task.FromResult(_db.Roles.ToList());
    public Task<RefreshToken?> ObtenerRefreshTokenAsync(string tokenHash, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash)); }
    public void AgregarRefreshToken(RefreshToken token) => _db.RefreshTokens.Add(token);
    public Task RevocarRefreshTokensAsync(int idUsuario, CancellationToken ct = default) { foreach (var t in _db.RefreshTokens.Where(t => t.IdUsuario == idUsuario)) t.Revocado = true; return Task.CompletedTask; }
}

public class CatalogoRepositorioEnMemoria : ICatalogoRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public CatalogoRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task<List<Sede>> ListarSedesAsync(CancellationToken ct = default) => Task.FromResult(_db.Sedes.OrderBy(s => s.Nombre).ToList());
    public Task<Sede?> ObtenerSedeAsync(int id, CancellationToken ct = default) => Task.FromResult(_db.Sedes.FirstOrDefault(s => s.IdSede == id));
    public Task<bool> ExisteSedeAsync(string nombre, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Sedes.Any(s => Igual(s.Nombre, nombre) && s.IdSede != excluirId));
    public void AgregarSede(Sede sede) => _db.Sedes.Add(sede);

    public Task<List<Especialidad>> ListarEspecialidadesAsync(CancellationToken ct = default) => Task.FromResult(_db.Especialidades.OrderBy(e => e.Nombre).ToList());
    public Task<Especialidad?> ObtenerEspecialidadAsync(int id, CancellationToken ct = default) => Task.FromResult(_db.Especialidades.FirstOrDefault(e => e.IdEspecialidad == id));
    public Task<bool> ExisteEspecialidadAsync(string nombre, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Especialidades.Any(e => Igual(e.Nombre, nombre) && e.IdEspecialidad != excluirId));
    public Task<bool> EspecialidadTieneMedicosAsync(int id, CancellationToken ct = default) => Task.FromResult(_db.Medicos.Any(m => m.IdEspecialidad == id));
    public void AgregarEspecialidad(Especialidad especialidad) => _db.Especialidades.Add(especialidad);
    public void EliminarEspecialidad(Especialidad especialidad) => _db.Especialidades.Remove(especialidad);

    public Task<List<Consultorio>> ListarConsultoriosAsync(int? idSede, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Consultorios.Where(c => idSede == null || c.IdSede == idSede).OrderBy(c => c.IdSede).ThenBy(c => c.Numero).ToList()); }
    public Task<Consultorio?> ObtenerConsultorioAsync(int id, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Consultorios.FirstOrDefault(c => c.IdConsultorio == id)); }
    public Task<bool> ExisteConsultorioAsync(int idSede, string numero, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Consultorios.Any(c => c.IdSede == idSede && Igual(c.Numero, numero) && c.IdConsultorio != excluirId));
    public void AgregarConsultorio(Consultorio consultorio) => _db.Consultorios.Add(consultorio);

    private static bool Igual(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

public class MedicoRepositorioEnMemoria : IMedicoRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public MedicoRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task<List<Medico>> ListarAsync(bool soloActivos, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Medicos.Where(m => !soloActivos || m.Estado).OrderBy(m => m.Apellidos).ToList()); }
    public Task<Medico?> ObtenerAsync(int id, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Medicos.FirstOrDefault(m => m.IdMedico == id)); }
    public Task<bool> ExisteColegiaturaAsync(string codigo, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Medicos.Any(m => m.CodigoColegiatura == codigo && m.IdMedico != excluirId));
    public void Agregar(Medico medico) => _db.Medicos.Add(medico);
    public Task<List<HorarioMedico>> ListarHorariosAsync(int idMedico, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Horarios.Where(h => h.IdMedico == idMedico && h.Vigente).ToList()); }
    public Task<List<HorarioMedico>> ListarHorariosDeConsultorioAsync(int idConsultorio, byte diaSemana, CancellationToken ct = default) => Task.FromResult(_db.Horarios.Where(h => h.IdConsultorio == idConsultorio && h.DiaSemana == diaSemana && h.Vigente).ToList());
    public Task<HorarioMedico?> ObtenerHorarioAsync(int idHorario, CancellationToken ct = default) => Task.FromResult(_db.Horarios.FirstOrDefault(h => h.IdHorario == idHorario));
    public void AgregarHorario(HorarioMedico horario) => _db.Horarios.Add(horario);
}

public class PacienteRepositorioEnMemoria : IPacienteRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public PacienteRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task<List<Paciente>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        Task.FromResult(_db.Pacientes.Where(p => busqueda == null || p.Dni.Contains(busqueda) || p.NombreCompleto.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Apellidos).ToList());
    public Task<Paciente?> ObtenerAsync(int id, CancellationToken ct = default) => Task.FromResult(_db.Pacientes.FirstOrDefault(p => p.IdPaciente == id));
    public Task<Paciente?> ObtenerPorDniAsync(string dni, CancellationToken ct = default) => Task.FromResult(_db.Pacientes.FirstOrDefault(p => p.Dni == dni));
    public Task<bool> ExisteDniAsync(string dni, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Pacientes.Any(p => p.Dni == dni && p.IdPaciente != excluirId));
    public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct = default) => Task.FromResult(_db.Pacientes.Any(p => string.Equals(p.Email, email, StringComparison.OrdinalIgnoreCase) && p.IdPaciente != excluirId));
    public void Agregar(Paciente paciente) => _db.Pacientes.Add(paciente);
}

public class CitaRepositorioEnMemoria : ICitaRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public CitaRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;

    public Task<Cita?> ObtenerAsync(int id, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Citas.FirstOrDefault(c => c.IdCita == id)); }
    public Task<Cita?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) { _db.Enlazar(); return Task.FromResult(_db.Citas.FirstOrDefault(c => c.Codigo == codigo)); }

    public Task<List<Cita>> ListarAsync(FiltroCitas f, CancellationToken ct = default)
    {
        _db.Enlazar();
        return Task.FromResult(_db.Citas.Where(c =>
            (f.Desde == null || c.Fecha >= f.Desde) && (f.Hasta == null || c.Fecha <= f.Hasta) &&
            (f.IdSede == null || c.IdSede == f.IdSede) && (f.IdMedico == null || c.IdMedico == f.IdMedico) &&
            (f.IdEspecialidad == null || c.Medico?.IdEspecialidad == f.IdEspecialidad) &&
            (f.IdEstadoCita == null || c.IdEstadoCita == f.IdEstadoCita) &&
            (f.IdPaciente == null || c.IdPaciente == f.IdPaciente)).ToList());
    }

    public Task<bool> ExisteCruceAsync(int idMedico, int idConsultorio, int idPaciente, DateOnly fecha, TimeOnly inicio, TimeOnly fin, int? excluirIdCita, CancellationToken ct = default) =>
        Task.FromResult(_db.Citas.Any(c => c.IdCita != excluirIdCita && c.IdEstadoCita != EstadosCita.Cancelada &&
            (c.IdMedico == idMedico || c.IdConsultorio == idConsultorio || c.IdPaciente == idPaciente) && c.SeCruzaCon(fecha, inicio, fin)));

    public Task<List<Cita>> ListarOcupadasAsync(int idMedico, IReadOnlyCollection<int> idsConsultorio, DateOnly fecha, CancellationToken ct = default) =>
        Task.FromResult(_db.Citas.Where(c => c.Fecha == fecha && c.IdEstadoCita != EstadosCita.Cancelada && (c.IdMedico == idMedico || idsConsultorio.Contains(c.IdConsultorio))).ToList());

    public void Agregar(Cita cita) => _db.Citas.Add(cita);
    public void AgregarNotificacion(Notificacion notificacion) => _db.Notificaciones.Add(notificacion);

    public Task<List<Notificacion>> ListarNotificacionesPendientesAsync(DateTime hasta, int maximo, CancellationToken ct = default)
    {
        _db.Enlazar();
        return Task.FromResult(_db.Notificaciones.Where(n => n.EstadoEnvio == Notificacion.EstadoPendiente && n.FechaProgramada <= hasta).Take(maximo).ToList());
    }

    public Task<List<Cita>> ListarCitasSinRecordatorioAsync(DateOnly fecha, CancellationToken ct = default)
    {
        _db.Enlazar();
        return Task.FromResult(_db.Citas.Where(c => c.Fecha == fecha && c.EstaActiva && !string.IsNullOrEmpty(c.Paciente?.Email) &&
            !_db.Notificaciones.Any(n => n.IdCita == c.IdCita && n.Tipo == Notificacion.TipoRecordatorio)).ToList());
    }
}

public class AuditoriaRepositorioEnMemoria : IAuditoriaRepositorio
{
    private readonly AlmacenEnMemoria _db;
    public AuditoriaRepositorioEnMemoria(AlmacenEnMemoria db) => _db = db;
    public void Agregar(Auditoria registro) => _db.Auditorias.Add(registro);
    public Task<List<Auditoria>> ListarAsync(DateTime desde, DateTime hasta, int maximo, CancellationToken ct = default)
    {
        _db.Enlazar();
        return Task.FromResult(_db.Auditorias.Where(a => a.Fecha >= desde && a.Fecha < hasta).OrderByDescending(a => a.Fecha).Take(maximo).ToList());
    }
}
