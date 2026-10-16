using System.Security.Cryptography;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

public class CitaService
{
    private const string MensajeCruce = "El médico, el consultorio o el paciente ya tiene una cita en ese horario.";

    private readonly ICitaRepositorio _citas;
    private readonly IPacienteRepositorio _pacientes;
    private readonly IMedicoRepositorio _medicos;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;
    private readonly IContextoUsuario _contexto;
    private readonly IAuditoriaService _auditoria;

    public CitaService(ICitaRepositorio citas, IPacienteRepositorio pacientes, IMedicoRepositorio medicos,
        IUnitOfWork uow, IReloj reloj, IContextoUsuario contexto, IAuditoriaService auditoria)
    {
        _citas = citas;
        _pacientes = pacientes;
        _medicos = medicos;
        _uow = uow;
        _reloj = reloj;
        _contexto = contexto;
        _auditoria = auditoria;
    }

    /// <summary>
    /// Programa una cita. Las reglas RN02 a RN06 se verifican dentro de una transacción serializable,
    /// de modo que dos usuarios que eligen el mismo intervalo a la vez no puedan registrarlo ambos.
    /// </summary>
    public async Task<CitaDto> ProgramarAsync(ProgramarCitaDto dto, CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;
        ValidarNoPasada(dto.Fecha, dto.HoraInicio, ahora);                                   // RN06

        var paciente = await _pacientes.ObtenerAsync(dto.IdPaciente, ct)
                       ?? throw new ReglaNegocioException("El paciente no existe.");
        if (!paciente.TieneConsentimientoVigente)                                            // RN02
            throw new ReglaNegocioException("El paciente no registra un consentimiento vigente para el tratamiento de sus datos.");

        var medico = await _medicos.ObtenerAsync(dto.IdMedico, ct)
                     ?? throw new ReglaNegocioException("El médico no existe.");
        if (!medico.Estado) throw new ReglaNegocioException("El médico está inactivo.");

        var (horario, horaFin) = await BuscarIntervaloAsync(dto.IdMedico, dto.Fecha, dto.HoraInicio, ct);   // RN03
        ValidarSede(horario.Consultorio!.IdSede);                                             // RN01

        await using var transaccion = await _uow.IniciarTransaccionAsync(serializable: true, ct);

        if (await _citas.ExisteCruceAsync(dto.IdMedico, horario.IdConsultorio, dto.IdPaciente,
                dto.Fecha, dto.HoraInicio, horaFin, null, ct))                                // RN04 y RN05
            throw new ConflictoException(MensajeCruce);

        var cita = new Cita
        {
            Codigo = GenerarCodigo(dto.Fecha),
            IdSede = horario.Consultorio.IdSede,
            IdPaciente = dto.IdPaciente,
            IdMedico = dto.IdMedico,
            IdConsultorio = horario.IdConsultorio,
            Fecha = dto.Fecha,
            HoraInicio = dto.HoraInicio,
            HoraFin = horaFin,
            MotivoConsulta = string.IsNullOrWhiteSpace(dto.MotivoConsulta) ? null : dto.MotivoConsulta.Trim()
        };
        cita.RegistrarCreacion(_contexto.IdUsuario, ahora);
        _citas.Agregar(cita);
        await _uow.GuardarAsync(ct);          // inserta CITA e HISTORIAL_CITA; el índice único respalda la RN04

        if (!string.IsNullOrWhiteSpace(paciente.Email))
        {
            _citas.AgregarNotificacion(new Notificacion
            {
                IdCita = cita.IdCita, Tipo = Notificacion.TipoConfirmacion, Destino = paciente.Email, FechaProgramada = ahora
            });
        }
        _auditoria.Registrar("PROGRAMAR", "CITA", cita.IdCita.ToString(), new { cita.Codigo, cita.IdMedico, cita.Fecha, cita.HoraInicio });
        await _uow.GuardarAsync(ct);
        await transaccion.ConfirmarAsync(ct);

        return await ObtenerAsync(cita.IdCita, ct);
    }

