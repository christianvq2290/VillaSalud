using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Application.Servicios;

/// <summary>Conversión de entidades a DTO. Las entidades del dominio nunca salen de la API.</summary>
public static class Mapeos
{
    private static readonly string[] Dias = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

    public static string NombreDia(byte dia) => dia < Dias.Length ? Dias[dia] : "";

    public static SedeDto ADto(this Sede s) => new()
    {
        IdSede = s.IdSede, Nombre = s.Nombre, Direccion = s.Direccion, Distrito = s.Distrito, Estado = s.Estado
    };

    public static EspecialidadDto ADto(this Especialidad e) => new() { IdEspecialidad = e.IdEspecialidad, Nombre = e.Nombre };

    public static ConsultorioDto ADto(this Consultorio c) => new()
    {
        IdConsultorio = c.IdConsultorio, IdSede = c.IdSede, Sede = c.Sede?.Nombre, Numero = c.Numero, Piso = c.Piso, Estado = c.Estado
    };

    public static MedicoDto ADto(this Medico m) => new()
    {
        IdMedico = m.IdMedico, CodigoColegiatura = m.CodigoColegiatura, Nombres = m.Nombres, Apellidos = m.Apellidos,
        IdEspecialidad = m.IdEspecialidad, Especialidad = m.Especialidad?.Nombre, Telefono = m.Telefono, Email = m.Email, Estado = m.Estado
    };

    public static HorarioDto ADto(this HorarioMedico h) => new()
    {
        IdHorario = h.IdHorario, IdMedico = h.IdMedico, IdConsultorio = h.IdConsultorio,
        Consultorio = h.Consultorio?.Numero, Sede = h.Consultorio?.Sede?.Nombre,
        DiaSemana = h.DiaSemana, Dia = NombreDia(h.DiaSemana),
        HoraInicio = h.HoraInicio, HoraFin = h.HoraFin, DuracionCitaMin = h.DuracionCitaMin
    };

    public static PacienteDto ADto(this Paciente p)
    {
        var vigente = p.Consentimientos.Where(c => !c.Revocado).OrderByDescending(c => c.FechaAceptacion).FirstOrDefault();
        return new PacienteDto
        {
            IdPaciente = p.IdPaciente, Dni = p.Dni, Nombres = p.Nombres, Apellidos = p.Apellidos, FechaNacimiento = p.FechaNacimiento,
            Telefono = p.Telefono, Email = p.Email, Direccion = p.Direccion, TipoSangre = p.TipoSangre, Alergias = p.Alergias,
            ConsentimientoVigente = vigente is not null, AceptaTratamientoDatos = vigente is not null, FechaConsentimiento = vigente?.FechaAceptacion
        };
    }

    public static UsuarioDto ADto(this Usuario u, DateTime ahora) => new()
    {
        IdUsuario = u.IdUsuario, Username = u.Username, IdRol = u.IdRol, Rol = u.Rol?.Nombre ?? "",
        IdSede = u.IdSede, Sede = u.Sede?.Nombre, IdMedico = u.IdMedico, Medico = u.Medico?.NombreCompleto,
        Estado = u.Estado, Bloqueado = u.EstaBloqueado(ahora), DebeCambiarClave = u.DebeCambiarClave
    };

    public static CitaDto ADto(this Cita c)
    {
        var dto = new CitaDto();
        Llenar(dto, c);
        return dto;
    }

    public static CitaDetalleDto ADetalleDto(this Cita c)
    {
        var dto = new CitaDetalleDto();
        Llenar(dto, c);
        dto.Historial = c.Historial.OrderBy(h => h.Fecha).ThenBy(h => h.IdHistorial).Select(h => new HistorialCitaDto
        {
            Fecha = h.Fecha,
            EstadoAnterior = h.IdEstadoAnterior.HasValue ? EstadosCita.Nombre(h.IdEstadoAnterior.Value) : null,
            EstadoNuevo = EstadosCita.Nombre(h.IdEstadoNuevo),
            Motivo = h.Motivo,
            Usuario = h.Usuario?.Username
        }).ToList();
        return dto;
    }

    private static void Llenar(CitaDto dto, Cita c)
    {
        dto.IdCita = c.IdCita;
        dto.Codigo = c.Codigo;
        dto.IdSede = c.IdSede;
        dto.Sede = c.Sede?.Nombre ?? "";
        dto.IdPaciente = c.IdPaciente;
        dto.Paciente = c.Paciente?.NombreCompleto ?? "";
        dto.DniPaciente = c.Paciente?.Dni ?? "";
        dto.IdMedico = c.IdMedico;
        dto.Medico = c.Medico?.NombreCompleto ?? "";
        dto.Especialidad = c.Medico?.Especialidad?.Nombre ?? "";
        dto.IdConsultorio = c.IdConsultorio;
        dto.Consultorio = c.Consultorio?.Numero ?? "";
        dto.Fecha = c.Fecha;
        dto.HoraInicio = c.HoraInicio;
        dto.HoraFin = c.HoraFin;
        dto.MotivoConsulta = c.MotivoConsulta;
        dto.IdEstadoCita = c.IdEstadoCita;
        dto.Estado = EstadosCita.Nombre(c.IdEstadoCita);
        dto.Observacion = c.Observacion;
        dto.FechaRegistro = c.FechaRegistro;
        dto.RowVersion = Convert.ToBase64String(c.RowVersion);
        dto.Activa = c.EstaActiva;
    }
}
