using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;

namespace VillaSalud.Application.Servicios;

public class MedicoService
{
    private readonly IMedicoRepositorio _medicos;
    private readonly ICatalogoRepositorio _catalogo;
    private readonly ICitaRepositorio _citas;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;
    private readonly IAuditoriaService _auditoria;

    public MedicoService(IMedicoRepositorio medicos, ICatalogoRepositorio catalogo, ICitaRepositorio citas,
        IUnitOfWork uow, IReloj reloj, IAuditoriaService auditoria)
    {
        _medicos = medicos;
        _catalogo = catalogo;
        _citas = citas;
        _uow = uow;
        _reloj = reloj;
        _auditoria = auditoria;
    }

    public async Task<List<MedicoDto>> ListarAsync(bool soloActivos = false, CancellationToken ct = default) =>
        (await _medicos.ListarAsync(soloActivos, ct)).Select(m => m.ADto()).ToList();

    public async Task<MedicoDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await _medicos.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Médico no encontrado.")).ADto();

    public async Task<MedicoDto> GuardarAsync(int id, MedicoDto dto, CancellationToken ct = default)
    {
        var especialidad = await _catalogo.ObtenerEspecialidadAsync(dto.IdEspecialidad, ct)
                           ?? throw new ReglaNegocioException("La especialidad seleccionada no existe.");
        var codigo = dto.CodigoColegiatura.Trim();
        if (await _medicos.ExisteColegiaturaAsync(codigo, id == 0 ? null : id, ct))
            throw new ReglaNegocioException("El código de colegiatura ya está registrado.");

        Medico medico;
        if (id == 0)
        {
            medico = new Medico();
            _medicos.Agregar(medico);
        }
        else
        {
            medico = await _medicos.ObtenerAsync(id, ct) ?? throw new NoEncontradoException("Médico no encontrado.");
        }
        medico.CodigoColegiatura = codigo;
        medico.Nombres = dto.Nombres.Trim();
        medico.Apellidos = dto.Apellidos.Trim();
        medico.IdEspecialidad = dto.IdEspecialidad;
        medico.Especialidad = especialidad;
        medico.Telefono = Limpiar(dto.Telefono);
        medico.Email = Limpiar(dto.Email);
        medico.Estado = dto.Estado;
        await _uow.GuardarAsync(ct);
        _auditoria.Registrar(id == 0 ? "CREAR" : "MODIFICAR", "MEDICO", medico.IdMedico.ToString(), new { medico.CodigoColegiatura, medico.Estado });
        await _uow.GuardarAsync(ct);
        return medico.ADto();
    }

    // ---------- Horarios ----------
    public async Task<List<HorarioDto>> ListarHorariosAsync(int idMedico, CancellationToken ct = default) =>
        (await _medicos.ListarHorariosAsync(idMedico, ct))
            .Where(h => h.Vigente)
            .OrderBy(h => h.DiaSemana == 0 ? 7 : h.DiaSemana).ThenBy(h => h.HoraInicio)
            .Select(h => h.ADto()).ToList();

    public async Task<HorarioDto> AgregarHorarioAsync(int idMedico, HorarioDto dto, CancellationToken ct = default)
    {
        var medico = await _medicos.ObtenerAsync(idMedico, ct) ?? throw new NoEncontradoException("Médico no encontrado.");
        if (!medico.Estado) throw new ReglaNegocioException("El médico está inactivo.");
        if (dto.HoraFin <= dto.HoraInicio) throw new ReglaNegocioException("La hora de fin debe ser posterior a la hora de inicio.");
        if ((dto.HoraFin - dto.HoraInicio).TotalMinutes < dto.DuracionCitaMin)
            throw new ReglaNegocioException("El bloque es más corto que la duración de una cita.");

        var consultorio = await _catalogo.ObtenerConsultorioAsync(dto.IdConsultorio, ct)
                          ?? throw new ReglaNegocioException("El consultorio seleccionado no existe.");
        if (!consultorio.Estado) throw new ReglaNegocioException("El consultorio está inactivo.");

        var propios = await _medicos.ListarHorariosAsync(idMedico, ct);
        if (propios.Any(h => h.Vigente && h.SeSolapaCon(dto.DiaSemana, dto.HoraInicio, dto.HoraFin)))
            throw new ConflictoException("El médico ya tiene un horario que se cruza con ese bloque.");

        var delConsultorio = await _medicos.ListarHorariosDeConsultorioAsync(dto.IdConsultorio, dto.DiaSemana, ct);
        if (delConsultorio.Any(h => h.IdMedico != idMedico && h.SeSolapaCon(dto.DiaSemana, dto.HoraInicio, dto.HoraFin)))
            throw new ConflictoException("El consultorio ya está asignado a otro médico en ese bloque.");

        var horario = new HorarioMedico
        {
            IdMedico = idMedico, IdConsultorio = dto.IdConsultorio, Consultorio = consultorio,
            DiaSemana = dto.DiaSemana, HoraInicio = dto.HoraInicio, HoraFin = dto.HoraFin,
            DuracionCitaMin = dto.DuracionCitaMin, Vigente = true
        };
        _medicos.AgregarHorario(horario);
        await _uow.GuardarAsync(ct);
        _auditoria.Registrar("CREAR", "HORARIO_MEDICO", horario.IdHorario.ToString(), new { idMedico, dto.DiaSemana, dto.HoraInicio, dto.HoraFin });
        await _uow.GuardarAsync(ct);
        return horario.ADto();
    }

    /// <summary>Los horarios no se borran: se marcan como no vigentes para conservar la trazabilidad de las citas ya dadas.</summary>
    public async Task QuitarHorarioAsync(int idMedico, int idHorario, CancellationToken ct = default)
    {
        var horario = await _medicos.ObtenerHorarioAsync(idHorario, ct);
        if (horario is null || horario.IdMedico != idMedico) throw new NoEncontradoException("Horario no encontrado.");
        horario.Vigente = false;
        _auditoria.Registrar("ELIMINAR", "HORARIO_MEDICO", idHorario.ToString());
        await _uow.GuardarAsync(ct);
    }

    /// <summary>Intervalos del médico en la fecha, marcando cuáles siguen libres (RN03, RN04, RN06).</summary>
    public async Task<List<IntervaloDto>> DisponibilidadAsync(int idMedico, DateOnly fecha, CancellationToken ct = default)
    {
        var horarios = (await _medicos.ListarHorariosAsync(idMedico, ct)).Where(h => h.AplicaA(fecha)).ToList();
        if (horarios.Count == 0) return new List<IntervaloDto>();

        var idsConsultorio = horarios.Select(h => h.IdConsultorio).Distinct().ToList();
        var ocupadas = await _citas.ListarOcupadasAsync(idMedico, idsConsultorio, fecha, ct);
        var ahora = _reloj.Ahora;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);

        var resultado = new List<IntervaloDto>();
        foreach (var horario in horarios.OrderBy(h => h.HoraInicio))
        {
            foreach (var (inicio, fin) in horario.GenerarIntervalos())
            {
                var pasado = fecha < hoy || (fecha == hoy && inicio <= horaActual);
                var ocupado = ocupadas.Any(c => c.SeCruzaCon(fecha, inicio, fin));
                resultado.Add(new IntervaloDto
                {
                    HoraInicio = inicio, HoraFin = fin, IdConsultorio = horario.IdConsultorio,
                    Consultorio = horario.Consultorio?.Numero ?? "", Disponible = !pasado && !ocupado
                });
            }
        }
        return resultado;
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
