using Microsoft.Extensions.DependencyInjection;
using VillaSalud.Application.Servicios;

namespace VillaSalud.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios de casos de uso de la capa de aplicación.</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<MedicoService>();
        services.AddScoped<PacienteService>();
        services.AddScoped<CitaService>();
        services.AddScoped<ReporteService>();
        services.AddScoped<NotificacionService>();
        return services;
    }
}
