using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VillaSalud.Api.Seguridad;
using VillaSalud.Api.Servicios;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Domain.Entidades;

namespace VillaSalud.Api.Configuracion;

public static class ApiExtensions
{
    public const string CorsClienteWeb = "ClienteWeb";

    /// <summary>Servicios propios de la API: controladores, JWT, políticas, límites, CORS, Swagger y manejo de errores.</summary>
    public static IServiceCollection AddApiVillaSalud(this IServiceCollection services, IConfiguration configuracion)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(ApiExtensions).Assembly)
            .AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new ConvertidorHora()));
        services.AddHttpContextAccessor();
        services.AddScoped<IContextoUsuario, ContextoUsuarioHttp>();

        // ---- Autenticación con JWT (HMAC-SHA256)
        services.Configure<OpcionesJwt>(configuracion.GetSection("Jwt"));
        services.AddSingleton<IGeneradorTokens, GeneradorTokensJwt>();
        var jwt = configuracion.GetSection("Jwt").Get<OpcionesJwt>() ?? new OpcionesJwt();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;      // se conservan los nombres cortos de los claims (sub, name, role)
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.ClaveEnBytes()),
                    NameClaimType = ClaimsToken.Nombre,
                    RoleClaimType = ClaimsToken.Rol
                };
            });

        // ---- Autorización por rol
        services.AddAuthorization(opciones =>
        {
            opciones.AddPolicy(Politicas.SoloAdmin, p => p.RequireRole(Roles.Administrador));
            opciones.AddPolicy(Politicas.GestionCitas, p => p.RequireRole(Politicas.RolesGestionCitas));
            opciones.AddPolicy(Politicas.Agenda, p => p.RequireRole(Politicas.RolesAgenda));
        });

        // ---- Límite de peticiones por IP en el inicio de sesión y en el canal público
        var loginPorMinuto = configuracion.GetValue("Limites:LoginPorMinuto", 20);
        var publicoPorMinuto = configuracion.GetValue("Limites:PublicoPorMinuto", 30);
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.AddPolicy(Politicas.LimiteLogin, contexto => VentanaFija(contexto, loginPorMinuto));
            opciones.AddPolicy(Politicas.LimitePublico, contexto => VentanaFija(contexto, publicoPorMinuto));
        });

        // ---- CORS para clientes de navegador autorizados
        var origenes = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(opciones => opciones.AddPolicy(CorsClienteWeb, p => p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

        // ---- Errores en formato ProblemDetails
        services.AddExceptionHandler<ManejadorErrores>();
        services.AddProblemDetails();

        // ---- Documentación OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API Villa Salud – Gestión de Citas Médicas",
                Version = "v1",
                Description = "Servicios REST de la plataforma. Use POST /api/v1/auth/login y pegue el token en Authorize."
            });
            opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token de acceso obtenido en /api/v1/auth/login"
            });
            opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
        });

        services.AddHostedService<ProcesoNotificaciones>();
        return services;
    }

    /// <summary>Canalización de peticiones de la API.</summary>
    public static WebApplication UseApiVillaSalud(this WebApplication app)
    {
        app.UseExceptionHandler();
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        app.UseHttpsRedirection();

        // Cabeceras de seguridad en todas las respuestas
        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
            contexto.Response.Headers["X-Frame-Options"] = "DENY";
            contexto.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await siguiente();
        });

        app.UseSwagger();
        app.UseSwaggerUI(opciones =>
        {
            opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "API Villa Salud v1");
            opciones.DocumentTitle = "API Villa Salud";
        });

        app.UseCors(CorsClienteWeb);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        return app;
    }

    private static RateLimitPartition<string> VentanaFija(HttpContext contexto, int permitidas) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitidas, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