    public async Task<CitaDto> ReprogramarAsync(int id, ReprogramarCitaDto dto, CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;
        var cita = await CargarAsync(id, ct);
        ValidarVersion(cita, dto.RowVersion);                                                 // RN09
        ValidarNoPasada(dto.Fecha, dto.HoraInicio, ahora);

        var (horario, horaFin) = await BuscarIntervaloAsync(cita.IdMedico, dto.Fecha, dto.HoraInicio, ct);
        ValidarSede(horario.Consultorio!.IdSede);

        await using var transaccion = await _uow.IniciarTransaccionAsync(serializable: true, ct);

        if (await _citas.ExisteCruceAsync(cita.IdMedico, horario.IdConsultorio, cita.IdPaciente,
                dto.Fecha, dto.HoraInicio, horaFin, cita.IdCita, ct))
            throw new ConflictoException(MensajeCruce);

        cita.Reprogramar(dto.Fecha, dto.HoraInicio, horaFin, horario.IdConsultorio, dto.Motivo, _contexto.IdUsuario, ahora);   // RN07 y RN08
        cita.IdSede = horario.Consultorio.IdSede;
        cita.Consultorio = horario.Consultorio;

        if (!string.IsNullOrWhiteSpace(cita.Paciente?.Email))
        {
            _citas.AgregarNotificacion(new Notificacion
            {
                IdCita = cita.IdCita, Tipo = Notificacion.TipoConfirmacion, Destino = cita.Paciente.Email, FechaProgramada = ahora
            });
        }
        _auditoria.Registrar("REPROGRAMAR", "CITA", id.ToString(), new { dto.Fecha, dto.HoraInicio, dto.Motivo });
        await _uow.GuardarAsync(ct);
        await transaccion.ConfirmarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<CitaDto> CancelarAsync(int id, CancelarCitaDto dto, CancellationToken ct = default)
    {
        var cita = await CargarAsync(id, ct);
        ValidarVersion(cita, dto.RowVersion);
        cita.Cancelar(dto.Motivo, _contexto.IdUsuario, _reloj.Ahora);                         // RN07 y RN08
        _auditoria.Registrar("CANCELAR", "CITA", id.ToString(), new { dto.Motivo });
        await _uow.GuardarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    /// <summary>El médico registra la atención o la inasistencia de una cita propia.</summary>
    public async Task<CitaDto> RegistrarAtencionAsync(int id, AtenderCitaDto dto, CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;
        var cita = await CargarAsync(id, ct);
        if (cita.Fecha > DateOnly.FromDateTime(ahora))
            throw new ReglaNegocioException("No se puede registrar la atención de una cita futura.");

        if (dto.Asistio) cita.MarcarAtendida(dto.Observacion, _contexto.IdUsuario, ahora);
        else cita.MarcarNoAsistio(_contexto.IdUsuario, ahora);

        _auditoria.Registrar(dto.Asistio ? "ATENDER" : "NO_ASISTIO", "CITA", id.ToString());
        await _uow.GuardarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<CitaDetalleDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await CargarAsync(id, ct)).ADetalleDto();

    public async Task<List<CitaDto>> ListarAsync(FiltroCitasDto filtro, CancellationToken ct = default)
    {
        var citas = await _citas.ListarAsync(AplicarAlcance(filtro), ct);
        return citas.OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).Select(c => c.ADto()).ToList();
    }

    /// <summary>Agenda del médico autenticado para una fecha.</summary>
    public async Task<List<CitaDto>> AgendaAsync(DateOnly fecha, int? idMedico = null, CancellationToken ct = default)
    {
        var medico = _contexto.IdMedico ?? idMedico
                     ?? throw new ReglaNegocioException("Indique el médico cuya agenda desea consultar.");
        return await ListarAsync(new FiltroCitasDto { Desde = fecha, Hasta = fecha, IdMedico = medico }, ct);
    }

    /// <summary>RN10: consulta del asistente virtual. Devuelve nulo si el DNI y el código no coinciden.</summary>
    public async Task<EstadoCitaPublicoDto?> EstadoPublicoAsync(string? dni, string? codigo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(codigo)) return null;
        var cita = await _citas.ObtenerPorCodigoAsync(codigo.Trim().ToUpperInvariant(), ct);
        if (cita?.Paciente is null || cita.Paciente.Dni != dni.Trim()) return null;
        return new EstadoCitaPublicoDto
        {
            Codigo = cita.Codigo, Fecha = cita.Fecha, HoraInicio = cita.HoraInicio,
            Medico = cita.Medico?.NombreCompleto ?? "", Consultorio = cita.Consultorio?.Numero ?? "",
            Sede = cita.Sede?.Nombre ?? "", Estado = EstadosCita.Nombre(cita.IdEstadoCita)
        };
    }

