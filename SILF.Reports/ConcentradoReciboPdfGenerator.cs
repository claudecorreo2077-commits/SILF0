// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\ConcentradoReciboPdfGenerator.cs
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SILF.Core.Helpers;

namespace SILF.Reports;

/// <summary>
/// Datos para el recibo (valorización) de un concentrado.
/// NO muestra tipo de cambio (regla del cliente).
/// </summary>
public class ConcentradoReciboData
{
    public string EmpresaNombre { get; set; } = "";
    public string EmpresaNit { get; set; } = "";
    public string EmpresaMunicipio { get; set; } = "";
    public string NombreLiquidador { get; set; } = "";
    public byte[]? Logo { get; set; }

    public string TipoConcentrado { get; set; } = "";   // "ZN-AG" / "AG"
    public string NumeroLiquidacion { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public string ClienteCi { get; set; } = "";
    public string Procedencia { get; set; } = "";
    public DateTime FechaEntrega { get; set; }
    public DateTime FechaLiquidacion { get; set; }

    public decimal PesoBruto { get; set; }   // TMH
    public decimal PesoNeto { get; set; }     // TMS
    public decimal LeyZn { get; set; }
    public decimal LeyAg { get; set; }
    public decimal LeyPb { get; set; }

    public decimal LiquidoPagableBs { get; set; }
    public decimal Anticipo { get; set; }
    public decimal SaldoPagarBs { get; set; }

    // Descuentos de Ley
    public decimal RegaliaMinera { get; set; }
    public decimal Cns { get; set; }
    public decimal Comibol { get; set; }
    public decimal Fedecomin { get; set; }
    public decimal Fencomin { get; set; }
    public decimal Wilstermann { get; set; }
    public decimal AporteCoop { get; set; }
    public decimal TotalRetenciones { get; set; }

    public string Identificador => $"CONC-{TipoConcentrado}-{NumeroLiquidacion}";
}

public static class ConcentradoReciboPdfGenerator
{
    public static void Generar(ConcentradoReciboData d, string rutaSalida)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var saldoLetras = NumeroALetras.Convertir(d.SaldoPagarBs);
        var fechaTexto = FechaTexto(d.FechaLiquidacion);

