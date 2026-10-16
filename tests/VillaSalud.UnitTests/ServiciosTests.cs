using VillaSalud.Application.Dtos;
using VillaSalud.Application.Servicios;
using VillaSalud.Domain.Entidades;
using VillaSalud.Domain.Excepciones;
using Xunit;

namespace VillaSalud.UnitTests;

public class AuthServiceTests
{
    private readonly Escenario _e = new();

    [Fact]
    public async Task Login_correcto_entrega_token_de_acceso_y_de_renovacion()
    {
        var sesion = await _e.Auth.LoginAsync(new LoginDto { Username = "recepcion.ves", Password = Escenario.ClaveValida });

        Assert.NotEmpty(sesion.TokenAcceso);
        Assert.NotEmpty(sesion.RefreshToken);
        Assert.Equal(Roles.Recepcionista, sesion.Rol);
        Assert.Equal(Escenario.SedeVes, sesion.IdSede);
        // en la base solo queda el hash del token de renovación
        Assert.DoesNotContain(_e.Db.RefreshTokens, t => t.TokenHash == sesion.RefreshToken);
        Assert.Contains(_e.Db.Auditorias, a => a.Accion == "LOGIN");
    }

    [Fact]
    public async Task Cinco_claves_incorrectas_bloquean_la_cuenta_aunque_luego_la_clave_sea_correcta()
    {
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = "incorrecta" }));

        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = Escenario.ClaveValida }));
        Assert.Contains("bloqueada", ex.Message);

        _e.Reloj.Ahora = _e.Reloj.Ahora.AddMinutes(16);
        var sesion = await _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = Escenario.ClaveValida });
        Assert.NotEmpty(sesion.TokenAcceso);
    }

    [Fact]
    public async Task Usuario_inexistente_e_inactivo_reciben_el_mismo_mensaje()
    {
        _e.Db.Usuarios.Single(u => u.Username == "erojas").Estado = false;

        var inexistente = await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.LoginAsync(new LoginDto { Username = "nadie", Password = "x" }));
        var inactivo = await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.LoginAsync(new LoginDto { Username = "erojas", Password = Escenario.ClaveValida }));

        Assert.Equal(inexistente.Message, inactivo.Message);
    }

    [Fact]
    public async Task El_token_de_renovacion_solo_sirve_una_vez()
    {
        var sesion = await _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = Escenario.ClaveValida });

        var renovada = await _e.Auth.RefrescarAsync(sesion.RefreshToken);

        Assert.NotEqual(sesion.RefreshToken, renovada.RefreshToken);
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.RefrescarAsync(sesion.RefreshToken));
    }

    [Theory]
    [InlineData("corta1")]          // menos de 8 caracteres
    [InlineData("sololetrasss")]    // sin dígitos
    [InlineData("1234567890")]      // sin letras
    public async Task Cambiar_clave_exige_la_politica_de_contrasenas(string nueva)
    {
        var dto = new CambiarClaveDto { ClaveActual = Escenario.ClaveValida, ClaveNueva = nueva, ConfirmarClave = nueva };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Auth.CambiarClaveAsync(Escenario.UsuAdmin, dto));
    }

    [Fact]
    public async Task Cambiar_clave_revoca_las_sesiones_abiertas()
    {
        var sesion = await _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = Escenario.ClaveValida });

        await _e.Auth.CambiarClaveAsync(Escenario.UsuAdmin, new CambiarClaveDto { ClaveActual = Escenario.ClaveValida, ClaveNueva = "Nueva#2027", ConfirmarClave = "Nueva#2027" });

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _e.Auth.RefrescarAsync(sesion.RefreshToken));
        var nueva = await _e.Auth.LoginAsync(new LoginDto { Username = "admin", Password = "Nueva#2027" });
        Assert.False(nueva.DebeCambiarClave);
    }
}

public class MedicoServiceTests
{
    private readonly Escenario _e = new();

