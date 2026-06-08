// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\FlotacionExcelReport.cs
using ClosedXML.Excel;

namespace SILF.Reports;

public class FlotacionExcelData
{
    public int NumeroEnProceso { get; set; }
    public string Proceso { get; set; } = "";
    public string Ticket { get; set; } = "";
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaLiquidacion { get; set; }
    public string Cooperativa { get; set; } = "";
    public string Mina { get; set; } = "";
    public int NumeroLote { get; set; }
    public string Proveedor { get; set; } = "";
    public string Placa { get; set; } = "";
    public decimal PesoBruto { get; set; }
    public decimal Tara { get; set; }
    public decimal PesoNeto { get; set; }
    public decimal LeyZn { get; set; }
    public decimal LeyAg { get; set; }
    public decimal LeyPb { get; set; }
    public decimal ValorComercial { get; set; }
    public decimal Regalias { get; set; }
    public decimal CNS { get; set; }
    public decimal COMIBOL { get; set; }
    public decimal FENCOMIN { get; set; }
    public decimal FEDECOMIN { get; set; }
    public decimal Cooperativa_Ded { get; set; }
    public decimal IUE { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal BonoTransporte { get; set; }
    public decimal LiquidoPagable { get; set; }
    public decimal CostoLaboratorio { get; set; }
}

public class FlotacionExcelReport
{
    public static byte[] Generar(List<FlotacionExcelData> registros, string filtroInfo)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Flotación");
        ws.Style.Font.FontName = "Calibri";
        ws.Style.Font.FontSize = 10;

        int r = 1;

        // Título
        ws.Cell(r, 1).Value = "INVERSIÓN - FLOTACIÓN";
        ws.Range(r, 1, r, 10).Merge().Style.Font.SetBold(true).Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        r++;
        if (!string.IsNullOrEmpty(filtroInfo))
        {
            ws.Cell(r, 1).Value = filtroInfo;
            ws.Range(r, 1, r, 10).Merge().Style.Font.SetItalic(true).Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        r += 2;

        // Headers
        var headers = new[] { "N°", "PROCESO", "TICKET", "F. INGRESO", "F. LIQUIDACIÓN",
            "COOPERATIVA", "MINA", "N° LOTE", "PROVEEDOR", "PLACA",
            "BRUTO", "TARA", "NETO", "ZN", "AG", "PB",
            "VALOR TOTAL", "REGALÍAS", "CNS", "COMIBOL", "FENCOMIN", "FEDECOMIN",
            "COOPERATIVA", "IUE", "TOTAL DEDUCCIONES", "BONO TRANSPORTE",
            "LÍQUIDO PAGABLE", "LABORATORIO" };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(r, c + 1).Value = headers[c];
            ws.Cell(r, c + 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#37474F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        int headerRow = r;
        r++;

        // Datos
        foreach (var d in registros)
        {
            int c = 1;
            ws.Cell(r, c++).Value = d.NumeroEnProceso;
            ws.Cell(r, c++).Value = d.Proceso;
            ws.Cell(r, c++).Value = d.Ticket;
            ws.Cell(r, c++).Value = d.FechaIngreso.ToString("dd/MM/yyyy");
            ws.Cell(r, c++).Value = d.FechaLiquidacion.ToString("dd/MM/yyyy");
            ws.Cell(r, c++).Value = d.Cooperativa;
            ws.Cell(r, c++).Value = d.Mina;
            ws.Cell(r, c++).Value = d.NumeroLote;
            ws.Cell(r, c++).Value = d.Proveedor;
            ws.Cell(r, c++).Value = d.Placa;
            ws.Cell(r, c++).Value = (double)d.PesoBruto;
            ws.Cell(r, c++).Value = (double)d.Tara;
            ws.Cell(r, c++).Value = (double)d.PesoNeto;
            ws.Cell(r, c++).Value = (double)d.LeyZn;
            ws.Cell(r, c++).Value = (double)d.LeyAg;
            ws.Cell(r, c++).Value = (double)d.LeyPb;
            ws.Cell(r, c++).Value = (double)d.ValorComercial;
            ws.Cell(r, c++).Value = (double)d.Regalias;
            ws.Cell(r, c++).Value = (double)d.CNS;
            ws.Cell(r, c++).Value = (double)d.COMIBOL;
            ws.Cell(r, c++).Value = (double)d.FENCOMIN;
            ws.Cell(r, c++).Value = (double)d.FEDECOMIN;
            ws.Cell(r, c++).Value = (double)d.Cooperativa_Ded;
            ws.Cell(r, c++).Value = (double)d.IUE;
            ws.Cell(r, c++).Value = (double)d.TotalDeducciones;
            ws.Cell(r, c++).Value = (double)d.BonoTransporte;
            ws.Cell(r, c++).Value = (double)d.LiquidoPagable;
            ws.Cell(r, c++).Value = (double)d.CostoLaboratorio;
            r++;
        }

        // Fila de totales
        r++;
        ws.Cell(r, 1).Value = "TOTALES";
        ws.Cell(r, 1).Style.Font.SetBold(true);
        ws.Cell(r, 1 + 0).Style.Font.SetBold(true);

        int[] colsNum = { 11, 12, 13, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28 };
        foreach (var c in colsNum)
        {
            var colLetter = ws.Cell(headerRow + 1, c).Address.ColumnLetter;
            ws.Cell(r, c).FormulaA1 = $"SUM({colLetter}{headerRow + 1}:{colLetter}{r - 2})";
            ws.Cell(r, c).Style.Font.SetBold(true).NumberFormat.Format = "#,##0.00";
        }

        ws.Range(r, 1, r, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));

        // Formato numérico para columnas de montos
        for (int row = headerRow + 1; row < r; row++)
        {
            foreach (var c in colsNum)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
        }

        // Ajustar anchos
        ws.Columns().AdjustToContents();

        // Bordes
        var dataRange = ws.Range(headerRow, 1, r, headers.Length);
        dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorderColor(XLColor.LightGray)
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetOutsideBorderColor(XLColor.DarkGray);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
