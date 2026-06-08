// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LiquidacionListViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SILF.Core.Enums;
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class LiquidacionListViewModel : BaseViewModel
{
    public ObservableCollection<LiquidacionResumen> Items { get; } = new();
    public ICollectionView ItemsView { get; }

    [ObservableProperty] private string _filtroEstado = "Todos";
    [ObservableProperty] private string _textoBusqueda = "";
    public List<string> FiltrosEstado { get; } = new() { "Todos", "Pendientes", "Liquidados" };

    public Func<int, Task>? NavegarALiquidar { get; set; }
    public Func<int, Task>? NavegarAVer { get; set; }

    public LiquidacionListViewModel()
    {
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FiltrarItem;
    }

    partial void OnFiltroEstadoChanged(string value) => ItemsView.Refresh();
    partial void OnTextoBusquedaChanged(string value) => ItemsView.Refresh();

    private bool FiltrarItem(object obj)
    {
        if (obj is not LiquidacionResumen item) return false;
        var pasa = FiltroEstado switch
        {
            "Pendientes" => !item.TieneLiquidacion,
            "Liquidados" => item.TieneLiquidacion,
            _ => true
        };
        if (!pasa) return false;
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var txt = TextoBusqueda.ToUpper();
            return item.Proveedor.ToUpper().Contains(txt) || item.Mina.ToUpper().Contains(txt)
                || item.NumeroLote.ToString().Contains(txt) || item.CiNit.ToUpper().Contains(txt);
        }
        return true;
    }

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var lotes = await db.Lotes.Include(l => l.Proveedor).Include(l => l.Mina)
                .Include(l => l.Liquidacion).Include(l => l.Pago)
                .Where(l => l.Visible).OrderByDescending(l => l.FechaRegistro).ToListAsync();

            Items.Clear();
            foreach (var l in lotes)
            {
                Items.Add(new LiquidacionResumen
                {
                    LoteId = l.Id, NumeroLote = l.NumeroLote,
                    Proveedor = l.Proveedor.NombreCompleto, CiNit = l.Proveedor.CiNit,
                    Mina = l.Mina.Nombre, PesoNeto = l.PesoNeto,
                    TipoMineral = l.TipoMineral?.ToString() ?? "",
                    TieneLiquidacion = l.Liquidacion != null,
                    LiquidoPagable = l.Liquidacion?.LiquidoPagable ?? 0,
                    Fecha = l.FechaRegistro, Estado = l.Liquidacion != null ? "Liquidado" : "Pendiente"
                });
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { Cargando = false; }
    }

    [RelayCommand] private async Task LiquidarAsync(int loteId) { if (NavegarALiquidar != null) await NavegarALiquidar(loteId); }
    [RelayCommand] private async Task VerEditarAsync(int loteId) { if (NavegarAVer != null) await NavegarAVer(loteId); }

    [RelayCommand]
    private async Task EliminarLiquidacionAsync(int loteId)
    {
        var r = MessageBox.Show("¿Está seguro de eliminar esta liquidación?", "SILF",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var lote = await db.Lotes.Include(l => l.Liquidacion).FirstOrDefaultAsync(l => l.Id == loteId);
            if (lote?.Liquidacion != null)
            {
                db.Set<Liquidacion>().Remove(lote.Liquidacion);
                lote.Estado = EstadoLote.Registrado; lote.FechaLiquidacion = null;
                await db.SaveChangesAsync();
            }
            await CargarDatosAsync();
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    // ══════════════════════════════════════════
    // PDF e IMPRIMIR por fila
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task ExportarPdfAsync(int loteId)
    {
        try
        {
            var data = await CargarDatosPdfAsync(loteId);
            if (data == null) return;

            var pdf = new LiquidacionPdfReport(data).Generar();
            var dialog = new SaveFileDialog
            {
                FileName = $"Liquidacion_Lote_{data.NumeroLote}.pdf",
                Filter = "PDF (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, pdf);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error PDF: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    [RelayCommand]
    private async Task ImprimirLiquidacionAsync(int loteId)
    {
        try
        {
            var data = await CargarDatosPdfAsync(loteId);
            if (data == null) return;

            var pdf = new LiquidacionPdfReport(data).Generar();
            var temp = Path.Combine(Path.GetTempPath(), $"Liquidacion_Lote_{data.NumeroLote}.pdf");
            File.WriteAllBytes(temp, pdf);
            Process.Start(new ProcessStartInfo(temp) { UseShellExecute = true, Verb = "print" });
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async Task<LiquidacionPdfData?> CargarDatosPdfAsync(int loteId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

        var lote = await db.Lotes.Include(l => l.Proveedor).ThenInclude(p => p.Cooperativa)
            .Include(l => l.Mina).Include(l => l.Liquidacion).Include(l => l.Pago)
            .Include(l => l.BonoTransporte)
            .FirstOrDefaultAsync(l => l.Id == loteId);
        if (lote?.Liquidacion == null) return null;

        var empresa = await db.Empresas.FirstOrDefaultAsync();
        var liq = lote.Liquidacion;

        return new LiquidacionPdfData
        {
            Proveedor = lote.Proveedor.NombreCompleto, CiNit = lote.Proveedor.CiNit,
            Mina = lote.Mina.Nombre, Cooperativa = lote.Proveedor.Cooperativa?.Nombre ?? "—",
            TipoMineral = lote.TipoMineral?.ToString()?.ToUpper() ?? "",
            NumeroLote = lote.NumeroLote, PesoNeto = lote.PesoNeto,
            FechaIngreso = lote.FechaRegistro, FechaLiquidacion = liq.FechaCalculo ?? DateTime.Today,
            TipoCambio = liq.TipoCambio,
            LeyZn = lote.LeyZn ?? 0, LeyAg = lote.LeyAg ?? 0, LeyPb = lote.LeyPb ?? 0,
            Humedad = liq.Humedad, CostoLaboratorio = liq.CostoLaboratorio,
            PesoHumedad = liq.PesoHumedad, PesoNetoSeco = liq.PesoNetoSeco,
            ValorBrutoZn = liq.ValorBrutoZn, ValorBrutoAg = liq.ValorBrutoAg,
            ValorBrutoPb = liq.ValorBrutoPb, ValorComercialUs = liq.ValorComercialUs,
            ValorComercialBs = liq.ValorComercialBs,
            Regalias = liq.Regalias, CNS = liq.CNS, COMIBOL = liq.COMIBOL,
            TotalDeduccionesLegales = liq.TotalDeduccionesLegales,
            FENCOMIN = liq.FENCOMIN, FEDECOMIN = liq.FEDECOMIN,
            PorcentajeCooperativa = liq.PorcentajeCooperativa,
            MontoCooperativa = liq.MontoCooperativa, IUE = liq.IUE,
            TotalOtrasDeducciones = liq.TotalOtrasDeducciones,
            TotalDeducciones = liq.TotalDeducciones,
            LiquidoPagable = liq.LiquidoPagable, LiquidoPagableUs = liq.LiquidoPagableUs,
            MontoLiteral = LiquidacionViewModel.NumeroALiteral(liq.LiquidoPagable),
            Anticipo = lote.Pago?.Anticipo ?? 0,
            SaldoPagar = liq.LiquidoPagable - (lote.Pago?.Anticipo ?? 0),
            BonoTransporte = lote.BonoTransporte?.Monto ?? 0,
            Observaciones = liq.Observaciones,
            EmpresaNombre = empresa?.RazonSocial ?? "",
            NombreLiquidador = empresa?.NombreLiquidador ?? "",
            EmpresaLogo = !string.IsNullOrEmpty(empresa?.LogoPath) && File.Exists(empresa.LogoPath)
                ? File.ReadAllBytes(empresa.LogoPath) : null
        };
    }

    [RelayCommand]
    private async Task ExportarExcelAsync()
    {
        try
        {
            // Cargar datos de todos los lotes liquidados visibles
            var lotesIds = Items.Where(i => i.TieneLiquidacion).Select(i => i.LoteId).ToList();
            if (!lotesIds.Any()) { MessageBox.Show("No hay liquidaciones para exportar.", "SILF"); return; }

            var datos = new List<SILF.Reports.LiquidacionPdfData>();
            foreach (var id in lotesIds)
            {
                var d = await CargarDatosPdfAsync(id);
                if (d != null) datos.Add(d);
            }

            var excel = SILF.Reports.LiquidacionExcelReport.Generar(datos);
            var dialog = new SaveFileDialog
            {
                FileName = $"Liquidaciones_{DateTime.Now:yyyyMMdd}.xlsx",
                Filter = "Excel (*.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, excel);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex) { MessageBox.Show($"Error Excel: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}

public class LiquidacionResumen
{
    public int LoteId { get; set; }
    public int NumeroLote { get; set; }
    public string Proveedor { get; set; } = "";
    public string CiNit { get; set; } = "";
    public string Mina { get; set; } = "";
    public decimal PesoNeto { get; set; }
    public string TipoMineral { get; set; } = "";
    public bool TieneLiquidacion { get; set; }
    public decimal LiquidoPagable { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
}
