using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VillaSalud.Api.Configuracion;

/// <summary>Acepta horas en formato HH:mm o HH:mm:ss en el JSON de entrada; siempre responde HH:mm:ss.</summary>
public class ConvertidorHora : JsonConverter<TimeOnly>
{
    private static readonly string[] Formatos = { "HH:mm:ss", "HH:mm", "H:mm" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var texto = reader.GetString();
        if (TimeOnly.TryParseExact(texto, Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora)) return hora;
        throw new JsonException($"Hora no válida: '{texto}'. Use el formato HH:mm.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}
