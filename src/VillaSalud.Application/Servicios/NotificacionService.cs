using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Application.Servicios;

/// <summary>Genera los recordatorios del día siguiente y envía las notificaciones pendientes. Lo ejecuta un proceso en segundo plano de la API.</summary>
public class NotificacionService
{
    public const int HorasAntesRecordatorio = 24;
    private const int MaximoPorCiclo = 20;

    private readonly ICitaRepositorio _citas;
    private readonly IEnviadorCorreo _correo;
    private readonly IUnitOfWork _uow;
    private readonly IReloj _reloj;

    public NotificacionService(ICitaRepositorio citas, IEnviadorCorreo correo, IUnitOfWork uow, IReloj reloj)
    {
        _citas = citas;
        _correo = correo;
        _uow = uow;
        _reloj = reloj;
    }

    /// <returns>Cantidad de correos enviados en el ciclo.</returns>
    public async Task<int> ProcesarAsync(CancellationToken ct = default)
    {
        var ahora = _reloj.Ahora;

        // 1. Recordatorios para las citas de mañana
        var manana = DateOnly.FromDateTime(ahora).AddDays(1);
        foreach (var cita in await _citas.ListarCitasSinRecordatorioAsync(manana, ct))
        {
            _citas.AgregarNotificacion(new Notificacion
            {
                IdCita = cita.IdCita, Tipo = Notificacion.TipoRecordatorio, Destino = cita.Paciente!.Email!,
                FechaProgramada = cita.Fecha.ToDateTime(cita.HoraInicio).AddHours(-HorasAntesRecordatorio)
            });
        }
        await _uow.GuardarAsync(ct);

        // 2. Envío de pendientes (si no hay SMTP configurado, quedan en cola)
        if (!_correo.Configurado) return 0;

        var enviados = 0;
        foreach (var notificacion in await _citas.ListarNotificacionesPendientesAsync(ahora, MaximoPorCiclo, ct))
        {
            try
            {
                var (asunto, cuerpo) = Redactar(notificacion);
                await _correo.EnviarAsync(notificacion.Destino, asunto, cuerpo, ct);
                notificacion.EstadoEnvio = Notificacion.EstadoEnviada;
                notificacion.FechaEnvio = _reloj.Ahora;
                enviados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notificacion.EstadoEnvio = Notificacion.EstadoError;
                notificacion.Detalle = ex.Message.Length > 240 ? ex.Message[..240] : ex.Message;
            }
        }
        await _uow.GuardarAsync(ct);
        return enviados;
    }

    public static (string Asunto, string Cuerpo) Redactar(Notificacion n)
    {
        var c = n.Cita!;
        var encabezado = n.Tipo == Notificacion.TipoRecordatorio ? "Le recordamos su cita médica" : "Su cita médica fue registrada";
        var cuerpo =
            $"Estimado(a) {c.Paciente?.NombreCompleto}:\n\n{encabezado}.\n\n" +
            $"Código: {c.Codigo}\nFecha: {c.Fecha:dd/MM/yyyy}\nHora: {c.HoraInicio:HH\\:mm}\n" +
            $"Médico: {c.Medico?.NombreCompleto}\nConsultorio: {c.Consultorio?.Numero}\nSede: {c.Sede?.Nombre} – {c.Sede?.Direccion}\n\n" +
            "Por favor, preséntese 15 minutos antes con su documento de identidad.\n\nClínica Villa Salud";
        var asunto = n.Tipo == Notificacion.TipoRecordatorio ? $"Recordatorio de cita {c.Codigo}" : $"Confirmación de cita {c.Codigo}";
        return (asunto, cuerpo);
    }
}
