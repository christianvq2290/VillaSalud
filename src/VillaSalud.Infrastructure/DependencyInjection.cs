using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Infrastructure.Persistencia;
using VillaSalud.Infrastructure.Reportes;
using VillaSalud.Infrastructure.Repositorios;
using VillaSalud.Infrastructure.Servicios;

namespace VillaSalud.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra la persistencia (EF Core + SQL Server) y los servicios técnicos: hash, correo y exportadores.</summary>
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddDbContext<VillaSaludDbContext>(opciones =>
            opciones.UseSqlServer(configuracion.GetConnectionString("VillaSalud")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        services.AddScoped<IMedicoRepositorio, MedicoRepositorio>();
        services.AddScoped<IPacienteRepositorio, PacienteRepositorio>();
        services.AddScoped<ICitaRepositorio, CitaRepositorio>();
        services.AddScoped<IAuditoriaRepositorio, AuditoriaRepositorio>();

        services.AddSingleton<IReloj, RelojPeru>();
        services.AddSingleton<IHasherClaves, HasherClaves>();

        services.Configure<OpcionesSmtp>(configuracion.GetSection("Smtp"));
        services.AddSingleton<IEnviadorCorreo, EnviadorCorreoSmtp>();

        services.AddSingleton<IExportadorReporte, ExportadorPdf>();
        services.AddSingleton<IExportadorReporte, ExportadorExcel>();
        services.AddSingleton<IExportadorReporte, ExportadorCsv>();

        return services;
    }
}
