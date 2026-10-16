using VillaSalud.Domain.Entidades;

namespace VillaSalud.UnitTests.Dobles;

/// <summary>
/// "Base de datos" en memoria para las pruebas unitarias: listas simples que los repositorios
/// de prueba comparten. Permite probar las reglas de negocio sin SQL Server.
/// </summary>
public class AlmacenEnMemoria
{
    public List<Sede> Sedes { get; } = new();
    public List<Rol> Roles { get; } = new();
    public List<Usuario> Usuarios { get; } = new();
    public List<RefreshToken> RefreshTokens { get; } = new();
    public List<Especialidad> Especialidades { get; } = new();
    public List<Medico> Medicos { get; } = new();
    public List<Consultorio> Consultorios { get; } = new();
    public List<HorarioMedico> Horarios { get; } = new();
    public List<Paciente> Pacientes { get; } = new();
    public List<Cita> Citas { get; } = new();
    public List<Notificacion> Notificaciones { get; } = new();
    public List<Auditoria> Auditorias { get; } = new();

    public int Guardados { get; private set; }
    public int TransaccionesConfirmadas { get; set; }

    private int _secuencia = 1000;
    private readonly Dictionary<Cita, string> _huellas = new();

    /// <summary>Simula SaveChanges: asigna identificadores, enlaza navegaciones y cambia la versión de las citas modificadas.</summary>
    public void Guardar()
    {
        Guardados++;
        foreach (var x in Sedes.Where(x => x.IdSede == 0)) x.IdSede = ++_secuencia;
        foreach (var x in Usuarios.Where(x => x.IdUsuario == 0)) x.IdUsuario = ++_secuencia;
        foreach (var x in RefreshTokens.Where(x => x.IdToken == 0)) x.IdToken = ++_secuencia;
        foreach (var x in Especialidades.Where(x => x.IdEspecialidad == 0)) x.IdEspecialidad = ++_secuencia;
        foreach (var x in Medicos.Where(x => x.IdMedico == 0)) x.IdMedico = ++_secuencia;
        foreach (var x in Consultorios.Where(x => x.IdConsultorio == 0)) x.IdConsultorio = ++_secuencia;
        foreach (var x in Horarios.Where(x => x.IdHorario == 0)) x.IdHorario = ++_secuencia;
        foreach (var x in Pacientes.Where(x => x.IdPaciente == 0)) x.IdPaciente = ++_secuencia;
        foreach (var x in Pacientes.SelectMany(p => p.Consentimientos.Where(c => c.IdConsentimiento == 0).Select(c => (p, c))))
        {
            x.c.IdConsentimiento = ++_secuencia;
            x.c.IdPaciente = x.p.IdPaciente;
        }
        foreach (var x in Citas.Where(x => x.IdCita == 0)) x.IdCita = ++_secuencia;
        foreach (var cita in Citas)
        {
            foreach (var h in cita.Historial.Where(h => h.IdHistorial == 0))
            {
                h.IdHistorial = ++_secuencia;
                h.IdCita = cita.IdCita;
            }
            var huella = $"{cita.IdEstadoCita}|{cita.Fecha}|{cita.HoraInicio}|{cita.IdConsultorio}|{cita.Observacion}";
            if (!_huellas.TryGetValue(cita, out var anterior) || anterior != huella)
            {
                _huellas[cita] = huella;
                cita.RowVersion = BitConverter.GetBytes((long)++_secuencia);
            }
        }
        foreach (var x in Notificaciones.Where(x => x.IdNotificacion == 0)) x.IdNotificacion = ++_secuencia;
        foreach (var x in Auditorias.Where(x => x.IdAuditoria == 0)) x.IdAuditoria = ++_secuencia;
        Enlazar();
    }

    /// <summary>Equivale a los Include de EF Core: completa las propiedades de navegación.</summary>
    public void Enlazar()
    {
        foreach (var u in Usuarios)
        {
            u.Rol = Roles.FirstOrDefault(r => r.IdRol == u.IdRol);
            u.Sede = Sedes.FirstOrDefault(s => s.IdSede == u.IdSede);
            u.Medico = Medicos.FirstOrDefault(m => m.IdMedico == u.IdMedico);
        }
        foreach (var t in RefreshTokens) t.Usuario = Usuarios.FirstOrDefault(u => u.IdUsuario == t.IdUsuario);
        foreach (var m in Medicos) m.Especialidad = Especialidades.FirstOrDefault(e => e.IdEspecialidad == m.IdEspecialidad);
        foreach (var c in Consultorios) c.Sede = Sedes.FirstOrDefault(s => s.IdSede == c.IdSede);
        foreach (var h in Horarios)
        {
            h.Medico = Medicos.FirstOrDefault(m => m.IdMedico == h.IdMedico);
            h.Consultorio = Consultorios.FirstOrDefault(c => c.IdConsultorio == h.IdConsultorio);
        }
        foreach (var c in Citas)
        {
            c.Paciente = Pacientes.FirstOrDefault(p => p.IdPaciente == c.IdPaciente);
            c.Medico = Medicos.FirstOrDefault(m => m.IdMedico == c.IdMedico);
            c.Consultorio = Consultorios.FirstOrDefault(x => x.IdConsultorio == c.IdConsultorio);
            c.Sede = Sedes.FirstOrDefault(s => s.IdSede == c.IdSede);
            foreach (var h in c.Historial) h.Usuario = Usuarios.FirstOrDefault(u => u.IdUsuario == h.IdUsuario);
        }
        foreach (var n in Notificaciones) n.Cita = Citas.FirstOrDefault(c => c.IdCita == n.IdCita);
        foreach (var a in Auditorias) a.Usuario = Usuarios.FirstOrDefault(u => u.IdUsuario == a.IdUsuario);
    }
}
