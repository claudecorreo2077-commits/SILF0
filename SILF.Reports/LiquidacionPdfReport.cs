// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\LiquidacionPdfReport.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace SILF.Reports;

public class LiquidacionPdfData
{
    public string Proveedor { get; set; } = "";
    public string CiNit { get; set; } = "";
    public string Mina { get; set; } = "";
    public string Cooperativa { get; set; } = "";
    public string TipoMineral { get; set; } = "";
    public int NumeroLote { get; set; }
    public decimal PesoNeto { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaLiquidacion { get; set; }
    /// <summary>
    /// T/C usado en cálculo. Se mantiene en el modelo para retrocompatibilidad
    /// con el ViewModel, pero NO se imprime en el PDF (regla del cliente).
    /// </summary>
    public decimal TipoCambio { get; set; }
    public decimal LeyZn { get; set; }
    public decimal LeyAg { get; set; }
    public decimal LeyPb { get; set; }
    public decimal Humedad { get; set; }
    public decimal CostoLaboratorio { get; set; }
    public decimal PesoHumedad { get; set; }
    public decimal PesoNetoSeco { get; set; }
    public decimal ValorBrutoZn { get; set; }
    public decimal ValorBrutoAg { get; set; }
    public decimal ValorBrutoPb { get; set; }
    public decimal ValorComercialUs { get; set; }
    public decimal ValorComercialBs { get; set; }
    public decimal Regalias { get; set; }
    public decimal CNS { get; set; }
    public decimal COMIBOL { get; set; }
    public decimal TotalDeduccionesLegales { get; set; }
    public decimal FENCOMIN { get; set; }
    public decimal FEDECOMIN { get; set; }
    public decimal PorcentajeCooperativa { get; set; }
    public decimal MontoCooperativa { get; set; }
    public decimal IUE { get; set; }
    public decimal TotalOtrasDeducciones { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal LiquidoPagable { get; set; }
    public decimal LiquidoPagableUs { get; set; }
    public string MontoLiteral { get; set; } = "";
    public decimal Anticipo { get; set; }
    public decimal SaldoPagar { get; set; }
    public decimal BonoTransporte { get; set; }
    public string? Observaciones { get; set; }
    public string EmpresaNombre { get; set; } = "";
    public string NombreLiquidador { get; set; } = "";
    public byte[]? EmpresaLogo { get; set; }
    /// <summary>Número del Proceso de Flotación al que pertenece el lote.</summary>
    public int NumeroProceso { get; set; }
}

public class LiquidacionPdfReport
{
    private readonly LiquidacionPdfData _d;

    public LiquidacionPdfReport(LiquidacionPdfData data) => _d = data;

    public byte[] Generar()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(25);
                page.MarginVertical(15);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Content().Column(col =>
                {
                    // ── COPIA PROVEEDOR ──
                    col.Item().Element(c => CopiaLiquidacion(c, "COPIA PROVEEDOR", mostrarControlPago: false));

                    // ── LÍNEA DE CORTE ──
                    col.Item().PaddingVertical(3).AlignCenter()
                        .Text("- - - - - - - - - - - - - - - - - - - - - -  ✂  CORTAR AQUÍ  - - - - - - - - - - - - - - - - - - - - - -")
                        .FontSize(6).FontColor(Colors.Grey.Medium);

                    // ── RECIBO BONO TRANSPORTE (queda con liquidador al cortar) ──
                    if (_d.BonoTransporte > 0)
                    {
                        col.Item().PaddingVertical(4).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(6).Row(row =>
                        {
                            row.RelativeItem().Column(b =>
                            {
                                b.Item().Text("RECIBO BONO TRANSPORTE").FontSize(8).Bold();
                                b.Item().Text($"Proveedor: {_d.Proveedor}  |  Lote: #{_d.NumeroLote}  |  Fecha: {_d.FechaLiquidacion:dd/MM/yyyy}").FontSize(7);
                            });
                            row.ConstantItem(120).AlignRight().AlignMiddle()
                                .Text($"Bs {_d.BonoTransporte:N2}").FontSize(14).Bold();
                        });
                    }

                    // ── SEPARADOR CENTRAL ──
                    col.Item().PaddingVertical(4).LineHorizontal(2).LineColor(Colors.Black);

                    // ── COPIA LIQUIDADOR ──
                    col.Item().Element(c => CopiaLiquidacion(c, "COPIA LIQUIDADOR", mostrarControlPago: true));
                });
            });
        }).GeneratePdf();
    }

    // ══════════════════════════════════════════
    // GENERA UNA COPIA (se usa 2 veces)
    // ══════════════════════════════════════════

    private void CopiaLiquidacion(IContainer container, string tipoCopia, bool mostrarControlPago)
    {
        container.Column(col =>
        {
            col.Spacing(2);

            // ── Header ──
            col.Item().Row(row =>
            {
                if (_d.EmpresaLogo != null && _d.EmpresaLogo.Length > 0)
                    row.ConstantItem(35).Image(_d.EmpresaLogo).FitArea();

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignCenter().Text("LIQUIDACIÓN DE MINERALES").FontSize(10).Bold();
                    c.Item().AlignCenter().Text(tipoCopia).FontSize(7).Bold().FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(_d.EmpresaNombre))
                        c.Item().AlignCenter().Text(_d.EmpresaNombre).FontSize(7).Light();
                });

                if (_d.EmpresaLogo != null && _d.EmpresaLogo.Length > 0)
                    row.ConstantItem(35);
            });

            // ── Info lote (2 columnas compactas) — SIN T/C ──
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    I(c, "NOMBRE:", _d.Proveedor);
                    I(c, "CI/NIT:", _d.CiNit);
                    I(c, "MINA:", _d.Mina);
                    I(c, "COOP:", _d.Cooperativa);
                    I(c, "TIPO:", _d.TipoMineral);
                });
                row.RelativeItem().Column(c =>
                {
                    if (_d.NumeroProceso > 0)
                        I(c, "PROCESO N°:", _d.NumeroProceso.ToString());
                    I(c, "LOTE N°:", _d.NumeroLote.ToString());
                    I(c, "PESO NETO:", $"{_d.PesoNeto:N2} Tn");
                    I(c, "F.INGRESO:", _d.FechaIngreso.ToString("dd/MM/yyyy"));
                    I(c, "F.LIQUID:", _d.FechaLiquidacion.ToString("dd/MM/yyyy"));
                });
            });

            // ── Leyes (fila compacta) ──
            col.Item().PaddingTop(2).Row(row =>
            {
                C(row, "ZN", _d.LeyZn); C(row, "AG", _d.LeyAg); C(row, "PB", _d.LeyPb);
                C(row, "HUM%", _d.Humedad); C(row, "LAB", _d.CostoLaboratorio);
                C(row, "P.ZN", _d.ValorBrutoZn > 0 ? _d.ValorBrutoZn / (_d.PesoNetoSeco * _d.LeyZn != 0 ? _d.PesoNetoSeco * _d.LeyZn : 1) : 0);
                C(row, "P.AG", _d.ValorBrutoAg > 0 ? _d.ValorBrutoAg / (_d.PesoNetoSeco * _d.LeyAg != 0 ? _d.PesoNetoSeco * _d.LeyAg : 1) : 0);
            });

            // ── Peso + Valor + Deducciones (3 columnas) ──
            col.Item().PaddingTop(2).Row(row =>
            {
                // Col 1: Peso + Valor
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(p =>
                {
                    p.Item().Text("PESO / VALOR").FontSize(6).Bold().FontColor(Colors.Blue.Darken2);
                    F(p, "Peso Neto", _d.PesoNeto);
                    F(p, $"(-) Hum {_d.Humedad:N2}%", _d.PesoHumedad, neg: true);
                    F(p, "= P. Seco", _d.PesoNetoSeco, bold: true);
                    p.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    F(p, "Zinc", _d.ValorBrutoZn);
                    F(p, "Plata", _d.ValorBrutoAg);
                    F(p, "Plomo", _d.ValorBrutoPb);
                    F(p, "Total $US", _d.ValorComercialUs, bold: true);
                    F(p, "Total Bs", _d.ValorComercialBs, bold: true);
                });
                row.ConstantItem(4);
                // Col 2: Ded. Legales
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(dl =>
                {
                    dl.Item().Text("DED. LEGALES").FontSize(6).Bold().FontColor(Colors.Blue.Darken2);
                    D(dl, "Regalías", "6%", _d.Regalias);
                    D(dl, "CNS", "1.8%", _d.CNS);
                    D(dl, "COMIBOL", "1%", _d.COMIBOL);
                    dl.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    F(dl, "Subtotal", _d.TotalDeduccionesLegales, bold: true);
                });
                row.ConstantItem(4);
                // Col 3: Otras Ded.
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(od =>
                {
                    od.Item().Text("OTRAS DED.").FontSize(6).Bold().FontColor(Colors.Blue.Darken2);
                    D(od, "FENCOMIN", "0.4%", _d.FENCOMIN);
                    D(od, "FEDECOMIN", "1%", _d.FEDECOMIN);
                    if (_d.PorcentajeCooperativa > 0)
                        D(od, "Coop.", $"{_d.PorcentajeCooperativa:N0}%", _d.MontoCooperativa);
                    if (_d.IUE > 0)
                        D(od, "IUE", "5%", _d.IUE);
                    od.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    F(od, "Subtotal", _d.TotalOtrasDeducciones, bold: true);
                });
            });

            // ── Resultado: Valor → Deducciones → Líquido ──
            col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(r =>
                {
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("VALOR COMERCIAL Bs").FontSize(8);
                        rr.ConstantItem(120).AlignRight().Text($"{_d.ValorComercialBs:N2}").FontSize(8).SemiBold();
                    });
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("(-) TOTAL DEDUCCIONES").FontSize(8);
                        rr.ConstantItem(120).AlignRight().Text($"{_d.TotalDeducciones:N2}").FontSize(8).SemiBold().FontColor(Colors.Red.Medium);
                    });
                    r.Item().PaddingVertical(3).LineHorizontal(1.5f).LineColor(Colors.Black);
                    r.Item().Row(rr =>
                    {
                        rr.RelativeItem().Text("LÍQUIDO PAGABLE Bs").FontSize(12).Bold();
                        rr.ConstantItem(150).AlignRight().Text($"{_d.LiquidoPagable:N2}").FontSize(14).Bold();
                    });
                    r.Item().Text($"$US {_d.LiquidoPagableUs:N2}").FontSize(7).Light();
                    r.Item().PaddingTop(2).Text(_d.MontoLiteral).FontSize(7).Italic().Bold();
                });

                // QR
                row.ConstantItem(70).AlignRight().AlignMiddle().Column(q =>
                {
                    var qrBytes = GenerarQR();
                    if (qrBytes != null)
                        q.Item().Width(60).Height(60).Image(qrBytes).FitArea();
                });
            });

            // ── Control de pago (solo en copia liquidador) ──
            // Muestra: Líquido Pagable, (-) Anticipo, = Saldo por pagar.
            // El Bono Transporte NO va aquí; tiene su propio recibo recortable arriba.
            if (mostrarControlPago)
            {
                col.Item().PaddingTop(2).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(cp =>
                {
                    cp.Item().Text("CONTROL DE PAGO").FontSize(6).Bold().FontColor(Colors.Grey.Medium);
                    cp.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Líquido Pagable").FontSize(8);
                        r.ConstantItem(110).AlignRight().Text($"Bs {_d.LiquidoPagable:N2}").FontSize(8).SemiBold();
                    });
                    cp.Item().Row(r =>
                    {
                        r.RelativeItem().Text("(-) Anticipo").FontSize(8);
                        r.ConstantItem(110).AlignRight().Text($"Bs {_d.Anticipo:N2}").FontSize(8).SemiBold().FontColor(Colors.Red.Medium);
                    });
                    cp.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                    cp.Item().Row(r =>
                    {
                        r.RelativeItem().Text("SALDO A PAGAR").Bold().FontSize(10);
                        r.ConstantItem(110).AlignRight().Text($"Bs {_d.SaldoPagar:N2}").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    });
                });
            }

            // ── Firmas ──
            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_d.NombreLiquidador))
                        f.Item().AlignCenter().Text(_d.NombreLiquidador).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("LIQUIDADOR").FontSize(6).Light();
                });
                row.ConstantItem(40);
                row.RelativeItem().AlignCenter().Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(Colors.Black);
                    f.Item().AlignCenter().Text(_d.Proveedor).FontSize(7).Bold();
                    f.Item().AlignCenter().Text("PROVEEDOR - RECIBÍ CONFORME").FontSize(6).Light();
                });
            });
        });
    }

    // ══════════════════════════════════════════
    // QR
    // ══════════════════════════════════════════

    private byte[]? GenerarQR()
    {
        try
        {
            var contenido = $"SILF|L{_d.NumeroLote}|{_d.FechaLiquidacion:yyyyMMdd}|{_d.Proveedor}|Bs{_d.LiquidoPagable:N2}";
            using var qrGen = new QRCodeGenerator();
            using var qrData = qrGen.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(5);
        }
        catch { return null; }
    }

    // ══════════════════════════════════════════
    // Helpers compactos
    // ══════════════════════════════════════════

    private void I(ColumnDescriptor c, string label, string value)
    {
        c.Item().Row(r =>
        {
            r.ConstantItem(65).Text(label).FontSize(7).SemiBold();
            r.RelativeItem().Text(value).FontSize(7);
        });
    }

    private void C(RowDescriptor row, string label, decimal value)
    {
        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Column(c =>
        {
            c.Item().Text(label).FontSize(5).Light();
            c.Item().Text($"{value:N2}").FontSize(7).SemiBold();
        });
    }

    private void F(ColumnDescriptor c, string label, decimal value, bool bold = false, bool neg = false)
    {
        c.Item().PaddingVertical(0.5f).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(7);
            r.ConstantItem(70).AlignRight().Text(t =>
            {
                var s = t.Span($"{value:N2}");
                if (bold) s.Bold();
                if (neg) s.FontColor(Colors.Red.Medium);
            });
        });
    }

    private void D(ColumnDescriptor c, string label, string pct, decimal value)
    {
        c.Item().PaddingVertical(0.5f).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(7);
            r.ConstantItem(30).AlignRight().Text(pct).FontSize(6);
            r.ConstantItem(60).AlignRight().Text($"{value:N2}").FontSize(7).SemiBold();
        });
    }
}
