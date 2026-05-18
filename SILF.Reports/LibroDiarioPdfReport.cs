// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\LibroDiarioPdfReport.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SILF.Core.Models;

namespace SILF.Reports;

public static class LibroDiarioPdfReport
{
    public static void Generar(List<ReciboCaja> recibos, string rutaSalida,
        string empresaNombre, DateTime desde, DateTime hasta,
        decimal saldoAnterior, byte[]? logoBytes = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(30);
                page.MarginVertical(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ── HEADER ──
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes != null)
                        {
                            row.ConstantItem(50).Height(50).Image(logoBytes);
                            row.ConstantItem(10);
                        }
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().Text(empresaNombre.ToUpperInvariant())
                                .Bold().FontSize(14);
                            c.Item().AlignCenter().Text("LIBRO DIARIO DE CAJA CHICA")
                                .Bold().FontSize(11);
                            c.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                        if (logoBytes != null)
                            row.ConstantItem(50); // balance
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // ── CONTENIDO ──
                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(65);  // Fecha
                        cols.ConstantColumn(45);  // Nº Recibo
                        cols.RelativeColumn(3);   // Detalle
                        cols.ConstantColumn(70);  // Cuenta
                        cols.ConstantColumn(75);  // Entrada
                        cols.ConstantColumn(75);  // Salida
                        cols.ConstantColumn(80);  // Saldo
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        var headerStyle = TextStyle.Default.FontSize(8).Bold()
                            .FontColor(Colors.White);

                        void HeaderCell(IContainer c, string text) =>
                            c.Background(Colors.Indigo.Medium).Padding(4)
                                .AlignCenter().Text(text).Style(headerStyle);

                        HeaderCell(header.Cell(), "FECHA");
                        HeaderCell(header.Cell(), "Nº REC.");
                        HeaderCell(header.Cell(), "DETALLE / BENEFICIARIO");
                        HeaderCell(header.Cell(), "CUENTA");
                        HeaderCell(header.Cell(), "ENTRADA");
                        HeaderCell(header.Cell(), "SALIDA");
                        HeaderCell(header.Cell(), "SALDO");
                    });

                    // Fila: Saldo anterior
                    if (saldoAnterior != 0 || recibos.Count > 0)
                    {
                        var bgAnterior = Colors.Grey.Lighten4;
                        table.Cell().Background(bgAnterior).Padding(3)
                            .Text(desde.ToString("dd/MM/yyyy")).FontSize(8);
                        table.Cell().Background(bgAnterior).Padding(3).Text("");
                        table.Cell().Background(bgAnterior).Padding(3)
                            .Text("SALDO ANTERIOR").Bold().FontSize(8);
                        table.Cell().Background(bgAnterior).Padding(3).Text("");
                        table.Cell().Background(bgAnterior).Padding(3).AlignRight()
                            .Text(saldoAnterior > 0 ? saldoAnterior.ToString("N2") : "").FontSize(8);
                        table.Cell().Background(bgAnterior).Padding(3).Text("");
                        table.Cell().Background(bgAnterior).Padding(3).AlignRight()
                            .Text(saldoAnterior.ToString("N2")).Bold().FontSize(8);
                    }

                    // Filas de movimientos
                    decimal saldo = saldoAnterior;
                    decimal totalEntradas = 0, totalSalidas = 0;
                    bool alt = false;

                    foreach (var r in recibos)
                    {
                        decimal entrada = r.TipoMovimiento == "Entrada" ? r.Monto : 0;
                        decimal salida = r.TipoMovimiento == "Salida" ? r.Monto : 0;
                        saldo += entrada - salida;
                        totalEntradas += entrada;
                        totalSalidas += salida;

                        var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                        alt = !alt;

                        table.Cell().Background(bg).Padding(3)
                            .Text(r.Fecha.ToString("dd/MM/yyyy")).FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignCenter()
                            .Text(r.NumeroRecibo.ToString()).FontSize(8);
                        table.Cell().Background(bg).Padding(3)
                            .Text($"{r.Beneficiario} - {r.Concepto}").FontSize(8);
                        table.Cell().Background(bg).Padding(3)
                            .Text(r.Cuenta ?? "").FontSize(7);
                        table.Cell().Background(bg).Padding(3).AlignRight()
                            .Text(entrada > 0 ? entrada.ToString("N2") : "").FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight()
                            .Text(salida > 0 ? salida.ToString("N2") : "").FontSize(8);
                        table.Cell().Background(bg).Padding(3).AlignRight()
                            .Text(saldo.ToString("N2")).FontSize(8);
                    }

                    // Fila de totales
                    var bgTotal = Colors.Indigo.Lighten5;
                    table.Cell().ColumnSpan(4).Background(bgTotal).Padding(4)
                        .AlignRight().Text("TOTALES").Bold().FontSize(9);
                    table.Cell().Background(bgTotal).Padding(4).AlignRight()
                        .Text(totalEntradas.ToString("N2")).Bold().FontSize(9);
                    table.Cell().Background(bgTotal).Padding(4).AlignRight()
                        .Text(totalSalidas.ToString("N2")).Bold().FontSize(9);
                    table.Cell().Background(bgTotal).Padding(4).AlignRight()
                        .Text(saldo.ToString("N2")).Bold().FontSize(9);
                });

                // ── FOOTER ──
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("SILF — Libro Diario de Caja Chica — Página ").FontSize(7)
                        .FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                    t.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(rutaSalida);
    }
}
