// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\HistorialProveedorPdfReport.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SILF.Core.Models;

namespace SILF.Reports;

public static class HistorialProveedorPdfReport
{
    public static void Generar(Proveedor proveedor, List<Lote> lotes,
        string rutaSalida, string empresaNombre, byte[]? logoBytes = null)
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
                            c.Item().AlignCenter().Text("HISTORIAL DE PROVEEDOR")
                                .Bold().FontSize(11);
                        });
                        if (logoBytes != null)
                            row.ConstantItem(50);
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Datos del proveedor
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("Proveedor: ").Bold();
                                t.Span(proveedor.NombreCompleto);
                            });
                            c.Item().Text(t =>
                            {
                                t.Span("CI/NIT: ").Bold();
                                t.Span(proveedor.CiNit);
                            });
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("Cooperativa: ").Bold();
                                t.Span(proveedor.Cooperativa?.Nombre ?? "—");
                            });
                            c.Item().Text(t =>
                            {
                                t.Span("Total Lotes: ").Bold();
                                t.Span(lotes.Count.ToString());
                            });
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                });

                // ── CONTENIDO ──
                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);  // N°
                        cols.ConstantColumn(45);  // Lote
                        cols.ConstantColumn(65);  // Fecha
                        cols.ConstantColumn(60);  // Mina
                        cols.ConstantColumn(50);  // Tipo
                        cols.ConstantColumn(55);  // Peso Neto
                        cols.ConstantColumn(35);  // ZN
                        cols.ConstantColumn(35);  // AG
                        cols.ConstantColumn(35);  // PB
                        cols.ConstantColumn(70);  // Valor Bs
                        cols.ConstantColumn(65);  // Deducciones
                        cols.ConstantColumn(70);  // Líquido
                        cols.ConstantColumn(45);  // Estado
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        var hs = TextStyle.Default.FontSize(7).Bold().FontColor(Colors.White);

                        void H(IContainer c, string text) =>
                            c.Background(Colors.Indigo.Medium).Padding(3)
                                .AlignCenter().Text(text).Style(hs);

                        H(header.Cell(), "N°");
                        H(header.Cell(), "LOTE");
                        H(header.Cell(), "FECHA");
                        H(header.Cell(), "MINA");
                        H(header.Cell(), "TIPO");
                        H(header.Cell(), "NETO Tn");
                        H(header.Cell(), "ZN");
                        H(header.Cell(), "AG");
                        H(header.Cell(), "PB");
                        H(header.Cell(), "VALOR Bs");
                        H(header.Cell(), "DEDUCC.");
                        H(header.Cell(), "LÍQUIDO Bs");
                        H(header.Cell(), "ESTADO");
                    });

                    // Datos
                    int idx = 1;
                    bool alt = false;
                    decimal totalValor = 0, totalDed = 0, totalLiquido = 0;

                    foreach (var lote in lotes)
                    {
                        var liq = lote.Liquidacion;
                        var bg = alt ? Colors.Grey.Lighten5 : Colors.White;
                        alt = !alt;

                        var valorBs = liq?.ValorComercialBs ?? 0;
                        var ded = liq?.TotalDeducciones ?? 0;
                        var liquido = liq?.LiquidoPagable ?? 0;
                        totalValor += valorBs;
                        totalDed += ded;
                        totalLiquido += liquido;

                        void Cell(IContainer c, string text, bool right = false)
                        {
                            var container = c.Background(bg).Padding(3);
                            if (right) container.AlignRight().Text(text).FontSize(8);
                            else container.AlignCenter().Text(text).FontSize(8);
                        }

                        Cell(table.Cell(), idx++.ToString());
                        Cell(table.Cell(), lote.NumeroLote.ToString());
                        Cell(table.Cell(), lote.FechaRegistro.ToString("dd/MM/yy"));
                        Cell(table.Cell(), lote.Mina?.Nombre ?? "");
                        Cell(table.Cell(), lote.TipoMineral.ToString() ?? "");
                        Cell(table.Cell(), lote.PesoNeto.ToString("N2"), true);
                        Cell(table.Cell(), (lote.LeyZn ?? 0).ToString("N2"), true);
                        Cell(table.Cell(), (lote.LeyAg ?? 0).ToString("N2"), true);
                        Cell(table.Cell(), (lote.LeyPb ?? 0).ToString("N2"), true);
                        Cell(table.Cell(), valorBs.ToString("N2"), true);
                        Cell(table.Cell(), ded.ToString("N2"), true);
                        Cell(table.Cell(), liquido.ToString("N2"), true);
                        Cell(table.Cell(), lote.Estado.ToString());
                    }

                    // Totales
                    var bgT = Colors.Indigo.Lighten5;
                    table.Cell().ColumnSpan(9).Background(bgT).Padding(4)
                        .AlignRight().Text("TOTALES").Bold().FontSize(9);
                    table.Cell().Background(bgT).Padding(4).AlignRight()
                        .Text(totalValor.ToString("N2")).Bold().FontSize(9);
                    table.Cell().Background(bgT).Padding(4).AlignRight()
                        .Text(totalDed.ToString("N2")).Bold().FontSize(9);
                    table.Cell().Background(bgT).Padding(4).AlignRight()
                        .Text(totalLiquido.ToString("N2")).Bold().FontSize(9);
                    table.Cell().Background(bgT).Padding(4).Text("");
                });

                // ── FOOTER ──
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("SILF — Historial de Proveedor — Página ").FontSize(7)
                        .FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                    t.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(rutaSalida);
    }
}
