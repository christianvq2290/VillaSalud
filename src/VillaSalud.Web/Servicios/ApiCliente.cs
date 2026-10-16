using System.Net;
using System.Text.Json;

namespace VillaSalud.Web.Servicios;

/// <summary>Cliente HTTP tipado de la API REST. Toda la lógica de negocio vive en la API; el cliente web solo la consume.</summary>
public class ApiCliente
{
    public static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiCliente(HttpClient http) => _http = http;

    public async Task<T> GetAsync<T>(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, ruta, null, ct);
        return await LeerAsync<T>(respuesta, ct);
    }

    public async Task<T> PostAsync<T>(string ruta, object? cuerpo, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Post, ruta, cuerpo, ct);
        return await LeerAsync<T>(respuesta, ct);
    }

    public async Task PostAsync(string ruta, object? cuerpo = null, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Post, ruta, cuerpo, ct);
    }

    public async Task<T> PutAsync<T>(string ruta, object? cuerpo, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Put, ruta, cuerpo, ct);
        return await LeerAsync<T>(respuesta, ct);
    }

    public async Task DeleteAsync(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Delete, ruta, null, ct);
    }

    /// <summary>Descarga un archivo (reportes) conservando el tipo y el nombre que definió la API.</summary>
    public async Task<(byte[] Contenido, string Mime, string Nombre)> DescargarAsync(string ruta, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, ruta, null, ct);
        var contenido = await respuesta.Content.ReadAsByteArrayAsync(ct);
        var mime = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var disposicion = respuesta.Content.Headers.ContentDisposition;
        var nombre = (disposicion?.FileNameStar ?? disposicion?.FileName ?? "reporte").Trim('"');
        return (contenido, mime, nombre);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct)
    {
        using var solicitud = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null) solicitud.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: OpcionesJson);

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _http.SendAsync(solicitud, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "No se pudo conectar con el servicio de la plataforma. Intente nuevamente en unos minutos.") { Source = ex.Source };
        }

        if (respuesta.IsSuccessStatusCode) return respuesta;

        using (respuesta)
        {
            throw await CrearErrorAsync(respuesta, ct);
        }
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, CancellationToken ct)
    {
        var valor = await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson, ct);
        return valor ?? throw new ApiException(HttpStatusCode.BadGateway, "La plataforma devolvió una respuesta vacía.");
    }

    // La API responde los errores como ProblemDetails: { title, status, detail, errors: { campo: [mensajes] } }
    private static async Task<ApiException> CrearErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        string? detalle = null;
        var errores = new Dictionary<string, string[]>();
        try
        {
            var texto = await respuesta.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                using var json = JsonDocument.Parse(texto);
                var raiz = json.RootElement;
                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String) detalle = d.GetString();
                    if (raiz.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var campo in e.EnumerateObject())
                        {
                            if (campo.Value.ValueKind != JsonValueKind.Array) continue;
                            errores[campo.Name] = campo.Value.EnumerateArray().Select(m => m.GetString() ?? "").ToArray();
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // respuesta sin cuerpo JSON (por ejemplo, 401 o 429 generados por la canalización)
        }

        detalle ??= errores.Count > 0
            ? string.Join(" ", errores.Values.SelectMany(m => m))
            : respuesta.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "La sesión expiró. Ingrese nuevamente.",
                HttpStatusCode.Forbidden => "No tiene permiso para realizar esta operación.",
                HttpStatusCode.TooManyRequests => "Demasiadas solicitudes. Espere un minuto e intente de nuevo.",
                HttpStatusCode.NotFound => "El recurso solicitado no existe.",
                _ => "La plataforma no pudo completar la operación."
            };
        return new ApiException(respuesta.StatusCode, detalle, errores);
    }
}
