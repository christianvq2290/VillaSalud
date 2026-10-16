using System.Text;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;

namespace VillaSalud.Infrastructure.Reportes;

/// <summary>CSV en UTF-8 con BOM y separador punto y coma, que es el que Excel espera con configuración regional de Perú.</summary>
public class ExportadorCsv : IExportadorReporte
{
    private const char Separador = ';';

    public string Formato => "csv";
    public string Mime => "text/csv";

    public byte[] Generar(ReporteCitasDto reporte)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separador, EncabezadosReporte.Citas));
        foreach (var f in reporte.Filas)
        {
            sb.AppendLine(string.Join(Separador, new[]
            {
                f.Fecha.ToString("dd/MM/yyyy"), f.HoraInicio.ToString("HH:mm"), f.HoraFin.ToString("HH:mm"), f.Codigo,
                f.Paciente, f.Dni, f.Medico, f.Especialidad, f.Consultorio, f.Sede, f.Estado
            }.Select(Escapar)));
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>Entrecomilla los valores con separador, comillas o saltos de línea, y neutraliza fórmulas (inyección CSV).</summary>
    public static string Escapar(string? valor)
    {
        var texto = valor ?? "";
        if (texto.Length > 0 && "=+-@".Contains(texto[0])) texto = "'" + texto;
        return texto.IndexOfAny(new[] { Separador, '"', '\n', '\r' }) >= 0 ? "\"" + texto.Replace("\"", "\"\"") + "\"" : texto;
    }
}

public static class EncabezadosReporte
{
    public static readonly string[] Citas = { "Fecha", "Inicio", "Fin", "Código", "Paciente", "DNI", "Médico", "Especialidad", "Consultorio", "Sede", "Estado" };
    public static readonly string[] Resumen = { "Médico", "Especialidad", "Total", "Atendidas", "Canceladas", "No asistió" };
}
