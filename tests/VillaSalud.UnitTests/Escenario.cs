using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Servicios;
using VillaSalud.Domain.Entidades;
using VillaSalud.UnitTests.Dobles;

namespace VillaSalud.UnitTests;

/// <summary>
/// Datos y servicios listos para cada prueba. "Hoy" es el lunes 09/11/2026 a las 07:00.
/// Sede 1 (Villa El Salvador): consultorios 101 y 102. Sede 2 (San Juan): consultorio 201.
/// La Dra. Rojas (cardiología) atiende lunes 08:00-12:00 y martes 08:00-10:00 en el 101.
/// El Dr. Paz (pediatría) atiende lunes 08:00-12:00 en el 102 y lunes 14:00-16:00 en el 201.
/// </summary>
public class Escenario
{
    public const int SedeVes = 1, SedeSjm = 2;
    public const int RolAdmin = 1, RolMedico = 2, RolRecepcion = 3;
    public const int Cardiologia = 1, Pediatria = 2;
    public const int DraRojas = 1, DrPaz = 2;
    public const int Cons101 = 1, Cons102 = 2, Cons201 = 3;
    public const int PacienteAna = 1, PacienteSinConsentimiento = 2, PacienteLuis = 3;
    public const int UsuAdmin = 1, UsuRecepVes = 2, UsuRecepSjm = 3, UsuDraRojas = 4;
    public const string ClaveValida = "Clave#2026";

    public static readonly DateOnly Lunes = new(2026, 11, 9);
    public static readonly DateOnly Martes = new(2026, 11, 10);

    public AlmacenEnMemoria Db { get; } = new();
    public RelojFijo Reloj { get; } = new(new DateTime(2026, 11, 9, 7, 0, 0));
    public ContextoFijo Contexto { get; } = new();
    public CorreoDePrueba Correo { get; } = new();
    public HasherDePrueba Hasher { get; } = new();

    public Escenario()
    {
        Db.Sedes.Add(new Sede { IdSede = SedeVes, Nombre = "Villa El Salvador", Direccion = "Av. Central 100", Distrito = "Villa El Salvador" });
        Db.Sedes.Add(new Sede { IdSede = SedeSjm, Nombre = "San Juan", Direccion = "Av. Los Héroes 200", Distrito = "San Juan de Miraflores" });
        Db.Roles.Add(new Rol { IdRol = RolAdmin, Nombre = Roles.Administrador });
        Db.Roles.Add(new Rol { IdRol = RolMedico, Nombre = Roles.Medico });
        Db.Roles.Add(new Rol { IdRol = RolRecepcion, Nombre = Roles.Recepcionista });
        Db.Especialidades.Add(new Especialidad { IdEspecialidad = Cardiologia, Nombre = "Cardiología" });
        Db.Especialidades.Add(new Especialidad { IdEspecialidad = Pediatria, Nombre = "Pediatría" });
        Db.Medicos.Add(new Medico { IdMedico = DraRojas, CodigoColegiatura = "CMP-1001", Nombres = "Elena", Apellidos = "Rojas", IdEspecialidad = Cardiologia });
        Db.Medicos.Add(new Medico { IdMedico = DrPaz, CodigoColegiatura = "CMP-1002", Nombres = "Mario", Apellidos = "Paz", IdEspecialidad = Pediatria });
        Db.Consultorios.Add(new Consultorio { IdConsultorio = Cons101, IdSede = SedeVes, Numero = "101", Piso = "1" });
        Db.Consultorios.Add(new Consultorio { IdConsultorio = Cons102, IdSede = SedeVes, Numero = "102", Piso = "1" });
        Db.Consultorios.Add(new Consultorio { IdConsultorio = Cons201, IdSede = SedeSjm, Numero = "201", Piso = "2" });
        Db.Horarios.Add(new HorarioMedico { IdHorario = 1, IdMedico = DraRojas, IdConsultorio = Cons101, DiaSemana = 1, HoraInicio = new(8, 0), HoraFin = new(12, 0), DuracionCitaMin = 30 });
        Db.Horarios.Add(new HorarioMedico { IdHorario = 2, IdMedico = DraRojas, IdConsultorio = Cons101, DiaSemana = 2, HoraInicio = new(8, 0), HoraFin = new(10, 0), DuracionCitaMin = 30 });
        Db.Horarios.Add(new HorarioMedico { IdHorario = 3, IdMedico = DrPaz, IdConsultorio = Cons102, DiaSemana = 1, HoraInicio = new(8, 0), HoraFin = new(12, 0), DuracionCitaMin = 30 });
        Db.Horarios.Add(new HorarioMedico { IdHorario = 4, IdMedico = DrPaz, IdConsultorio = Cons201, DiaSemana = 1, HoraInicio = new(14, 0), HoraFin = new(16, 0), DuracionCitaMin = 30 });

        var consentimiento = new Consentimiento { IdConsentimiento = 1, IdPaciente = PacienteAna, FechaAceptacion = Reloj.Ahora.AddDays(-30) };
        Db.Pacientes.Add(new Paciente { IdPaciente = PacienteAna, Dni = "40000001", Nombres = "Ana", Apellidos = "Quispe", Email = "ana@correo.pe", Consentimientos = { consentimiento } });
        Db.Pacientes.Add(new Paciente { IdPaciente = PacienteSinConsentimiento, Dni = "40000002", Nombres = "Beto", Apellidos = "Ramos" });
        Db.Pacientes.Add(new Paciente { IdPaciente = PacienteLuis, Dni = "40000003", Nombres = "Luis", Apellidos = "Soto", Consentimientos = { new Consentimiento { IdConsentimiento = 2, IdPaciente = PacienteLuis, FechaAceptacion = Reloj.Ahora.AddDays(-5) } } });

        Db.Usuarios.Add(new Usuario { IdUsuario = UsuAdmin, Username = "admin", PasswordHash = Hasher.Hash(ClaveValida), IdRol = RolAdmin });
        Db.Usuarios.Add(new Usuario { IdUsuario = UsuRecepVes, Username = "recepcion.ves", PasswordHash = Hasher.Hash(ClaveValida), IdRol = RolRecepcion, IdSede = SedeVes });
        Db.Usuarios.Add(new Usuario { IdUsuario = UsuRecepSjm, Username = "recepcion.sjm", PasswordHash = Hasher.Hash(ClaveValida), IdRol = RolRecepcion, IdSede = SedeSjm });
        Db.Usuarios.Add(new Usuario { IdUsuario = UsuDraRojas, Username = "erojas", PasswordHash = Hasher.Hash(ClaveValida), IdRol = RolMedico, IdMedico = DraRojas });
        Db.Enlazar();
        ComoRecepcionistaVes();
    }

