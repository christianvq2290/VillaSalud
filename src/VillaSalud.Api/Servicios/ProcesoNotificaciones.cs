using VillaSalud.Application.Servicios;

namespace VillaSalud.Api.Servicios;

/// <summary>Proceso en segundo plano: cada cierto intervalo genera los recordatorios y envía los correos pendientes.</summary>
public class ProcesoNotificaciones : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<ProcesoNotificaciones> _logger;

    public ProcesoNotificaciones(IServiceScopeFactory scopes, IConfiguration configuracion, ILogger<ProcesoNotificaciones> logger)
    {
        _scopes = scopes;
        _configuracion = configuracion;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuracion.GetValue("Notificaciones:Habilitado", true)) return;

        var segundos = Math.Max(10, _configuracion.GetValue("Notificaciones:IntervaloSegundos", 60));
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(segundos));
        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var servicio = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                    var enviados = await servicio.ProcesarAsync(stoppingToken);
                    if (enviados > 0) _logger.LogInformation("Notificaciones enviadas: {Cantidad}", enviados);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Un fallo (por ejemplo, la base de datos no disponible) no debe detener el proceso
                    _logger.LogWarning(ex, "No se pudieron procesar las notificaciones en este ciclo");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // La aplicación se está deteniendo
        }
    }
}
