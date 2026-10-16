using Azure.Identity;
using VillaSalud.Api.Configuracion;
using VillaSalud.Application;
using VillaSalud.Infrastructure;
using VillaSalud.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

// En Azure, los secretos (cadena de conexión, clave JWT, SMTP) se leen de Key Vault con identidad administrada.
// Nombres de los secretos: ConnectionStrings--VillaSalud, Jwt--Key, Smtp--Clave, Asistente--ApiKey.
var uriKeyVault = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(uriKeyVault))
    builder.Configuration.AddAzureKeyVault(new Uri(uriKeyVault), new DefaultAzureCredential());

builder.Services.AddAplicacion();                                   // casos de uso
builder.Services.AddInfraestructura(builder.Configuration);         // EF Core, repositorios, correo, exportadores
builder.Services.AddApiVillaSalud(builder.Configuration);           // controladores, JWT, políticas, Swagger
builder.Services.AddHealthChecks().AddDbContextCheck<VillaSaludDbContext>();

var app = builder.Build();
app.UseApiVillaSalud();
app.Run();

// Expone el punto de entrada a las pruebas de integración (WebApplicationFactory<Program>)
public partial class Program { }
