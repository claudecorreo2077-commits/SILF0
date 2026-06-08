// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\LiquidacionExcelReport.cs
using ClosedXML.Excel;

namespace SILF.Reports;

public class LiquidacionExcelReport
{
    public static byte[] Generar(List<LiquidacionPdfData> lotes)
    {
        using var wb = new XLWorkbook();

        foreach (var d in lotes)
        {
            var nombre = $"Lote {d.NumeroLote}";
            if (wb.Worksheets.Any(w => w.Name == nombre))
                nombre = $"Lote {d.NumeroLote}-{d.Proveedor[..Math.Min(5, d.Proveedor.Length)]}";

            var ws = wb.Worksheets.Add(nombre);
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 10;

            int r = 1;

            // ── TÍTULO ──
            ws.Cell(r, 1).Value = "LIQUIDACIÓN DE MINERALES";
            ws.Range(r, 1, r, 6).Merge().Style.Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            r += 2;

            // ── INFO LOTE ──
            Info(ws, ref r, "NOMBRE:", d.Proveedor, "LOTE N°:", d.NumeroLote.ToString());
            Info(ws, ref r, "CI/NIT:", d.CiNit, "PESO NETO:", $"{d.PesoNeto:N2} Tn");
            Info(ws, ref r, "MINA:", d.Mina, "F. INGRESO:", d.FechaIngreso.ToString("dd/MM/yyyy"));
            Info(ws, ref r, "COOPERATIVA:", d.Cooperativa, "F. LIQUIDACIÓN:", d.FechaLiquidacion.ToString("dd/MM/yyyy"));
            Info(ws, ref r, "TIPO:", d.TipoMineral, "T/C:", $"{d.TipoCambio:N2} Bs/$US");
            r++;

            // ── LEYES ──
            Seccion(ws, ref r, "LEYES DEL LABORATORIO");
            Encabezados(ws, r, "ZN (%)", "AG (oz/tc)", "PB (%)", "% Humedad", "Costo Lab. (Bs)");
            r++;
            ws.Cell(r, 1).Value = (double)d.LeyZn;
            ws.Cell(r, 2).Value = (double)d.LeyAg;
            ws.Cell(r, 3).Value = (double)d.LeyPb;
            ws.Cell(r, 4).Value = (double)d.Humedad;
            ws.Cell(r, 5).Value = (double)d.CostoLaboratorio;
            FormatoNumeros(ws, r, 1, 5);
            r += 2;

            // ── CÁLCULO DE PESO ──
            Seccion(ws, ref r, "CÁLCULO DE PESO");
            FilaCalc(ws, ref r, "Peso Neto", d.PesoNeto);
            FilaCalc(ws, ref r, $"(-) Humedad ({d.Humedad:N2}%)", d.PesoHumedad);
            FilaCalcBold(ws, ref r, "= Peso Neto Seco", d.PesoNetoSeco);
            r++;

            // ── VALOR COMERCIAL ──
            Seccion(ws, ref r, "VALOR COMERCIAL");
            FilaCalc(ws, ref r, "Zinc (Seco × Ley ZN × Precio ZN)", d.ValorBrutoZn);
            FilaCalc(ws, ref r, "Plata (Seco × Ley AG × Precio AG)", d.ValorBrutoAg);
            FilaCalc(ws, ref r, "Plomo (Seco × Ley PB × Precio PB)", d.ValorBrutoPb);
            FilaCalcBold(ws, ref r, "Total $US", d.ValorComercialUs);
            FilaCalcBold(ws, ref r, $"Total Bs (× T/C {d.TipoCambio:N2})", d.ValorComercialBs);
            r++;

            // ── DEDUCCIONES LEGALES ──
            Seccion(ws, ref r, "DEDUCCIONES LEGALES");
            FilaDed(ws, ref r, "Regalías Mineras", "6.00%", d.Regalias);
            FilaDed(ws, ref r, "CNS", "1.80%", d.CNS);
            FilaDed(ws, ref r, "COMIBOL", "1.00%", d.COMIBOL);
            FilaCalcBold(ws, ref r, "Subtotal Ded. Legales", d.TotalDeduccionesLegales);
            r++;

            // ── OTRAS DEDUCCIONES ──
            Seccion(ws, ref r, "OTRAS DEDUCCIONES");
            FilaDed(ws, ref r, "FENCOMIN", "0.40%", d.FENCOMIN);
            FilaDed(ws, ref r, "FEDECOMIN", "1.00%", d.FEDECOMIN);
            if (d.PorcentajeCooperativa > 0)
                FilaDed(ws, ref r, "Cooperativa", $"{d.PorcentajeCooperativa:N2}%", d.MontoCooperativa);
            if (d.IUE > 0)
                FilaDed(ws, ref r, "Retenciones IUE Bienes", "5.00%", d.IUE);
            FilaCalcBold(ws, ref r, "Subtotal Otras Ded.", d.TotalOtrasDeducciones);
            r += 2;

            // ── RESULTADO ──
            FilaCalc(ws, ref r, "VALOR COMERCIAL Bs", d.ValorComercialBs);
            FilaCalc(ws, ref r, "(-) TOTAL DEDUCCIONES", d.TotalDeducciones);
            ws.Cell(r - 1, 3).Style.Font.SetFontColor(XLColor.Red);
            r++;
            ws.Cell(r, 1).Value = "LÍQUIDO PAGABLE Bs";
            ws.Cell(r, 3).Value = (double)d.LiquidoPagable;
            ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(r, 1, r, 3).Style.Font.SetBold(true).Font.SetFontSize(14);
            r++;
            FilaCalc(ws, ref r, "LÍQUIDO PAGABLE $US", d.LiquidoPagableUs);
            r++;
            ws.Cell(r, 1).Value = d.MontoLiteral;
            ws.Range(r, 1, r, 5).Merge().Style.Font.SetItalic(true).Font.SetBold(true).Font.SetFontSize(9);
            r += 2;

            // ── CONTROL DE PAGO ──
            Seccion(ws, ref r, "CONTROL DE PAGO");
            FilaCalc(ws, ref r, "Anticipo entregado", d.Anticipo);
            FilaCalc(ws, ref r, "Bono Transporte", d.BonoTransporte);
            FilaCalcBold(ws, ref r, "Saldo por pagar", d.SaldoPagar);

            // Ajustar anchos
            ws.Column(1).Width = 35;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 18;
            ws.Column(4).Width = 18;
            ws.Column(5).Width = 18;
            ws.Column(6).Width = 18;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Helpers ──

    private static void Info(IXLWorksheet ws, ref int r, string l1, string v1, string l2, string v2)
    {
        ws.Cell(r, 1).Value = l1; ws.Cell(r, 1).Style.Font.SetBold(true);
        ws.Cell(r, 2).Value = v1;
        ws.Cell(r, 4).Value = l2; ws.Cell(r, 4).Style.Font.SetBold(true);
        ws.Cell(r, 5).Value = v2;
        r++;
    }

    private static void Seccion(IXLWorksheet ws, ref int r, string titulo)
    {
        ws.Cell(r, 1).Value = titulo;
        ws.Cell(r, 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.DarkBlue);
        r++;
    }

    private static void Encabezados(IXLWorksheet ws, int r, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(r, i + 1).Value = headers[i];
            ws.Cell(r, i + 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);
        }
    }

    private static void FormatoNumeros(IXLWorksheet ws, int r, int c1, int c2)
    {
        for (int c = c1; c <= c2; c++)
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
    }

    private static void FilaCalc(IXLWorksheet ws, ref int r, string label, decimal value)
    {
        ws.Cell(r, 1).Value = label;
        ws.Cell(r, 3).Value = (double)value;
        ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
        r++;
    }

    private static void FilaCalcBold(IXLWorksheet ws, ref int r, string label, decimal value)
    {
        ws.Cell(r, 1).Value = label;
        ws.Cell(r, 1).Style.Font.SetBold(true);
        ws.Cell(r, 3).Value = (double)value;
        ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(r, 3).Style.Font.SetBold(true);
        r++;
    }

    private static void FilaDed(IXLWorksheet ws, ref int r, string label, string pct, decimal value)
    {
        ws.Cell(r, 1).Value = label;
        ws.Cell(r, 2).Value = pct;
        ws.Cell(r, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(r, 3).Value = (double)value;
        ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00";
        r++;
    }
}
