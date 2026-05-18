// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\ReciboPdfGenerator.cs
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SILF.Core.Helpers;
using SILF.Core.Models;

namespace SILF.Reports;

public static class ReciboPdfGenerator
{
    public static void Generar(ReciboCaja recibo, string rutaSalida,
        Empresa? empresa = null, byte[]? logoBytes = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var montoLetras = recibo.MontoEnLetras ?? NumeroALetras.Convertir(recibo.Monto);
        var fechaTexto = FormatearFechaTexto(recibo.Fecha);
        var empresaNombre = empresa?.RazonSocial ?? "Empresa Minera";
        var empresaNit = empresa?.NIT ?? "";
        var empresaMunicipio = empresa?.Municipio ?? "";
        var liquidador = empresa?.NombreLiquidador ?? "";

        // Roles según tipo de movimiento:
        //   Salida  → Empresa ENTREGA dinero, Beneficiario RECIBE
        //   Entrada → Beneficiario ENTREGA dinero, Empresa RECIBE
        string firmaEntrego, firmaRecibio;
        if (recibo.TipoMovimiento == "Entrada")
        {
            firmaEntrego = recibo.Beneficiario;
            firmaRecibio = liquidador;
        }
        else // Salida
        {
            firmaEntrego = liquidador;
            firmaRecibio = recibo.Beneficiario;
        }

        byte[]? qrBytes = null;
        try
        {
            var qrData = QrHelper.DatosRecibo(recibo.NumeroRecibo, recibo.Monto, recibo.Fecha,
                recibo.Beneficiario, recibo.TipoMovimiento, firmaEntrego, firmaRecibio, recibo.Concepto);
            qrBytes = QrHelper.GenerarPng(qrData);
        }
        catch { }

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(25);
                page.MarginVertical(15);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => CopiaRecibo(c, recibo, montoLetras, fechaTexto,
                        firmaEntrego, firmaRecibio, empresaNombre, empresaNit, empresaMunicipio,
                        logoBytes, qrBytes, "ORIGINAL"));

                    col.Item().PaddingVertical(15).AlignCenter()
                        .Text("- - - - - - - - - - - - - - - - - - - - - -  ✂  CORTAR AQUÍ  - - - - - - - - - - - - - - - - - - - - - -")
                        .FontSize(6).FontColor(Colors.Grey.Medium);

                    col.Item().Element(c => CopiaRecibo(c, recibo, montoLetras, fechaTexto,
                        firmaEntrego, firmaRecibio, empresaNombre, empresaNit, empresaMunicipio,
                        logoBytes, qrBytes, $"COPIA EMPRESA - {recibo.TipoMovimiento.ToUpperInvariant()}"));
                });
            });
        }).GeneratePdf(rutaSalida);
    }

    private static void CopiaRecibo(IContainer container, ReciboCaja recibo,
        string montoLetras, string fechaTexto,
        string firmaEntrego, string firmaRecibio,
        string empresaNombre, string empresaNit, string empresaMunicipio,
        byte[]? logoBytes, byte[]? qrBytes, string tipoCopia)
    {
        container.Column(col =>
        {
            col.Spacing(2);

            // ── Header ──
            col.Item().Row(row =>
            {
                if (logoBytes != null && logoBytes.Length > 0)
                    row.ConstantItem(35).Image(logoBytes).FitArea();

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignCenter().Text("RECIBO DE PAGO").FontSize(10).Bold();
                    c.Item().AlignCenter().Text(tipoCopia).FontSize(7).Bold().FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(empresaNombre))
                        c.Item().AlignCenter().Text(empresaNombre).FontSize(7).Light();
                    if (!string.IsNullOrWhiteSpace(empresaNit))
                        c.Item().AlignCenter().Text($"NIT: {empresaNit}").FontSize(6).Light().FontColor(Colors.Grey.Darken1);
                });

                if (logoBytes != null && logoBytes.Length > 0)
                    row.ConstantItem(35);
            });

            // ── Info (2 columnas) ──
            var labelBenef = recibo.TipoMovimiento == "Salida"
                ? "PÁGUESE A:" : "RECIBIDO DE:";

            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    I(c, "RECIBO Nº:", recibo.NumeroRecibo.ToString());
                    I(c, "FECHA:", recibo.Fecha.ToString("dd/MM/yyyy"));
                    I(c, labelBenef, recibo.Beneficiario);
                });
                row.RelativeItem().Column(c =>
                {
                    I(c, "MONTO Bs:", $"{recibo.Monto:N2}");
                    I(c, "TIPO:", recibo.TipoMovimiento);
                    if (!string.IsNullOrWhiteSpace(recibo.Cuenta))
                        I(c, "CUENTA:", recibo.Cuenta);
                });
            });

            // ── Concepto ──
            col.Item().PaddingTop(3).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(cc =>
            {
                cc.Item().Text("POR CONCEPTO DE:").FontSize(6).Bold().FontColor(Colors.Blue.Darken2);
                cc.Item().PaddingTop(2).Text(recibo.Concepto).FontSize(8);
            });

            // ── Resultado + QR ──
            col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(r =>
                {
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("MONTO TOTAL").FontSize(8);
                        rr.ConstantItem(120).AlignRight().Text($"Bs {recibo.Monto:N2}").FontSize(8).SemiBold();
                    });
                    r.Item().PaddingVertical(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("TOTAL Bs").FontSize(12).Bold();
                        rr.ConstantItem(150).AlignRight().Text($"{recibo.Monto:N2}").FontSize(14).Bold();
                    });
                    r.Item().PaddingTop(2).Text($"SON: {montoLetras}").FontSize(7).Italic().Bold();
                });

                if (qrBytes != null)
                {
                    row.ConstantItem(70).AlignRight().AlignMiddle().Column(q =>
                    {
                        q.Item().Width(60).Height(60).Image(qrBytes).FitArea();
                    });
                }
            });

            // ── Fecha ──
            col.Item().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text(fechaTexto).FontSize(7).SemiBold();
                if (!string.IsNullOrWhiteSpace(empresaMunicipio))
                    r.ConstantItem(100).AlignRight().Text($"Municipio: {empresaMunicipio}").FontSize(6).Light();
            });

            // ── Firmas ──
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    if (!string.IsNullOrWhiteSpace(firmaEntrego))
                        f.Item().AlignCenter().Text(firmaEntrego).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("ENTREGUÉ CONFORME").FontSize(6).Light();
                });
                row.ConstantItem(40);
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    if (!string.IsNullOrWhiteSpace(firmaRecibio))
                        f.Item().AlignCenter().Text(firmaRecibio).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("RECIBÍ CONFORME").FontSize(6).Light();
                });
            });
        });
    }

    private static void I(ColumnDescriptor c, string label, string value)
    {
        c.Item().Row(r =>
        {
            r.ConstantItem(80).Text(label).FontSize(7).SemiBold();
            r.RelativeItem().Text(value).FontSize(7);
        });
    }

    private static string FormatearFechaTexto(DateTime fecha)
    {
        var culture = new CultureInfo("es-BO");
        string dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToUpperInvariant();
        string mes = culture.DateTimeFormat.GetMonthName(fecha.Month).ToUpperInvariant();
        return $"{dia} {fecha.Day:00} DE {mes} DE {fecha.Year}";
    }
}
