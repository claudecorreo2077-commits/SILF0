// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\FlotacionViewModel.cs
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
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class FlotacionViewModel : BaseViewModel
{
    private const int LotesPorProceso = 70;

    public ObservableCollection<FlotacionRegistro> Registros { get; } = new();
    public ICollectionView RegistrosView { get; }

    [ObservableProperty] private string _filtroProceso = "Todos";
    [ObservableProperty] private string _filtroMina = "Todos";
    [ObservableProperty] private string _textoBusqueda = "";

    public ObservableCollection<string> FiltrosProceso { get; } = new() { "Todos" };
    public ObservableCollection<string> FiltrosMina { get; } = new() { "Todos" };

    // Totales
    [ObservableProperty] private decimal _totalValorComercial;
    [ObservableProperty] private decimal _totalDeducciones;
    [ObservableProperty] private decimal _totalBonoTransporte;
    [ObservableProperty] private decimal _totalLiquidoPagable;
    [ObservableProperty] private decimal _totalLaboratorio;
    [ObservableProperty] private int _cantidadRegistros;

    public FlotacionViewModel()
    {
        RegistrosView = CollectionViewSource.GetDefaultView(Registros);
        RegistrosView.Filter = FiltrarItem;
    }

    partial void OnFiltroProcesoChanged(string value) { RegistrosView.Refresh(); CalcularTotales(); }
    partial void OnFiltroMinaChanged(string value) { RegistrosView.Refresh(); CalcularTotales(); }
    partial void OnTextoBusquedaChanged(string value) { RegistrosView.Refresh(); CalcularTotales(); }

    private bool FiltrarItem(object obj)
    {
        if (obj is not FlotacionRegistro r) return false;
        if (FiltroProceso != "Todos" && r.ProcesoLabel != FiltroProceso) return false;
        if (FiltroMina != "Todos" && r.Mina != FiltroMina) return false;
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var txt = TextoBusqueda.ToUpper();
            return r.Proveedor.ToUpper().Contains(txt) || r.Mina.ToUpper().Contains(txt)
                || r.NumeroLote.ToString().Contains(txt) || r.Cooperativa.ToUpper().Contains(txt);
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

            var lotes = await db.Lotes
                .Include(l => l.Proveedor).ThenInclude(p => p.Cooperativa)
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Include(l => l.BonoTransporte)
                .Where(l => l.Visible && l.Liquidacion != null)
                .OrderBy(l => l.NumeroLote)
                .ToListAsync();

            // Calcular procesos
            var minas = new HashSet<string>();
            var procesos = new HashSet<string>();

            Registros.Clear();
            for (int i = 0; i < lotes.Count; i++)
            {
                var l = lotes[i];
                var liq = l.Liquidacion!;
                var numeroProceso = (i / LotesPorProceso) + 1;
                var numeroEnProceso = (i % LotesPorProceso) + 1;
                var procesoLabel = $"Proceso {numeroProceso:D2}";

                minas.Add(l.Mina.Nombre);
                procesos.Add(procesoLabel);

                Registros.Add(new FlotacionRegistro
                {
                    LoteId = l.Id,
                    Proceso = numeroProceso,
                    ProcesoLabel = procesoLabel,
                    NumeroEnProceso = numeroEnProceso,
                    NumeroLote = l.NumeroLote,
                    Ticket = l.Ticket ?? "",
                    FechaIngreso = l.FechaRegistro,
                    FechaLiquidacion = l.FechaLiquidacion ?? l.FechaRegistro,
                    Cooperativa = l.Proveedor.Cooperativa?.Nombre ?? "",
                    Mina = l.Mina.Nombre,
                    Proveedor = l.Proveedor.NombreCompleto,
                    Placa = l.Placa ?? "",
                    PesoBruto = l.PesoBruto,
                    Tara = l.Tara,
                    PesoNeto = l.PesoNeto,
                    LeyZn = l.LeyZn ?? 0, LeyAg = l.LeyAg ?? 0, LeyPb = l.LeyPb ?? 0,
                    ValorComercial = liq.ValorComercialBs,
                    Regalias = liq.Regalias,
                    CNS = liq.CNS,
                    COMIBOL = liq.COMIBOL,
                    FENCOMIN = liq.FENCOMIN,
                    FEDECOMIN = liq.FEDECOMIN,
                    Cooperativa_Ded = liq.MontoCooperativa,
                    IUE = liq.IUE,
                    TotalDeducciones = liq.TotalDeducciones,
                    BonoTransporte = l.BonoTransporte?.Monto ?? 0,
                    LiquidoPagable = liq.LiquidoPagable,
                    CostoLaboratorio = liq.CostoLaboratorio
                });
            }

            // Filtros
            FiltrosProceso.Clear();
            FiltrosProceso.Add("Todos");
            foreach (var p in procesos.OrderBy(p => p)) FiltrosProceso.Add(p);

            FiltrosMina.Clear();
            FiltrosMina.Add("Todos");
            foreach (var m in minas.OrderBy(m => m)) FiltrosMina.Add(m);

            CalcularTotales();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Cargando = false; }
    }

    private void CalcularTotales()
    {
        var visibles = RegistrosView.Cast<FlotacionRegistro>().ToList();
        CantidadRegistros = visibles.Count;
        TotalValorComercial = visibles.Sum(r => r.ValorComercial);
        TotalDeducciones = visibles.Sum(r => r.TotalDeducciones);
        TotalBonoTransporte = visibles.Sum(r => r.BonoTransporte);
        TotalLiquidoPagable = visibles.Sum(r => r.LiquidoPagable);
        TotalLaboratorio = visibles.Sum(r => r.CostoLaboratorio);
    }

    [RelayCommand]
    private void ExportarExcel()
    {
        try
        {
            var visibles = RegistrosView.Cast<FlotacionRegistro>().ToList();
            if (!visibles.Any()) { MessageBox.Show("No hay registros para exportar.", "SILF"); return; }

            var datos = visibles.Select(r => new FlotacionExcelData
            {
                NumeroEnProceso = r.NumeroEnProceso, Proceso = r.ProcesoLabel,
                Ticket = r.Ticket, FechaIngreso = r.FechaIngreso,
                FechaLiquidacion = r.FechaLiquidacion, Cooperativa = r.Cooperativa,
                Mina = r.Mina, NumeroLote = r.NumeroLote, Proveedor = r.Proveedor,
                Placa = r.Placa, PesoBruto = r.PesoBruto, Tara = r.Tara,
                PesoNeto = r.PesoNeto, LeyZn = r.LeyZn, LeyAg = r.LeyAg, LeyPb = r.LeyPb,
                ValorComercial = r.ValorComercial, Regalias = r.Regalias, CNS = r.CNS,
                COMIBOL = r.COMIBOL, FENCOMIN = r.FENCOMIN, FEDECOMIN = r.FEDECOMIN,
                Cooperativa_Ded = r.Cooperativa_Ded, IUE = r.IUE,
                TotalDeducciones = r.TotalDeducciones, BonoTransporte = r.BonoTransporte,
                LiquidoPagable = r.LiquidoPagable, CostoLaboratorio = r.CostoLaboratorio
            }).ToList();

            var filtro = $"Filtro: {FiltroProceso} | Mina: {FiltroMina} | Registros: {visibles.Count}";
            var excel = FlotacionExcelReport.Generar(datos, filtro);

            var dialog = new SaveFileDialog
            {
                FileName = $"Flotacion_{DateTime.Now:yyyyMMdd}.xlsx",
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

public class FlotacionRegistro
{
    public int LoteId { get; set; }
    public int Proceso { get; set; }
    public string ProcesoLabel { get; set; } = "";
    public int NumeroEnProceso { get; set; }
    public int NumeroLote { get; set; }
    public string Ticket { get; set; } = "";
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaLiquidacion { get; set; }
    public string Cooperativa { get; set; } = "";
    public string Mina { get; set; } = "";
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
