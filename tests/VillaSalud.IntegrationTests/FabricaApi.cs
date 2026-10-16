using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;
using VillaSalud.Domain.Entidades;
using VillaSalud.Infrastructure.Servicios;
using VillaSalud.UnitTests.Dobles;

namespace VillaSalud.IntegrationTests;

/// <summary>
/// Levanta la API completa en memoria (controladores, JWT, políticas, límites, manejo de errores,
/// exportadores reales) y sustituye solo la persistencia por repositorios en memoria,
/// de modo que las pruebas no necesitan SQL Server y pueden ejecutarse en el pipeline.
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>
{
    public const string Clave = "Clave#2026";
    public const string ApiKeyAsistente = "clave-asistente-pruebas";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting deja los valores disponibles desde el inicio de Program.cs
        builder.UseSetting("Jwt:Key", "clave-solo-para-pruebas-de-integracion-0123456789");
        builder.UseSetting("Asistente:ApiKey", ApiKeyAsistente);
        builder.UseSetting("Notificaciones:Habilitado", "false");
        builder.UseSetting("Limites:LoginPorMinuto", "1000");
        builder.UseSetting("Limites:PublicoPorMinuto", "1000");
        builder.UseSetting("KeyVault:Uri", "");

        builder.ConfigureTestServices(services =>
        {
            var almacen = DatosDePrueba.Crear(new HasherClaves());
            services.AddSingleton(almacen);

            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<IUsuarioRepositorio>();
            services.RemoveAll<ICatalogoRepositorio>();
            services.RemoveAll<IMedicoRepositorio>();
            services.RemoveAll<IPacienteRepositorio>();
            services.RemoveAll<ICitaRepositorio>();
            services.RemoveAll<IAuditoriaRepositorio>();
            services.RemoveAll<IEnviadorCorreo>();

            services.AddScoped<IUnitOfWork, UnidadDeTrabajoEnMemoria>();
            services.AddScoped<IUsuarioRepositorio, UsuarioRepositorioEnMemoria>();
            services.AddScoped<ICatalogoRepositorio, CatalogoRepositorioEnMemoria>();
            services.AddScoped<IMedicoRepositorio, MedicoRepositorioEnMemoria>();
            services.AddScoped<IPacienteRepositorio, PacienteRepositorioEnMemoria>();
            services.AddScoped<ICitaRepositorio, CitaRepositorioEnMemoria>();
            services.AddScoped<IAuditoriaRepositorio, AuditoriaRepositorioEnMemoria>();
            services.AddSingleton<IEnviadorCorreo>(new CorreoDePrueba { Configurado = false });
        });
    }

    /// <summary>Cliente HTTP con la sesión del usuario indicado ya iniciada.</summary>
    public async Task<HttpClient> ClienteComoAsync(string usuario)
    {
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new LoginDto { Username = usuario, Password = Clave });
        respuesta.EnsureSuccessStatusCode();
        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.TokenAcceso);
        return cliente;
    }

    /// <summary>Próximo día a partir de mañana (todos los médicos de prueba atienden los siete días).</summary>
    public static DateOnly Dentro(int dias) => DateOnly.FromDateTime(new RelojPeru().Ahora).AddDays(dias);
}

public static class DatosDePrueba
{
    public static AlmacenEnMemoria Crear(IHasherClaves hasher)
    {
        var db = new AlmacenEnMemoria();
        var clave = hasher.Hash(FabricaApi.Clave);
        var ahora = new RelojPeru().Ahora;

        db.Sedes.Add(new Sede { IdSede = 1, Nombre = "Villa El Salvador", Direccion = "Av. Revolución 1250", Distrito = "Villa El Salvador" });
        db.Sedes.Add(new Sede { IdSede = 2, Nombre = "San Juan de Miraflores", Direccion = "Av. San Juan 980", Distrito = "San Juan de Miraflores" });
        db.Roles.Add(new Rol { IdRol = 1, Nombre = Roles.Administrador });
        db.Roles.Add(new Rol { IdRol = 2, Nombre = Roles.Medico });
        db.Roles.Add(new Rol { IdRol = 3, Nombre = Roles.Recepcionista });
        db.Especialidades.Add(new Especialidad { IdEspecialidad = 1, Nombre = "Cardiología" });
        db.Especialidades.Add(new Especialidad { IdEspecialidad = 2, Nombre = "Medicina General" });
        db.Medicos.Add(new Medico { IdMedico = 1, CodigoColegiatura = "CMP-45120", Nombres = "Elena", Apellidos = "Rojas Vega", IdEspecialidad = 1 });
        db.Medicos.Add(new Medico { IdMedico = 2, CodigoColegiatura = "CMP-51203", Nombres = "Lucía", Apellidos = "Campos Ríos", IdEspecialidad = 2 });
        db.Consultorios.Add(new Consultorio { IdConsultorio = 1, IdSede = 1, Numero = "101", Piso = "1" });
        db.Consultorios.Add(new Consultorio { IdConsultorio = 2, IdSede = 2, Numero = "101", Piso = "1" });

        var idHorario = 0;
        for (byte dia = 0; dia <= 6; dia++)
        {
            db.Horarios.Add(new HorarioMedico { IdHorario = ++idHorario, IdMedico = 1, IdConsultorio = 1, DiaSemana = dia, HoraInicio = new(8, 0), HoraFin = new(13, 0), DuracionCitaMin = 30 });
            db.Horarios.Add(new HorarioMedico { IdHorario = ++idHorario, IdMedico = 2, IdConsultorio = 2, DiaSemana = dia, HoraInicio = new(14, 0), HoraFin = new(18, 0), DuracionCitaMin = 30 });
        }

        (string Dni, string Nombres, string Apellidos, bool Consiente)[] pacientes =
        {
            ("40123456", "Ana", "Quispe Flores", true), ("41234567", "Luis", "Soto Mendoza", true),
            ("42345678", "Rosa", "Lima Torres", true), ("43456789", "Pedro", "Ccori Huanca", false),
        };
        for (var i = 0; i < pacientes.Length; i++)
        {
            var paciente = new Paciente { IdPaciente = i + 1, Dni = pacientes[i].Dni, Nombres = pacientes[i].Nombres, Apellidos = pacientes[i].Apellidos };
            if (pacientes[i].Consiente)
                paciente.Consentimientos.Add(new Consentimiento { IdConsentimiento = i + 1, IdPaciente = i + 1, FechaAceptacion = ahora.AddDays(-10) });
            db.Pacientes.Add(paciente);
        }

        db.Usuarios.Add(new Usuario { IdUsuario = 1, Username = "admin", PasswordHash = clave, IdRol = 1 });
        db.Usuarios.Add(new Usuario { IdUsuario = 2, Username = "recepcion.ves", PasswordHash = clave, IdRol = 3, IdSede = 1 });
        db.Usuarios.Add(new Usuario { IdUsuario = 3, Username = "recepcion.sjm", PasswordHash = clave, IdRol = 3, IdSede = 2 });
        db.Usuarios.Add(new Usuario { IdUsuario = 4, Username = "erojas", PasswordHash = clave, IdRol = 2, IdSede = 1, IdMedico = 1 });
        db.Enlazar();
        return db;
    }
}