    // ------------------------------------------------------------------ apoyo

    /// <summary>RN01: limita la consulta a la sede del usuario y, si es médico, a sus propias citas.</summary>
    internal FiltroCitas AplicarAlcance(FiltroCitasDto filtro) => new()
    {
        Desde = filtro.Desde,
        Hasta = filtro.Hasta,
        IdSede = _contexto.IdSede ?? filtro.IdSede,
        IdMedico = _contexto.Rol == Roles.Medico ? _contexto.IdMedico ?? -1 : filtro.IdMedico,
        IdEspecialidad = filtro.IdEspecialidad,
        IdEstadoCita = filtro.IdEstadoCita,
        IdPaciente = filtro.IdPaciente
    };

    private async Task<Cita> CargarAsync(int id, CancellationToken ct)
    {
        var cita = await _citas.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Cita no encontrada.");
        ValidarSede(cita.IdSede);
        if (_contexto.Rol == Roles.Medico && cita.IdMedico != _contexto.IdMedico)
            throw new AccesoDenegadoException("La cita pertenece a otro médico.");
        return cita;
    }

    // RN03: la hora elegida debe ser el inicio de un intervalo de un horario vigente del médico para ese día.
    private async Task<(HorarioMedico Horario, TimeOnly HoraFin)> BuscarIntervaloAsync(int idMedico, DateOnly fecha, TimeOnly horaInicio, CancellationToken ct)
    {
        var horarios = await _medicos.ListarHorariosAsync(idMedico, ct);
        foreach (var horario in horarios.Where(h => h.AplicaA(fecha)))
        {
            foreach (var (inicio, fin) in horario.GenerarIntervalos())
            {
                if (inicio == horaInicio)
                {
                    if (horario.Consultorio is null || !horario.Consultorio.Estado)
                        throw new ReglaNegocioException("El consultorio asignado a ese horario está inactivo.");
                    return (horario, fin);
                }
            }
        }
        throw new ReglaNegocioException("El médico no atiende en el horario solicitado.");
    }

    private void ValidarSede(int idSede)
    {
        if (_contexto.IdSede.HasValue && _contexto.IdSede.Value != idSede)
            throw new AccesoDenegadoException("La operación corresponde a otra sede.");
    }

    private static void ValidarNoPasada(DateOnly fecha, TimeOnly hora, DateTime ahora)
    {
        if (fecha.ToDateTime(hora) <= ahora)
            throw new ReglaNegocioException("No se pueden programar citas en fechas u horas pasadas.");
    }

    private static void ValidarVersion(Cita cita, string? rowVersion)
    {
        if (string.IsNullOrEmpty(rowVersion)) return;
        if (rowVersion != Convert.ToBase64String(cita.RowVersion))
            throw new ConflictoException("La cita fue modificada por otro usuario. Vuelva a cargarla e intente de nuevo.");
    }

    // Código legible para el paciente: C + fecha + 4 caracteres aleatorios (sin 0/O ni 1/I para evitar confusiones).
    private static string GenerarCodigo(DateOnly fecha)
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sufijo = new char[4];
        for (var i = 0; i < sufijo.Length; i++) sufijo[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
        return $"C{fecha:yyMMdd}-{new string(sufijo)}";
    }
}
