using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VillaSalud.Application.Abstracciones;
using VillaSalud.Application.Dtos;

namespace VillaSalud.Infrastructure.Reportes;

/// <summary>Reporte en PDF (A4 horizontal) con el detalle de citas y el resumen por médico (QuestPDF, licencia Community).</summary>
public class ExportadorPdf : IExportadorReporte
{
    static ExportadorPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Formato => "pdf";
    public string Mime => "application/pdf";

    public byte[] Generar(ReporteCitasDto reporte)
    {
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(8));

                pagina.Header().Column(columna =>
                {
                    columna.Item().Text(reporte.Titulo).FontSize(14).Bold();
                    columna.Item().Text($"Periodo: {reporte.Desde:dd/MM/yyyy} al {reporte.Hasta:dd/MM/yyyy}   |   Generado: {reporte.GeneradoEl:dd/MM/yyyy HH:mm}   |   Total de citas: {reporte.Filas.Count}");
                });

                pagina.Content().PaddingVertical(8).Column(columna =>
                {
                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(52);    // fecha
                            c.ConstantColumn(34);    // inicio
                            c.ConstantColumn(34);    // fin
                            c.ConstantColumn(70);    // código
                            c.RelativeColumn(3);     // paciente
                            c.ConstantColumn(50);    // DNI
                            c.RelativeColumn(3);     // médico
                            c.RelativeColumn(2);     // especialidad
                            c.ConstantColumn(50);    // consultorio
                            c.RelativeColumn(2);     // sede
                            c.ConstantColumn(62);    // estado
                        });
                        tabla.Header(encabezado =>
                        {
                            foreach (var titulo in EncabezadosReporte.Citas)
                                encabezado.Cell().Background(Colors.Indigo.Darken1).Padding(3).Text(titulo).FontColor(Colors.White).Bold();
                        });
                        foreach (var f in reporte.Filas)
                        {
                            Celda(tabla, f.Fecha.ToString("dd/MM/yyyy"));
                            Celda(tabla, f.HoraInicio.ToString("HH:mm"));
                            Celda(tabla, f.HoraFin.ToString("HH:mm"));
                            Celda(tabla, f.Codigo);
                            Celda(tabla, f.Paciente);
                            Celda(tabla, f.Dni);
                            Celda(tabla, f.Medico);
                            Celda(tabla, f.Especialidad);
                            Celda(tabla, f.Consultorio);
                            Celda(tabla, f.Sede);
                            Celda(tabla, f.Estado);
                        }
                    });

                    columna.Item().PaddingTop(14).Text("Resumen por médico").FontSize(11).Bold();
                    columna.Item().PaddingTop(4).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.ConstantColumn(60);
                            c.ConstantColumn(60);
                            c.ConstantColumn(60);
                            c.ConstantColumn(60);
                        });
                        tabla.Header(encabezado =>
                        {
                            foreach (var titulo in EncabezadosReporte.Resumen)
                                encabezado.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(titulo).Bold();
                        });
                        foreach (var r in reporte.Resumen)
                        {
                            Celda(tabla, r.Medico);
                            Celda(tabla, r.Especialidad);
                            Celda(tabla, r.Total.ToString());
                            Celda(tabla, r.Atendidas.ToString());
                            Celda(tabla, r.Canceladas.ToString());
                            Celda(tabla, r.NoAsistio.ToString());
                        }
                    });
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.Span("Clínica Villa Salud – Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Celda(TableDescriptor tabla, string texto) =>
        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(texto);
}