    [Fact]
    public async Task Disponibilidad_marca_como_ocupado_el_intervalo_con_cita()
    {
        await _e.Citas.ProgramarAsync(new ProgramarCitaDto { IdPaciente = Escenario.PacienteAna, IdMedico = Escenario.DraRojas, Fecha = Escenario.Lunes, HoraInicio = new(9, 0) });

        var intervalos = await _e.Medicos.DisponibilidadAsync(Escenario.DraRojas, Escenario.Lunes);

        Assert.Equal(8, intervalos.Count);                                                  // 08:00-12:00 cada 30 minutos
        Assert.False(intervalos.Single(i => i.HoraInicio == new TimeOnly(9, 0)).Disponible);
        Assert.True(intervalos.Single(i => i.HoraInicio == new TimeOnly(9, 30)).Disponible);
    }

    [Fact]
    public async Task Disponibilidad_no_ofrece_intervalos_que_ya_pasaron()
    {
        _e.Reloj.Ahora = new DateTime(2026, 11, 9, 10, 10, 0);

        var intervalos = await _e.Medicos.DisponibilidadAsync(Escenario.DraRojas, Escenario.Lunes);

        Assert.All(intervalos.Where(i => i.HoraInicio <= new TimeOnly(10, 0)), i => Assert.False(i.Disponible));
        Assert.True(intervalos.Single(i => i.HoraInicio == new TimeOnly(10, 30)).Disponible);
    }

    [Fact]
    public async Task Disponibilidad_de_un_dia_sin_horario_esta_vacia()
    {
        Assert.Empty(await _e.Medicos.DisponibilidadAsync(Escenario.DraRojas, Escenario.Lunes.AddDays(2)));
    }

    [Fact]
    public async Task No_se_agrega_un_horario_que_se_cruza_con_otro_del_mismo_medico()
    {
        var dto = new HorarioDto { IdConsultorio = Escenario.Cons102, DiaSemana = 1, HoraInicio = new(11, 0), HoraFin = new(13, 0) };

        await Assert.ThrowsAsync<ConflictoException>(() => _e.Medicos.AgregarHorarioAsync(Escenario.DraRojas, dto));
    }

    [Fact]
    public async Task No_se_asigna_un_consultorio_a_dos_medicos_en_el_mismo_bloque()
    {
        // El 101 ya es de la Dra. Rojas los martes de 08:00 a 10:00
        var dto = new HorarioDto { IdConsultorio = Escenario.Cons101, DiaSemana = 2, HoraInicio = new(9, 0), HoraFin = new(11, 0) };

        await Assert.ThrowsAsync<ConflictoException>(() => _e.Medicos.AgregarHorarioAsync(Escenario.DrPaz, dto));
    }

    [Fact]
    public async Task Quitar_un_horario_lo_deja_no_vigente_y_ya_no_ofrece_intervalos()
    {
        await _e.Medicos.QuitarHorarioAsync(Escenario.DraRojas, 2);

        Assert.Empty(await _e.Medicos.DisponibilidadAsync(Escenario.DraRojas, Escenario.Martes));
        Assert.False(_e.Db.Horarios.Single(h => h.IdHorario == 2).Vigente);
    }
}

public class PacienteYUsuarioTests
{
    private readonly Escenario _e = new();

    [Fact]
    public async Task Registrar_paciente_con_aceptacion_crea_el_consentimiento()
    {
        var dto = new PacienteDto { Dni = "40000099", Nombres = "Rosa", Apellidos = "Lima", AceptaTratamientoDatos = true };

        var paciente = await _e.Pacientes.GuardarAsync(0, dto);

        Assert.True(paciente.ConsentimientoVigente);
        Assert.Equal(_e.Reloj.Ahora, paciente.FechaConsentimiento);
    }

