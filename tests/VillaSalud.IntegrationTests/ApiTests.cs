using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VillaSalud.Application.Dtos;
using VillaSalud.Infrastructure.Servicios;
using Xunit;

namespace VillaSalud.IntegrationTests;

/// <summary>Pruebas de extremo a extremo de la API: petición HTTP real, autenticación, autorización y reglas.</summary>
public class ApiTests : IClassFixture<FabricaApi>
{
    private readonly FabricaApi _api;

    public ApiTests(FabricaApi api) => _api = api;

    private static object NuevaCita(int idPaciente, int idMedico, int dias, string hora) =>
        new { idPaciente, idMedico, fecha = FabricaApi.Dentro(dias).ToString("yyyy-MM-dd"), horaInicio = hora };

    [Fact]
    public async Task Login_con_clave_incorrecta_responde_401_sin_revelar_el_motivo()
    {
        var respuesta = await _api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginDto { Username = "admin", Password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, problema.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Login_correcto_entrega_token_de_acceso_y_de_renovacion()
    {
        var respuesta = await _api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginDto { Username = "recepcion.ves", Password = FabricaApi.Clave });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>();
        Assert.False(string.IsNullOrEmpty(sesion!.TokenAcceso));
        Assert.False(string.IsNullOrEmpty(sesion.RefreshToken));
    }

    [Fact]
    public async Task Sin_token_la_API_responde_401()
    {
        var respuesta = await _api.CreateClient().GetAsync("/api/v1/citas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Recepcionista_no_accede_a_la_administracion_de_usuarios()
    {
        var cliente = await _api.ClienteComoAsync("recepcion.ves");

        var respuesta = await cliente.GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Programar_cita_responde_201_y_el_mismo_intervalo_luego_responde_409()
    {
        var cliente = await _api.ClienteComoAsync("recepcion.ves");

        var primera = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(1, 1, 3, "09:00"));
        var segunda = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(2, 1, 3, "09:00"));

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        var cita = await primera.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pendiente", cita.GetProperty("estado").GetString());
        Assert.StartsWith("C", cita.GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Paciente_sin_consentimiento_no_puede_recibir_cita()
    {
        var cliente = await _api.ClienteComoAsync("recepcion.ves");

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(4, 1, 4, "10:00"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("consentimiento", problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Recepcionista_no_programa_en_otra_sede()
    {
        var cliente = await _api.ClienteComoAsync("recepcion.ves");

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(2, 2, 4, "14:00"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Cancelar_exige_motivo_y_libera_el_intervalo()
    {
        var cliente = await _api.ClienteComoAsync("recepcion.ves");
        var creada = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(3, 1, 5, "11:00"));
        var id = (await creada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idCita").GetInt32();

        var sinMotivo = await cliente.PutAsJsonAsync($"/api/v1/citas/{id}/cancelar", new { motivo = "" });
        var cancelada = await cliente.PutAsJsonAsync($"/api/v1/citas/{id}/cancelar", new { motivo = "El paciente viajó" });
        var nueva = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(2, 1, 5, "11:00"));

        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelada.StatusCode);
        Assert.Equal(HttpStatusCode.Created, nueva.StatusCode);
    }

    [Fact]
    public async Task Medico_consulta_su_agenda_pero_no_programa_citas()
    {
        var cliente = await _api.ClienteComoAsync("erojas");

        var agenda = await cliente.GetAsync($"/api/v1/agenda?fecha={FabricaApi.Dentro(1):yyyy-MM-dd}");
        var programar = await cliente.PostAsJsonAsync("/api/v1/citas", NuevaCita(1, 1, 6, "08:00"));

        Assert.Equal(HttpStatusCode.OK, agenda.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, programar.StatusCode);
    }

    [Fact]
    public async Task Servicio_publico_del_asistente_exige_clave_de_API()
    {
        var cliente = _api.CreateClient();

        var sinClave = await cliente.GetAsync("/api/v1/publico/especialidades");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", FabricaApi.ApiKeyAsistente);
        var conClave = await cliente.GetAsync("/api/v1/publico/especialidades");

        Assert.Equal(HttpStatusCode.Unauthorized, sinClave.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conClave.StatusCode);
    }

    [Theory]
    [InlineData("pdf", "%PDF")]     // QuestPDF
    [InlineData("xlsx", "PK")]      // ClosedXML (un .xlsx es un archivo ZIP)
    [InlineData("csv", "﻿")]   // CSV con BOM para Excel
    public async Task Exportar_reporte_genera_un_archivo_valido(string formato, string firma)
    {
        var cliente = await _api.ClienteComoAsync("admin");
        var desde = FabricaApi.Dentro(0).ToString("yyyy-MM-dd");
        var hasta = FabricaApi.Dentro(30).ToString("yyyy-MM-dd");

        var respuesta = await cliente.GetAsync($"/api/v1/reportes/citas/exportar?desde={desde}&hasta={hasta}&formato={formato}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var contenido = await respuesta.Content.ReadAsByteArrayAsync();
        var esperado = System.Text.Encoding.UTF8.GetBytes(firma);
        Assert.True(contenido.Length > esperado.Length);
        Assert.Equal(esperado, contenido.Take(esperado.Length).ToArray());
    }

    [Fact]
    public async Task Auditoria_registra_los_inicios_de_sesion()
    {
        var cliente = await _api.ClienteComoAsync("admin");

        var eventos = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/auditoria");

        Assert.Contains(eventos.EnumerateArray(), e => e.GetProperty("accion").GetString() == "LOGIN");
    }
}

/// <summary>Comprueba que los hashes escritos en los scripts de base de datos corresponden a las claves documentadas.</summary>
public class HashDeScriptsTests
{
    [Theory]
    [InlineData("Clave#2026", "AQAAAAIAAYagAAAAEPDli2f0RoDcZXo9hXooZ2WYgZscswvi3wGZXI5IoMfsFeQ+B6cDKvviMoJ5DAaA6g==")]      // bd/01_creacion_bd.sql
    [InlineData("Temporal#2026", "AQAAAAIAAYagAAAAEHnRV4tnsy69MnQkQT32sIwau3eViL8dXPD8ahJpponmXAI88lX1JYnI378/ZHp/CA==")]   // bd/02_migracion_desde_v1.sql
    public void El_hash_del_script_valida_la_clave_documentada(string clave, string hash)
    {
        var hasher = new HasherClaves();

        Assert.True(hasher.Verificar(hash, clave));
        Assert.False(hasher.Verificar(hash, clave + "x"));
    }
}
