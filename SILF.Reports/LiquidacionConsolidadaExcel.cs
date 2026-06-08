// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\LiquidacionConsolidadaExcel.cs
using ClosedXML.Excel;
using SILF.Core.Models;

namespace SILF.Reports;

public static class LiquidacionConsolidadaExcel
{
    public static void Generar(List<Lote> lotes, string rutaSalida,
        string empresaNombre, DateTime desde, DateTime hasta)
    {
        using var wb = new XLWorkbook();

        // ══════════════════════════════════════════
        // HOJA 1: RESUMEN CONSOLIDADO
        // ══════════════════════════════════════════
        var ws = wb.Worksheets.Add("Resumen");
        ws.Style.Font.FontName = "Calibri";
        ws.Style.Font.FontSize = 10;

        int r = 1;

        // Título
        ws.Cell(r, 1).Value = empresaNombre.ToUpperInvariant();
        ws.Range(r, 1, r, 12).Merge().Style.Font.SetBold(true).Font.SetFontSize(13)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;
        ws.Cell(r, 1).Value = "LIQUIDACIONES CONSOLIDADAS";
        ws.Range(r, 1, r, 12).Merge().Style.Font.SetBold(true).Font.SetFontSize(11)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;
        ws.Cell(r, 1).Value = $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        ws.Range(r, 1, r, 12).Merge().Style.Font.SetFontSize(9)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r += 2;

        // Encabezados
        var headers = new[] { "N°", "Lote", "Fecha", "Proveedor", "Mina", "Tipo",
            "Peso Neto (Tn)", "% Humedad", "Peso Seco (Tn)",
            "Valor $US", "Valor Bs", "Deducciones Bs", "Líquido Pagable Bs" };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(r, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#3F51B5"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        r++;

        // Datos
        int idx = 1;
        decimal totalValorUsd = 0, totalValorBs = 0, totalDeducciones = 0, totalLiquido = 0;

        foreach (var lote in lotes)
        {
            var liq = lote.Liquidacion!;

            ws.Cell(r, 1).Value = idx++;
            ws.Cell(r, 2).Value = lote.NumeroLote;
            ws.Cell(r, 3).Value = lote.FechaRegistro.ToString("dd/MM/yyyy");
            ws.Cell(r, 4).Value = lote.Proveedor?.NombreCompleto ?? "";
            ws.Cell(r, 5).Value = lote.Mina?.Nombre ?? "";
            ws.Cell(r, 6).Value = lote.TipoMineral.ToString();
            ws.Cell(r, 7).Value = lote.PesoNeto;
            ws.Cell(r, 8).Value = liq.Humedad;
            ws.Cell(r, 9).Value = liq.PesoNetoSeco;
            ws.Cell(r, 10).Value = liq.ValorComercialUs;
            ws.Cell(r, 11).Value = liq.ValorComercialBs;
            ws.Cell(r, 12).Value = liq.TotalDeducciones;
            ws.Cell(r, 13).Value = liq.LiquidoPagable;

            // Formato numérico
            for (int c = 7; c <= 13; c++)
                ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(r, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Bordes
            ws.Range(r, 1, r, 13).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            totalValorUsd += liq.ValorComercialUs;
            totalValorBs += liq.ValorComercialBs;
            totalDeducciones += liq.TotalDeducciones;
            totalLiquido += liq.LiquidoPagable;
            r++;
        }

        // Fila totales
        ws.Cell(r, 1).Value = "TOTALES";
        ws.Range(r, 1, r, 9).Merge().Style.Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(r, 10).Value = totalValorUsd;
        ws.Cell(r, 11).Value = totalValorBs;
        ws.Cell(r, 12).Value = totalDeducciones;
        ws.Cell(r, 13).Value = totalLiquido;
        for (int c = 10; c <= 13; c++)
        {
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, c).Style.Font.SetBold(true);
        }
        ws.Range(r, 1, r, 13).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));

        // Ajustar anchos
        ws.Columns(1, 13).AdjustToContents();
        ws.Column(4).Width = 25; // Proveedor más ancho

