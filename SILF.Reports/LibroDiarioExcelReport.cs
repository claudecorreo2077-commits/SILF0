// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\LibroDiarioExcelReport.cs
using ClosedXML.Excel;

namespace SILF.Reports;

public class LibroDiarioExcelRow
{
    public DateTime Fecha { get; set; }
    public string Detalle { get; set; } = "";
    public decimal Entrada { get; set; }
    public decimal Salida { get; set; }
    public decimal Saldo { get; set; }
    public string? Concepto { get; set; }
    public string? Cuenta { get; set; }
}

public class LibroDiarioExcelReport
{
    /// <summary>
    /// Genera el Excel del Libro Diario de Caja Chica.
    /// </summary>
    /// <param name="registros">Filas del libro diario, ya con saldo calculado.</param>
    /// <param name="rangoFiltro">Texto descriptivo del rango filtrado (ej: "Del 01/05/2026 al 24/05/2026").</param>
    /// <param name="empresaNombre">Nombre de la empresa para el header.</param>
    /// <param name="totalEntradas">Total de entradas del rango.</param>
    /// <param name="totalSalidas">Total de salidas del rango.</param>
    /// <param name="saldoFinal">Saldo final del periodo.</param>
    public static byte[] Generar(
        List<LibroDiarioExcelRow> registros,
        string rangoFiltro,
        string empresaNombre,
        decimal totalEntradas,
        decimal totalSalidas,
        decimal saldoFinal)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Libro Diario");
        ws.Style.Font.FontName = "Calibri";
        ws.Style.Font.FontSize = 10;

        int r = 1;

        // ── Título ──
        ws.Cell(r, 1).Value = empresaNombre;
        ws.Range(r, 1, r, 7).Merge()
            .Style.Font.SetBold(true).Font.SetFontSize(13)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;

        ws.Cell(r, 1).Value = "LIBRO DIARIO DE CAJA CHICA";
        ws.Range(r, 1, r, 7).Merge()
            .Style.Font.SetBold(true).Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;

        if (!string.IsNullOrEmpty(rangoFiltro))
        {
            ws.Cell(r, 1).Value = rangoFiltro;
            ws.Range(r, 1, r, 7).Merge()
                .Style.Font.SetItalic(true).Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        r += 2;

        // ── Headers ──
        var headers = new[] { "FECHA", "DETALLE", "ENTRADA (Bs)", "SALIDA (Bs)", "SALDO (Bs)", "CONCEPTO", "CUENTA" };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(r, c + 1).Value = headers[c];
            ws.Cell(r, c + 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#37474F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        int headerRow = r;
        r++;

        // ── Datos ──
        int dataStartRow = r;
        foreach (var d in registros)
        {
            int c = 1;
            ws.Cell(r, c++).Value = d.Fecha.ToString("dd/MM/yyyy");
            ws.Cell(r, c++).Value = d.Detalle;
            ws.Cell(r, c++).Value = (double)d.Entrada;
            ws.Cell(r, c++).Value = (double)d.Salida;
            ws.Cell(r, c++).Value = (double)d.Saldo;
            ws.Cell(r, c++).Value = d.Concepto ?? "";
            ws.Cell(r, c++).Value = d.Cuenta ?? "";

            // Color condicional para entrada/salida
            if (d.Entrada > 0)
                ws.Cell(r, 3).Style.Font.SetFontColor(XLColor.FromHtml("#2E7D32"));  // verde
            if (d.Salida > 0)
                ws.Cell(r, 4).Style.Font.SetFontColor(XLColor.FromHtml("#C62828"));  // rojo

            // Resaltar "SALDO ANTERIOR"
            if (d.Detalle == "SALDO ANTERIOR")
            {
                ws.Range(r, 1, r, 7).Style
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF8E1"))
                    .Font.SetBold(true);
            }
            r++;
        }
        int dataEndRow = r - 1;

        // ── Fila de totales ──
        r++;
        ws.Cell(r, 1).Value = "TOTALES";
        ws.Cell(r, 1).Style.Font.SetBold(true);

        if (dataEndRow >= dataStartRow)
        {
            ws.Cell(r, 3).FormulaA1 = $"SUM(C{dataStartRow}:C{dataEndRow})";
            ws.Cell(r, 4).FormulaA1 = $"SUM(D{dataStartRow}:D{dataEndRow})";
        }
        else
        {
            ws.Cell(r, 3).Value = (double)totalEntradas;
            ws.Cell(r, 4).Value = (double)totalSalidas;
        }
        ws.Cell(r, 5).Value = (double)saldoFinal;

        ws.Range(r, 1, r, 7).Style
            .Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"))
            .Font.SetBold(true);

        ws.Cell(r, 3).Style.Font.SetFontColor(XLColor.FromHtml("#2E7D32")).NumberFormat.Format = "#,##0.00";
        ws.Cell(r, 4).Style.Font.SetFontColor(XLColor.FromHtml("#C62828")).NumberFormat.Format = "#,##0.00";
        ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";

        // ── Formato numérico ──
        int[] colsNum = { 3, 4, 5 };
        for (int row = dataStartRow; row <= dataEndRow; row++)
        {
            foreach (var c in colsNum)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        }

        // ── Anchos de columnas ──
        ws.Column(1).Width = 12;   // Fecha
        ws.Column(2).Width = 40;   // Detalle
        ws.Column(3).Width = 14;   // Entrada
        ws.Column(4).Width = 14;   // Salida
        ws.Column(5).Width = 14;   // Saldo
        ws.Column(6).Width = 30;   // Concepto
        ws.Column(7).Width = 18;   // Cuenta

        // ── Bordes ──
        if (dataEndRow >= dataStartRow)
        {
            var dataRange = ws.Range(headerRow, 1, r, 7);
            dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorderColor(XLColor.LightGray)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.DarkGray);
        }

        // ── Pie con fecha de generación ──
        r += 2;
        ws.Cell(r, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Range(r, 1, r, 7).Merge()
            .Style.Font.SetItalic(true).Font.SetFontSize(8).Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        // ── Freeze pane ──
        ws.SheetView.FreezeRows(headerRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
