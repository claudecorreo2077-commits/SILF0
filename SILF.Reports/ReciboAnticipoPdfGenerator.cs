// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\ReciboAnticipoPdfGenerator.cs
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SILF.Core.Helpers;

namespace SILF.Reports;

/// <summary>
/// Datos para generar el PDF del Recibo de Anticipo.
/// El recibo NO muestra el tipo de cambio (regla del cliente).
/// </summary>
public class ReciboAnticipoData
{
    public string EmpresaNombre { get; set; } = "";
    public string EmpresaNit { get; set; } = "";
    public string EmpresaMunicipio { get; set; } = "";
    public string NombreLiquidador { get; set; } = "";
    public byte[]? Logo { get; set; }

    public int NumeroProceso { get; set; }
    public int NumeroLote { get; set; }
    public DateTime Fecha { get; set; }

    public string ProveedorNombre { get; set; } = "";
    public string ProveedorCi { get; set; } = "";
    public string Mina { get; set; } = "";

    public decimal Monto { get; set; }

    /// <summary>Identificador legible del recibo: P05-L003</summary>
    public string IdentificadorRecibo => $"ANT-P{NumeroProceso:00}-L{NumeroLote:000}";
}

/// <summary>
/// Generador del Recibo de Anticipo en formato PDF carta con dos copias recortables
/// (Original para el Proveedor + Copia para la Empresa).
/// NO muestra tipo de cambio.
/// </summary>
public static class ReciboAnticipoPdfGenerator
{
    public static void Generar(ReciboAnticipoData data, string rutaSalida)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var montoLetras = NumeroALetras.Convertir(data.Monto);
        var fechaTexto = FormatearFechaTexto(data.Fecha);

        // QR con los datos identificatorios del recibo
        byte[]? qrBytes = null;
        try
        {
            var qrData = $"SILF|{data.IdentificadorRecibo}|{data.Fecha:yyyyMMdd}|Bs{data.Monto:F2}|{data.ProveedorNombre}";
            qrBytes = QrHelper.GenerarPng(qrData);
        }
        catch { /* Si falla el QR, el recibo se genera sin él */ }

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
                    col.Item().Element(c => CopiaRecibo(c, data, montoLetras, fechaTexto, qrBytes,
                        "ORIGINAL — PROVEEDOR"));

                    col.Item().PaddingVertical(15).AlignCenter()
                        .Text("- - - - - - - - - - - - - - - - - - - - - -  ✂  CORTAR AQUÍ  - - - - - - - - - - - - - - - - - - - - - -")
                        .FontSize(6).FontColor(Colors.Grey.Medium);

                    col.Item().Element(c => CopiaRecibo(c, data, montoLetras, fechaTexto, qrBytes,
                        "COPIA — EMPRESA"));
                });
            });
        }).GeneratePdf(rutaSalida);
    }

    private static void CopiaRecibo(IContainer container, ReciboAnticipoData data,
        string montoLetras, string fechaTexto, byte[]? qrBytes, string tipoCopia)
    {
        container.Column(col =>
        {
            col.Spacing(2);

            // ── Header ──
            col.Item().Row(row =>
            {
                if (data.Logo != null && data.Logo.Length > 0)
                    row.ConstantItem(35).Image(data.Logo).FitArea();

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignCenter().Text("RECIBO DE ANTICIPO").FontSize(10).Bold();
                    c.Item().AlignCenter().Text(tipoCopia).FontSize(7).Bold().FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(data.EmpresaNombre))
                        c.Item().AlignCenter().Text(data.EmpresaNombre).FontSize(7).Light();
                    if (!string.IsNullOrWhiteSpace(data.EmpresaNit))
                        c.Item().AlignCenter().Text($"NIT: {data.EmpresaNit}").FontSize(6).Light().FontColor(Colors.Grey.Darken1);
                });

                if (data.Logo != null && data.Logo.Length > 0)
                    row.ConstantItem(35);
            });

            // ── Datos del lote y proveedor ──
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    I(c, "RECIBO Nº:", data.IdentificadorRecibo);
                    I(c, "FECHA:", data.Fecha.ToString("dd/MM/yyyy"));
                    I(c, "PROCESO Nº:", data.NumeroProceso.ToString());
                    I(c, "LOTE Nº:", data.NumeroLote.ToString());
                });
                row.RelativeItem().Column(c =>
                {
                    I(c, "PÁGUESE A:", data.ProveedorNombre);
                    I(c, "CI / NIT:", data.ProveedorCi);
                    if (!string.IsNullOrWhiteSpace(data.Mina))
                        I(c, "MINA:", data.Mina);
                });
            });

            // ── Concepto ──
            col.Item().PaddingTop(3).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(cc =>
            {
                cc.Item().Text("POR CONCEPTO DE:").FontSize(6).Bold().FontColor(Colors.Blue.Darken2);
                cc.Item().PaddingTop(2).Text(
                    $"Anticipo entregado por el Lote N° {data.NumeroLote} " +
                    $"del Proceso de Flotación N° {data.NumeroProceso}, " +
                    $"a cuenta de la liquidación final del mineral entregado por el proveedor."
                ).FontSize(8);
            });

            // ── Monto + QR ──
            col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(r =>
                {
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("MONTO ANTICIPADO").FontSize(8);
                        rr.ConstantItem(120).AlignRight().Text($"Bs {data.Monto:N2}").FontSize(8).SemiBold();
                    });
                    r.Item().PaddingVertical(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("TOTAL Bs").FontSize(12).Bold();
                        rr.ConstantItem(150).AlignRight().Text($"{data.Monto:N2}").FontSize(14).Bold();
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

            // ── Fecha y municipio ──
            col.Item().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text(fechaTexto).FontSize(7).SemiBold();
                if (!string.IsNullOrWhiteSpace(data.EmpresaMunicipio))
                    r.ConstantItem(100).AlignRight().Text($"Municipio: {data.EmpresaMunicipio}").FontSize(6).Light();
            });

            // ── Firmas ──
            // ENTREGUÉ: la empresa (liquidador) entrega el anticipo
            // RECIBÍ: el proveedor recibe el anticipo
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    if (!string.IsNullOrWhiteSpace(data.NombreLiquidador))
                        f.Item().AlignCenter().Text(data.NombreLiquidador).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("ENTREGUÉ CONFORME").FontSize(6).Light();
                });
                row.ConstantItem(40);
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    if (!string.IsNullOrWhiteSpace(data.ProveedorNombre))
                        f.Item().AlignCenter().Text(data.ProveedorNombre).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("RECIBÍ CONFORME").FontSize(6).Light();
                });
            });

            // ── Aclaración pequeña ──
            col.Item().PaddingTop(6).AlignCenter()
                .Text("Este monto será descontado del Líquido Pagable de la liquidación final.")
                .FontSize(6).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private static void I(QuestPDF.Fluent.ColumnDescriptor c, string label, string value)
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
