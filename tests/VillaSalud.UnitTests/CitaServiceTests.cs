using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;
using Xunit;

namespace VillaSalud.UnitTests;

public class CitaServiceTests
{
    private readonly Escenario _e = new();

    private static ProgramarCitaDto Solicitud(int paciente = Escenario.PacienteAna, int medico = Escenario.DraRojas, int hora = 9, int minuto = 0, DateOnly? fecha = null) =>
        new() { IdPaciente = paciente, IdMedico = medico, Fecha = fecha ?? Escenario.Lunes, HoraInicio = new(hora, minuto), MotivoConsulta = "Control" };

    [Fact]
    public async Task Programar_en_un_intervalo_libre_registra_la_cita_pendiente()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());

        Assert.Equal("Pendiente", cita.Estado);
        Assert.Equal(new TimeOnly(9, 30), cita.HoraFin);            // el fin sale de la duración del horario
        Assert.Equal("101", cita.Consultorio);                      // el consultorio sale del horario del médico
        Assert.Equal(Escenario.SedeVes, cita.IdSede);
        Assert.StartsWith("C261109-", cita.Codigo);
        Assert.Equal(1, _e.Db.TransaccionesConfirmadas);
    }

    [Fact]
    public async Task Programar_deja_historial_auditoria_y_confirmacion_por_correo()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());

        var guardada = Assert.Single(_e.Db.Citas);
        Assert.Single(guardada.Historial);
        Assert.Contains(_e.Db.Auditorias, a => a.Accion == "PROGRAMAR" && a.IdEntidad == cita.IdCita.ToString() && a.IdUsuario == Escenario.UsuRecepVes);
        var notificacion = Assert.Single(_e.Db.Notificaciones);
        Assert.Equal("ana@correo.pe", notificacion.Destino);
        Assert.Equal(Notificacion.TipoConfirmacion, notificacion.Tipo);
    }

    [Fact]
    public async Task RN04_no_se_registran_dos_citas_del_mismo_medico_en_el_mismo_intervalo()
    {
        await _e.Citas.ProgramarAsync(Solicitud());

        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _e.Citas.ProgramarAsync(Solicitud(paciente: Escenario.PacienteLuis)));

        Assert.Contains("ya tiene una cita", ex.Message);
        Assert.Single(_e.Db.Citas);
    }

    [Fact]
    public async Task RN05_un_paciente_no_puede_tener_dos_citas_a_la_misma_hora_con_medicos_distintos()
    {
        await _e.Citas.ProgramarAsync(Solicitud(medico: Escenario.DraRojas));

        await Assert.ThrowsAsync<ConflictoException>(() => _e.Citas.ProgramarAsync(Solicitud(medico: Escenario.DrPaz)));
    }

    [Theory]
    [InlineData(13, 0)]   // fuera del bloque 08:00-12:00
    [InlineData(9, 10)]   // dentro del bloque, pero no es inicio de intervalo
    public async Task RN03_solo_se_programa_en_un_intervalo_del_horario_del_medico(int hora, int minuto)
    {
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.ProgramarAsync(Solicitud(hora: hora, minuto: minuto)));

        Assert.Contains("no atiende", ex.Message);
    }

    [Fact]
    public async Task RN03_el_medico_no_atiende_ese_dia_de_la_semana()
    {
        var miercoles = Escenario.Lunes.AddDays(2);

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.ProgramarAsync(Solicitud(fecha: miercoles)));
    }

    [Fact]
    public async Task RN02_no_se_programa_sin_consentimiento_vigente()
    {
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.ProgramarAsync(Solicitud(paciente: Escenario.PacienteSinConsentimiento)));

        Assert.Contains("consentimiento", ex.Message);
    }

    [Fact]
    public async Task RN06_no_se_programa_en_una_hora_que_ya_paso()
    {
        _e.Reloj.Ahora = new DateTime(2026, 11, 9, 9, 5, 0);   // ya pasaron las 09:00 del lunes

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.ProgramarAsync(Solicitud(hora: 9)));
    }

    [Fact]
    public async Task RN01_la_recepcion_de_una_sede_no_programa_en_consultorios_de_otra()
    {
        _e.ComoRecepcionistaVes();

        // El Dr. Paz atiende a las 14:00 en el consultorio 201, que es de la sede San Juan
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _e.Citas.ProgramarAsync(Solicitud(medico: Escenario.DrPaz, hora: 14)));
    }

    [Fact]
    public async Task RN01_cada_recepcion_solo_lista_las_citas_de_su_sede()
    {
        _e.ComoAdministrador();
        await _e.Citas.ProgramarAsync(Solicitud(medico: Escenario.DraRojas, hora: 9));                                   // sede VES
        await _e.Citas.ProgramarAsync(Solicitud(paciente: Escenario.PacienteLuis, medico: Escenario.DrPaz, hora: 14));   // sede SJM

        _e.ComoRecepcionistaSjm();
        var citas = await _e.Citas.ListarAsync(new FiltroCitasDto());

        var unica = Assert.Single(citas);
        Assert.Equal(Escenario.SedeSjm, unica.IdSede);
    }

    [Fact]
    public async Task Cancelar_libera_el_intervalo_para_otro_paciente()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());
        await _e.Citas.CancelarAsync(cita.IdCita, new CancelarCitaDto { Motivo = "El paciente no podrá asistir" });

        var nueva = await _e.Citas.ProgramarAsync(Solicitud(paciente: Escenario.PacienteLuis));

        Assert.Equal("Pendiente", nueva.Estado);
        Assert.Equal(2, _e.Db.Citas.Count);
    }

    [Fact]
    public async Task Reprogramar_mueve_la_cita_y_la_deja_en_estado_reprogramada()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());

        var movida = await _e.Citas.ReprogramarAsync(cita.IdCita, new ReprogramarCitaDto { Fecha = Escenario.Martes, HoraInicio = new(8, 30), Motivo = "A pedido del paciente", RowVersion = cita.RowVersion });

        Assert.Equal("Reprogramada", movida.Estado);
        Assert.Equal(Escenario.Martes, movida.Fecha);
        Assert.Equal(new TimeOnly(9, 0), movida.HoraFin);
        Assert.NotEqual(cita.RowVersion, movida.RowVersion);
    }

    [Fact]
    public async Task Reprogramar_al_mismo_intervalo_de_la_propia_cita_no_se_considera_cruce()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());

        var movida = await _e.Citas.ReprogramarAsync(cita.IdCita, new ReprogramarCitaDto { Fecha = Escenario.Lunes, HoraInicio = new(9, 0), Motivo = "Confirmación de horario" });

        Assert.Equal("Reprogramada", movida.Estado);
    }

    [Fact]
    public async Task RN09_si_otro_usuario_modifico_la_cita_el_segundo_recibe_conflicto()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());
        var versionEnPantalla = cita.RowVersion;
        await _e.Citas.ReprogramarAsync(cita.IdCita, new ReprogramarCitaDto { Fecha = Escenario.Martes, HoraInicio = new(8, 0), Motivo = "Primer usuario", RowVersion = versionEnPantalla });

        var ex = await Assert.ThrowsAsync<ConflictoException>(() =>
            _e.Citas.CancelarAsync(cita.IdCita, new CancelarCitaDto { Motivo = "Segundo usuario", RowVersion = versionEnPantalla }));

        Assert.Contains("modificada por otro usuario", ex.Message);
    }

    [Fact]
    public async Task El_medico_registra_la_atencion_de_su_cita()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());
        _e.Reloj.Ahora = new DateTime(2026, 11, 9, 9, 40, 0);
        _e.ComoDraRojas();

        var atendida = await _e.Citas.RegistrarAtencionAsync(cita.IdCita, new AtenderCitaDto { Asistio = true, Observacion = "Control en 30 días" });

        Assert.Equal("Atendida", atendida.Estado);
        Assert.False(atendida.Activa);
    }

    [Fact]
    public async Task Un_medico_no_accede_a_la_cita_de_otro_medico()
    {
        _e.ComoAdministrador();
        var cita = await _e.Citas.ProgramarAsync(Solicitud(medico: Escenario.DrPaz));
        _e.ComoDraRojas();

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _e.Citas.ObtenerAsync(cita.IdCita));
    }

    [Fact]
    public async Task No_se_registra_la_atencion_de_una_cita_futura()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud(fecha: Escenario.Martes, hora: 8));
        _e.ComoDraRojas();

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.RegistrarAtencionAsync(cita.IdCita, new AtenderCitaDto()));
    }

    [Fact]
    public async Task RN10_el_asistente_solo_informa_si_el_dni_y_el_codigo_coinciden()
    {
        var cita = await _e.Citas.ProgramarAsync(Solicitud());

        var correcto = await _e.Citas.EstadoPublicoAsync("40000001", cita.Codigo.ToLowerInvariant());
        var dniAjeno = await _e.Citas.EstadoPublicoAsync("40000003", cita.Codigo);
        var codigoFalso = await _e.Citas.EstadoPublicoAsync("40000001", "C261109-ZZZZ");

        Assert.NotNull(correcto);
        Assert.Equal("Elena Rojas", correcto!.Medico);
        Assert.Null(dniAjeno);
        Assert.Null(codigoFalso);
    }
}