    [Fact]
    public async Task Retirar_la_aceptacion_revoca_el_consentimiento_y_bloquea_nuevas_citas()
    {
        var dto = await _e.Pacientes.ObtenerAsync(Escenario.PacienteAna);
        dto.AceptaTratamientoDatos = false;

        var actualizado = await _e.Pacientes.GuardarAsync(Escenario.PacienteAna, dto);

        Assert.False(actualizado.ConsentimientoVigente);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Citas.ProgramarAsync(new ProgramarCitaDto { IdPaciente = Escenario.PacienteAna, IdMedico = Escenario.DraRojas, Fecha = Escenario.Lunes, HoraInicio = new(9, 0) }));
    }

    [Fact]
    public async Task No_se_registran_dos_pacientes_con_el_mismo_dni()
    {
        var dto = new PacienteDto { Dni = "40000001", Nombres = "Otra", Apellidos = "Persona" };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Pacientes.GuardarAsync(0, dto));
    }

    [Fact]
    public async Task La_auditoria_de_pacientes_no_guarda_datos_de_salud()
    {
        var dto = new PacienteDto { Dni = "40000098", Nombres = "Iris", Apellidos = "Vega", Alergias = "Penicilina", TipoSangre = "O+", AceptaTratamientoDatos = true };

        await _e.Pacientes.GuardarAsync(0, dto);

        var registro = Assert.Single(_e.Db.Auditorias, a => a.Entidad == "PACIENTE");
        Assert.DoesNotContain("Penicilina", registro.ValoresJson);
        Assert.DoesNotContain("O+", registro.ValoresJson);
    }

    [Fact]
    public async Task Un_recepcionista_debe_tener_sede_asignada()
    {
        var dto = new UsuarioGuardarDto { Username = "nuevo.recep", Password = "Clave#2026", IdRol = Escenario.RolRecepcion };

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Usuarios.CrearAsync(dto));
    }

    [Fact]
    public async Task El_usuario_nuevo_guarda_la_clave_con_hash_y_debe_cambiarla()
    {
        var dto = new UsuarioGuardarDto { Username = "nuevo.recep", Password = "Clave#2026", IdRol = Escenario.RolRecepcion, IdSede = Escenario.SedeVes };

        var creado = await _e.Usuarios.CrearAsync(dto);

        var guardado = _e.Db.Usuarios.Single(u => u.IdUsuario == creado.IdUsuario);
        Assert.NotEqual("Clave#2026", guardado.PasswordHash);
        Assert.True(creado.DebeCambiarClave);
    }

    [Fact]
    public async Task No_se_elimina_una_especialidad_con_medicos()
    {
        _e.ComoAdministrador();

        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Catalogo.EliminarEspecialidadAsync(Escenario.Cardiologia));
    }
}

public class ReporteYNotificacionTests
{
    private readonly Escenario _e = new();

    private async Task<CitaDto> CitaAsync(int paciente, int medico, int hora, DateOnly? fecha = null) =>
        await _e.Citas.ProgramarAsync(new ProgramarCitaDto { IdPaciente = paciente, IdMedico = medico, Fecha = fecha ?? Escenario.Lunes, HoraInicio = new(hora, 0) });