        byte[]? qr = null;
        try
        {
            var data = $"SILF|{d.Identificador}|{d.FechaLiquidacion:yyyyMMdd}|Bs{d.SaldoPagarBs:F2}|{d.ClienteNombre}";
            qr = QrHelper.GenerarPng(data);
        }
        catch { }

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(28);
                page.MarginVertical(18);
                page.DefaultTextStyle(t => t.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Spacing(3);

                    // ── Header ──
                    col.Item().Row(row =>
                    {
                        if (d.Logo is { Length: > 0 }) row.ConstantItem(42).Image(d.Logo).FitArea();
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().Text("VALORIZACIÓN DE CONCENTRADO").FontSize(12).Bold();
                            c.Item().AlignCenter().Text($"Concentrado de {d.TipoConcentrado}").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrEmpty(d.EmpresaNombre))
                                c.Item().AlignCenter().Text(d.EmpresaNombre).FontSize(8).Light();
                            if (!string.IsNullOrWhiteSpace(d.EmpresaNit))
                                c.Item().AlignCenter().Text($"NIT: {d.EmpresaNit}").FontSize(7).Light().FontColor(Colors.Grey.Darken1);
                        });
                        if (d.Logo is { Length: > 0 }) row.ConstantItem(42);
                    });

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // ── Datos cliente / liquidación ──
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            I(c, "CLIENTE:", d.ClienteNombre);
                            I(c, "CI:", d.ClienteCi);
                            I(c, "PROCEDENCIA:", d.Procedencia);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            I(c, "LIQUIDACIÓN N°:", d.NumeroLiquidacion);
                            I(c, "F. ENTREGA:", d.FechaEntrega.ToString("dd/MM/yyyy"));
                            I(c, "F. LIQUIDACIÓN:", d.FechaLiquidacion.ToString("dd/MM/yyyy"));
                        });
                    });

                    // ── Pesos y leyes ──
                    col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            I(c, "PESO BRUTO (TMH):", d.PesoBruto.ToString("N2"));
                            I(c, "PESO NETO (TMS):", d.PesoNeto.ToString("N2"));
                        });
                        row.RelativeItem().Column(c =>
                        {
                            if (d.LeyAg > 0) I(c, "LEY AG:", d.LeyAg.ToString("N2"));
                            if (d.LeyZn > 0) I(c, "LEY ZN %:", d.LeyZn.ToString("N2"));
                            if (d.LeyPb > 0) I(c, "LEY PB %:", d.LeyPb.ToString("N2"));
                        });
                    });

                    // ── Descuentos de Ley ──
                    col.Item().PaddingTop(4).Text("DESCUENTOS DE LEY").FontSize(7).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(c =>
                    {
                        Ded(c, "Regalía Minera", d.RegaliaMinera);
                        Ded(c, "C.N.S.", d.Cns);
                        Ded(c, "COMIBOL", d.Comibol);
                        Ded(c, "FEDECOMIN", d.Fedecomin);
                        Ded(c, "FENCOMIN", d.Fencomin);
                        if (d.Wilstermann > 0) Ded(c, "C. Wilstermann", d.Wilstermann);
                        if (d.AporteCoop > 0) Ded(c, "Aporte Cooperativa", d.AporteCoop);
                        c.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL DESCUENTOS").FontSize(8).Bold();
                            r.ConstantItem(120).AlignRight().Text($"Bs {d.TotalRetenciones:N2}").FontSize(8).Bold();
                        });
                    });

                    // ── Totales + QR ──
                    col.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(r =>
                        {
                            Tot(r, "LÍQUIDO PAGABLE", d.LiquidoPagableBs, false);
                            Tot(r, "ANTICIPO", d.Anticipo, false);
                            r.Item().PaddingVertical(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                            Tot(r, "SALDO A PAGAR", d.SaldoPagarBs, true);
                            r.Item().PaddingTop(2).Text($"SON: {saldoLetras}").FontSize(7).Italic().Bold();
                        });
                        if (qr != null)
                            row.ConstantItem(72).AlignRight().AlignMiddle().Column(q => q.Item().Width(62).Height(62).Image(qr).FitArea());
                    });

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text(fechaTexto).FontSize(7).SemiBold();
                        if (!string.IsNullOrWhiteSpace(d.EmpresaMunicipio))
                            r.ConstantItem(120).AlignRight().Text($"Municipio: {d.EmpresaMunicipio}").FontSize(7).Light();
                    });

                    // ── Firmas ──
                    col.Item().PaddingTop(45).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(f =>
                        {
                            f.Item().LineHorizontal(1).LineColor(Colors.Black);
                            if (!string.IsNullOrWhiteSpace(d.NombreLiquidador))
                                f.Item().AlignCenter().Text(d.NombreLiquidador).FontSize(7).Bold();
                            f.Item().AlignCenter().Text("LIQUIDADOR / COMPRADOR").FontSize(6).Light();
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().AlignCenter().Column(f =>
                        {
                            f.Item().LineHorizontal(1).LineColor(Colors.Black);
                            if (!string.IsNullOrWhiteSpace(d.ClienteNombre))
                                f.Item().AlignCenter().Text(d.ClienteNombre).FontSize(7).Bold();
                            f.Item().AlignCenter().Text("CLIENTE / VENDEDOR").FontSize(6).Light();
                        });
                    });
                });
            });
        }).GeneratePdf(rutaSalida);
    }

    private static void I(ColumnDescriptor c, string label, string value)
    {
        c.Item().Row(r =>
        {
            r.ConstantItem(95).Text(label).FontSize(7).SemiBold();
            r.RelativeItem().Text(value).FontSize(7);
        });
    }

    private static void Ded(ColumnDescriptor c, string label, decimal monto)
    {
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(7);
            r.ConstantItem(120).AlignRight().Text($"Bs {monto:N2}").FontSize(7);
        });
    }

    private static void Tot(ColumnDescriptor c, string label, decimal monto, bool grande)
    {
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(grande ? 12 : 8).Bold();
            r.ConstantItem(150).AlignRight().Text($"Bs {monto:N2}").FontSize(grande ? 14 : 8).Bold();
        });
    }

    private static string FechaTexto(DateTime fecha)
    {
        var c = new CultureInfo("es-BO");
        var dia = c.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToUpperInvariant();
        var mes = c.DateTimeFormat.GetMonthName(fecha.Month).ToUpperInvariant();
        return $"{dia} {fecha.Day:00} DE {mes} DE {fecha.Year}";
    }
}
