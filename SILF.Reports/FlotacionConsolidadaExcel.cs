// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\FlotacionConsolidadaExcel.cs
using ClosedXML.Excel;
using SILF.Core.Models;

namespace SILF.Reports;

public static class FlotacionConsolidadaExcel
{
    public static void Generar(List<Lote> lotes, string rutaSalida,
        string empresaNombre, DateTime desde, DateTime hasta)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Flotación");
        ws.Style.Font.FontName = "Calibri";
        ws.Style.Font.FontSize = 9;

        int r = 1;

        // Título
        ws.Cell(r, 1).Value = empresaNombre.ToUpperInvariant();
        ws.Range(r, 1, r, 20).Merge().Style.Font.SetBold(true).Font.SetFontSize(13)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;
        ws.Cell(r, 1).Value = "INVERSIÓN - FLOTACIÓN";
        ws.Range(r, 1, r, 20).Merge().Style.Font.SetBold(true).Font.SetFontSize(11)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;
        ws.Cell(r, 1).Value = $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        ws.Range(r, 1, r, 20).Merge().Style.Font.SetFontSize(9)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r += 2;

        // Encabezados
        var headers = new[]
        {
            "N°", "LOTE", "FECHA", "COOPERATIVA", "MINA", "PROVEEDOR",
            "BRUTO", "TARA", "NETO", "ZN", "AG", "PB",
            "VALOR TOTAL", "REGALÍAS", "CNS", "COMIBOL", "FENCOMIN", "FEDECOMIN",
            "COOPERATIVA", "IUE", "TOTAL DED.", "BONO TRANSP.",
            "LÍQUIDO PAGABLE", "COSTO LAB."
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(r, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold(true).Font.SetFontSize(8)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#3F51B5"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetWrapText(true)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        r++;

        // Datos
        int idx = 1;
        decimal totalValor = 0, totalDed = 0, totalBono = 0, totalLiquido = 0, totalLab = 0;

        foreach (var lote in lotes)
        {
            var liq = lote.Liquidacion!;

            ws.Cell(r, 1).Value = idx++;
            ws.Cell(r, 2).Value = lote.NumeroLote;
            ws.Cell(r, 3).Value = lote.FechaRegistro.ToString("dd/MM/yyyy");
            ws.Cell(r, 4).Value = lote.Proveedor?.Cooperativa?.Nombre ?? "";
            ws.Cell(r, 5).Value = lote.Mina?.Nombre ?? "";
            ws.Cell(r, 6).Value = lote.Proveedor?.NombreCompleto ?? "";
            ws.Cell(r, 7).Value = lote.PesoBruto;
            ws.Cell(r, 8).Value = lote.Tara;
            ws.Cell(r, 9).Value = lote.PesoNeto;
            ws.Cell(r, 10).Value = lote.LeyZn ?? 0;
            ws.Cell(r, 11).Value = lote.LeyAg ?? 0;
            ws.Cell(r, 12).Value = lote.LeyPb ?? 0;
            ws.Cell(r, 13).Value = liq.ValorComercialBs;
            ws.Cell(r, 14).Value = liq.Regalias;
            ws.Cell(r, 15).Value = liq.CNS;
            ws.Cell(r, 16).Value = liq.COMIBOL;
            ws.Cell(r, 17).Value = liq.FENCOMIN;
            ws.Cell(r, 18).Value = liq.FEDECOMIN;
            ws.Cell(r, 19).Value = liq.MontoCooperativa;
            ws.Cell(r, 20).Value = liq.IUE;
            ws.Cell(r, 21).Value = liq.TotalDeducciones;
            ws.Cell(r, 22).Value = lote.BonoTransporte?.Monto ?? 0;
            ws.Cell(r, 23).Value = liq.LiquidoPagable;
            ws.Cell(r, 24).Value = liq.CostoLaboratorio;

            // Formato numérico
            for (int c = 7; c <= 24; c++)
                ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(r, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Range(r, 1, r, 24).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            totalValor += liq.ValorComercialBs;
            totalDed += liq.TotalDeducciones;
            totalBono += lote.BonoTransporte?.Monto ?? 0;
            totalLiquido += liq.LiquidoPagable;
            totalLab += liq.CostoLaboratorio;
            r++;
        }

        // Fila totales
        ws.Cell(r, 1).Value = "TOTALES";
        ws.Range(r, 1, r, 12).Merge().Style.Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(r, 13).Value = totalValor;
        ws.Cell(r, 21).Value = totalDed;
        ws.Cell(r, 22).Value = totalBono;
        ws.Cell(r, 23).Value = totalLiquido;
        ws.Cell(r, 24).Value = totalLab;
        for (int c = 13; c <= 24; c++)
        {
            ws.Cell(r, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, c).Style.Font.SetBold(true);
        }
        ws.Range(r, 1, r, 24).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));

        // Ajustar anchos
        ws.Columns(1, 24).AdjustToContents();
        ws.Column(6).Width = 22; // Proveedor

        wb.SaveAs(rutaSalida);
    }
}