    public void ComoAdministrador() => Fijar(UsuAdmin, Roles.Administrador, null, null);
    public void ComoRecepcionistaVes() => Fijar(UsuRecepVes, Roles.Recepcionista, SedeVes, null);
    public void ComoRecepcionistaSjm() => Fijar(UsuRecepSjm, Roles.Recepcionista, SedeSjm, null);
    public void ComoDraRojas() => Fijar(UsuDraRojas, Roles.Medico, null, DraRojas);

    private void Fijar(int idUsuario, string rol, int? idSede, int? idMedico)
    {
        Contexto.IdUsuario = idUsuario;
        Contexto.Rol = rol;
        Contexto.IdSede = idSede;
        Contexto.IdMedico = idMedico;
    }

    public IUnitOfWork Uow => new UnidadDeTrabajoEnMemoria(Db);
    public IAuditoriaService Auditoria => new AuditoriaService(new AuditoriaRepositorioEnMemoria(Db), Contexto, Reloj);

    public CitaService Citas => new(new CitaRepositorioEnMemoria(Db), new PacienteRepositorioEnMemoria(Db), new MedicoRepositorioEnMemoria(Db), Uow, Reloj, Contexto, Auditoria);
    public MedicoService Medicos => new(new MedicoRepositorioEnMemoria(Db), new CatalogoRepositorioEnMemoria(Db), new CitaRepositorioEnMemoria(Db), Uow, Reloj, Auditoria);
    public PacienteService Pacientes => new(new PacienteRepositorioEnMemoria(Db), Uow, Reloj, Auditoria);
    public CatalogoService Catalogo => new(new CatalogoRepositorioEnMemoria(Db), Uow, Contexto, Auditoria);
    public UsuarioService Usuarios => new(new UsuarioRepositorioEnMemoria(Db), new CatalogoRepositorioEnMemoria(Db), new MedicoRepositorioEnMemoria(Db), Hasher, Uow, Reloj, Auditoria);
    public AuthService Auth => new(new UsuarioRepositorioEnMemoria(Db), Hasher, new GeneradorTokensDePrueba(), Uow, Reloj, Auditoria);
    public ReporteService Reportes => new(new CitaRepositorioEnMemoria(Db), Citas, new IExportadorReporte[] { new ExportadorDePrueba() }, Reloj, Auditoria, Uow);
    public NotificacionService Notificaciones => new(new CitaRepositorioEnMemoria(Db), Correo, Uow, Reloj);
}
