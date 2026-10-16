using ClosedXML.Excel;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;

namespace VillaSalud.Infrastructure.Reportes;

/// <summary>Libro de Excel con dos hojas: el detalle de citas y el resumen por médico (ClosedXML).</summary>
public class ExportadorExcel : IExportadorReporte
{
    private const int FilaEncabezado = 4;

    public string Formato => "xlsx";
    public string Mime => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Generar(ReporteCitasDto reporte)
    {
        using var libro = new XLWorkbook();

        var hoja = libro.Worksheets.Add("Citas");
        hoja.Cell(1, 1).SetValue(reporte.Titulo);
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).SetValue($"Periodo: {reporte.Desde:dd/MM/yyyy} al {reporte.Hasta:dd/MM/yyyy}   |   Generado: {reporte.GeneradoEl:dd/MM/yyyy HH:mm}");
        Encabezados(hoja, EncabezadosReporte.Citas);

        var fila = FilaEncabezado + 1;
        foreach (var f in reporte.Filas)
        {
            hoja.Cell(fila, 1).SetValue(f.Fecha.ToDateTime(TimeOnly.MinValue));
            hoja.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            hoja.Cell(fila, 2).SetValue(f.HoraInicio.ToString("HH:mm"));
            hoja.Cell(fila, 3).SetValue(f.HoraFin.ToString("HH:mm"));
            hoja.Cell(fila, 4).SetValue(f.Codigo);
            hoja.Cell(fila, 5).SetValue(f.Paciente);
            hoja.Cell(fila, 6).SetValue(f.Dni);
            hoja.Cell(fila, 7).SetValue(f.Medico);
            hoja.Cell(fila, 8).SetValue(f.Especialidad);
            hoja.Cell(fila, 9).SetValue(f.Consultorio);
            hoja.Cell(fila, 10).SetValue(f.Sede);
            hoja.Cell(fila, 11).SetValue(f.Estado);
            fila++;
        }
        hoja.SheetView.FreezeRows(FilaEncabezado);
        Ajustar(hoja);

        var resumen = libro.Worksheets.Add("Resumen por médico");
        resumen.Cell(1, 1).SetValue("Resumen por médico");
        resumen.Cell(1, 1).Style.Font.Bold = true;
        Encabezados(resumen, EncabezadosReporte.Resumen);
        fila = FilaEncabezado + 1;
        foreach (var r in reporte.Resumen)
        {
            resumen.Cell(fila, 1).SetValue(r.Medico);
            resumen.Cell(fila, 2).SetValue(r.Especialidad);
            resumen.Cell(fila, 3).SetValue(r.Total);
            resumen.Cell(fila, 4).SetValue(r.Atendidas);
            resumen.Cell(fila, 5).SetValue(r.Canceladas);
            resumen.Cell(fila, 6).SetValue(r.NoAsistio);
            fila++;
        }
        Ajustar(resumen);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Encabezados(IXLWorksheet hoja, string[] titulos)
    {
        for (var i = 0; i < titulos.Length; i++) hoja.Cell(FilaEncabezado, i + 1).SetValue(titulos[i]);
        var rango = hoja.Range(FilaEncabezado, 1, FilaEncabezado, titulos.Length);
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
    }

    private static void Ajustar(IXLWorksheet hoja)
    {
        try
        {
            hoja.Columns().AdjustToContents();
        }
        catch (Exception)
        {
            // El ajuste de ancho mide texto con fuentes del sistema; si el servidor no las tiene, se deja el ancho por defecto
        }
    }
}