    [Fact]
    public async Task Indicadores_calculan_la_tasa_de_inasistencia_sobre_las_citas_cerradas()
    {
        _e.ComoAdministrador();
        var a = await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 8);
        var b = await CitaAsync(Escenario.PacienteLuis, Escenario.DraRojas, 9);
        var c = await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 10);
        await CitaAsync(Escenario.PacienteLuis, Escenario.DraRojas, 11);
        _e.Reloj.Ahora = new DateTime(2026, 11, 9, 12, 30, 0);
        await _e.Citas.RegistrarAtencionAsync(a.IdCita, new AtenderCitaDto { Asistio = true });
        await _e.Citas.RegistrarAtencionAsync(b.IdCita, new AtenderCitaDto { Asistio = true });
        await _e.Citas.RegistrarAtencionAsync(c.IdCita, new AtenderCitaDto { Asistio = false });

        var indicadores = await _e.Reportes.IndicadoresAsync(Escenario.Lunes, Escenario.Lunes);

        Assert.Equal(4, indicadores.TotalCitas);
        Assert.Equal(2, indicadores.Atendidas);
        Assert.Equal(1, indicadores.NoAsistio);
        Assert.Equal(1, indicadores.Pendientes);
        Assert.Equal(33.3m, indicadores.TasaInasistencia);           // 1 de 3 citas cerradas
        Assert.Equal(4, indicadores.PorEspecialidad.Single().Valor);
    }

    [Fact]
    public async Task Exportar_usa_el_exportador_del_formato_pedido_y_deja_auditoria()
    {
        var cita = await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 9);

        var archivo = await _e.Reportes.ExportarAsync(new FiltroCitasDto { Desde = Escenario.Lunes, Hasta = Escenario.Lunes }, "CSV");

        Assert.Equal("text/csv", archivo.Mime);
        Assert.Equal("citas_20261109_20261109.csv", archivo.Nombre);
        Assert.Contains(cita.Codigo, System.Text.Encoding.UTF8.GetString(archivo.Contenido));
        Assert.Contains(_e.Db.Auditorias, a => a.Accion == "EXPORTAR");
    }

    [Fact]
    public async Task Exportar_rechaza_formatos_desconocidos_y_periodos_de_mas_de_12_meses()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Reportes.ExportarAsync(new FiltroCitasDto(), "docx"));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => _e.Reportes.ExportarAsync(new FiltroCitasDto { Desde = new(2025, 1, 1), Hasta = new(2026, 11, 9) }, "csv"));
    }

    [Fact]
    public async Task El_reporte_respeta_la_sede_del_usuario()
    {
        _e.ComoAdministrador();
        await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 9);
        await CitaAsync(Escenario.PacienteLuis, Escenario.DrPaz, 14);

        _e.ComoRecepcionistaVes();
        var reporte = await _e.Reportes.CitasPorPeriodoAsync(new FiltroCitasDto { Desde = Escenario.Lunes, Hasta = Escenario.Lunes });

        Assert.Equal("Villa El Salvador", Assert.Single(reporte.Filas).Sede);
    }

    [Fact]
    public async Task Las_notificaciones_envian_la_confirmacion_y_generan_el_recordatorio_del_dia_siguiente()
    {
        var cita = await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 8, Escenario.Martes);

        var enviados = await _e.Notificaciones.ProcesarAsync();

        Assert.Equal(1, enviados);                                                   // la confirmación
        Assert.Contains(cita.Codigo, _e.Correo.Enviados.Single().Asunto);
        var recordatorio = Assert.Single(_e.Db.Notificaciones, n => n.Tipo == Notificacion.TipoRecordatorio);
        Assert.Equal(Notificacion.EstadoPendiente, recordatorio.EstadoEnvio);        // programado para 24 horas antes
        Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), recordatorio.FechaProgramada);

        _e.Reloj.Ahora = new DateTime(2026, 11, 9, 8, 1, 0);
        Assert.Equal(1, await _e.Notificaciones.ProcesarAsync());                    // ahora sí sale el recordatorio
        Assert.Single(_e.Db.Notificaciones, n => n.Tipo == Notificacion.TipoRecordatorio);   // y no se duplica
    }

    [Fact]
    public async Task Sin_servidor_de_correo_las_notificaciones_quedan_pendientes()
    {
        _e.Correo.Configurado = false;
        await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 9);

        Assert.Equal(0, await _e.Notificaciones.ProcesarAsync());
        Assert.All(_e.Db.Notificaciones, n => Assert.Equal(Notificacion.EstadoPendiente, n.EstadoEnvio));
    }

    [Fact]
    public async Task Un_fallo_de_envio_queda_registrado_y_no_detiene_el_proceso()
    {
        _e.Correo.Fallar = true;
        await CitaAsync(Escenario.PacienteAna, Escenario.DraRojas, 9);

        Assert.Equal(0, await _e.Notificaciones.ProcesarAsync());
        var fallida = Assert.Single(_e.Db.Notificaciones);
        Assert.Equal(Notificacion.EstadoError, fallida.EstadoEnvio);
        Assert.Contains("SMTP", fallida.Detalle);
    }
}