        // ══════════════════════════════════════════
        // HOJAS INDIVIDUALES POR LOTE
        // ══════════════════════════════════════════
        foreach (var lote in lotes)
        {
            var liq = lote.Liquidacion!;
            var nombre = $"Lote {lote.NumeroLote}";
            // Evitar nombres duplicados
            if (wb.Worksheets.Any(w => w.Name == nombre))
                nombre = $"{nombre}-{lote.Id}";
            // Limitar a 31 caracteres (límite Excel)
            if (nombre.Length > 31) nombre = nombre[..31];

            var wl = wb.Worksheets.Add(nombre);
            wl.Style.Font.FontName = "Calibri";
            wl.Style.Font.FontSize = 10;

            int lr = 1;

            // Título
            wl.Cell(lr, 1).Value = "LIQUIDACIÓN DE MINERALES";
            wl.Range(lr, 1, lr, 6).Merge().Style.Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            lr += 2;

            // Info del lote
            LoteInfo(wl, ref lr, "PROVEEDOR:", lote.Proveedor?.NombreCompleto ?? "", "LOTE N°:", lote.NumeroLote.ToString());
            LoteInfo(wl, ref lr, "CI/NIT:", lote.Proveedor?.CiNit ?? "", "PESO NETO:", $"{lote.PesoNeto:N2} Tn");
            LoteInfo(wl, ref lr, "MINA:", lote.Mina?.Nombre ?? "", "FECHA:", lote.FechaRegistro.ToString("dd/MM/yyyy"));
            LoteInfo(wl, ref lr, "TIPO:", lote.TipoMineral.ToString() ?? "", "T/C:", $"{liq.TipoCambio:N2} Bs/$US");
            lr++;

            // Leyes
            SeccionTitulo(wl, ref lr, "LEYES DEL LABORATORIO");
            EncabezadosFila(wl, lr, "ZN (%)", "AG (oz/tc)", "PB (%)", "% Humedad");
            lr++;
            wl.Cell(lr, 1).Value = lote.LeyZn ?? 0; wl.Cell(lr, 2).Value = lote.LeyAg ?? 0;
            wl.Cell(lr, 3).Value = lote.LeyPb ?? 0; wl.Cell(lr, 4).Value = liq.Humedad;
            for (int c = 1; c <= 4; c++) wl.Cell(lr, c).Style.NumberFormat.Format = "#,##0.00";
            lr += 2;

            // Cálculo
            SeccionTitulo(wl, ref lr, "CÁLCULO DE LIQUIDACIÓN");
            FilaCalculo(wl, ref lr, "Peso Neto Seco (Tn)", liq.PesoNetoSeco);
            FilaCalculo(wl, ref lr, "Valor Comercial ($US)", liq.ValorComercialUs);
            FilaCalculo(wl, ref lr, "Tipo de Cambio", liq.TipoCambio);
            FilaCalculo(wl, ref lr, "Valor Comercial (Bs)", liq.ValorComercialBs);
            lr++;

            // Deducciones
            SeccionTitulo(wl, ref lr, "DEDUCCIONES");
            FilaCalculo(wl, ref lr, "Regalías (6%)", liq.Regalias);
            FilaCalculo(wl, ref lr, "CNS (1.8%)", liq.CNS);
            FilaCalculo(wl, ref lr, "COMIBOL (1%)", liq.COMIBOL);
            FilaCalculo(wl, ref lr, "FENCOMIN (0.4%)", liq.FENCOMIN);
            FilaCalculo(wl, ref lr, "FEDECOMIN (1%)", liq.FEDECOMIN);
            FilaCalculo(wl, ref lr, "Cooperativa", liq.MontoCooperativa);
            FilaCalculo(wl, ref lr, "IUE (5%)", liq.IUE);

            var anticipo = lote.Pago?.Anticipo ?? 0;
            if (anticipo > 0) FilaCalculo(wl, ref lr, "Anticipo", anticipo);

            FilaCalculo(wl, ref lr, "Total Deducciones", liq.TotalDeducciones);
            lr++;

            // Líquido pagable
            wl.Cell(lr, 1).Value = "LÍQUIDO PAGABLE (Bs)";
            wl.Cell(lr, 2).Value = liq.LiquidoPagable;
            wl.Cell(lr, 1).Style.Font.SetBold(true).Font.SetFontSize(12);
            wl.Cell(lr, 2).Style.Font.SetBold(true).Font.SetFontSize(12)
                .NumberFormat.Format = "#,##0.00";

            wl.Columns(1, 6).AdjustToContents();
        }

        wb.SaveAs(rutaSalida);
    }

    private static void LoteInfo(IXLWorksheet ws, ref int r,
        string label1, string val1, string label2, string val2)
    {
        ws.Cell(r, 1).Value = label1; ws.Cell(r, 1).Style.Font.SetBold(true);
        ws.Cell(r, 2).Value = val1;
        ws.Cell(r, 4).Value = label2; ws.Cell(r, 4).Style.Font.SetBold(true);
        ws.Cell(r, 5).Value = val2;
        r++;
    }

    private static void SeccionTitulo(IXLWorksheet ws, ref int r, string titulo)
    {
        ws.Cell(r, 1).Value = titulo;
        ws.Range(r, 1, r, 4).Merge().Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));
        r++;
    }

    private static void EncabezadosFila(IXLWorksheet ws, int r, params string[] headers)
    {
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(r, c + 1).Value = headers[c];
            ws.Cell(r, c + 1).Style.Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
    }

    private static void FilaCalculo(IXLWorksheet ws, ref int r, string concepto, decimal valor)
    {
        ws.Cell(r, 1).Value = concepto;
        ws.Cell(r, 2).Value = valor;
        ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0.00";
        r++;
    }
}
