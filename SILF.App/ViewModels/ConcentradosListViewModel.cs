// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ConcentradosListViewModel.cs
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SILF.Core.Enums;
using SILF.Core.Helpers;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class ConcentradosListViewModel : BaseViewModel
{
    private List<ConcentradoResumen> _todos = new();

    /// <summary>Navega al formulario nuevo con el tipo elegido.</summary>
    public Action<TipoConcentrado>? NavegarANuevo { get; set; }
    /// <summary>Navega al formulario en modo ediciÃ³n.</summary>
    public Func<int, Task>? NavegarAEditar { get; set; }

    public ConcentradosListViewModel() { Titulo = "Concentrados"; }

    public ObservableCollection<ConcentradoResumen> Filtrados { get; } = new();
    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private bool _sinResultados;

    partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            _todos = await db.Concentrados
                .Where(c => c.Visible)
                .OrderByDescending(c => c.NumeroConcentrado)
                .Select(c => new ConcentradoResumen
                {
                    Id = c.Id,
                    NumeroConcentrado = c.NumeroConcentrado,
                    TipoTexto = c.Tipo == TipoConcentrado.ZnAg ? "ZN-AG" : "AG-PB",
                    ClienteNombre = c.ClienteNombre,
                    NumeroLiquidacion = c.NumeroLiquidacion ?? "",
                    FechaLiquidacion = c.FechaLiquidacion,
                    SaldoPagarBs = c.SaldoPagarBs
                }).ToListAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        { MessageBox.Show($"Error al cargar concentrados:\n{ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { Cargando = false; }
    }

    private void AplicarFiltro()
    {
        var f = _todos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var t = TextoBusqueda.ToUpper();
            f = f.Where(c => c.ClienteNombre.ToUpper().Contains(t)
                || c.NumeroLiquidacion.ToUpper().Contains(t)
                || c.NumeroConcentrado.ToString().Contains(t)
                || c.TipoTexto.ToUpper().Contains(t));
        }
        Filtrados.Clear();
        foreach (var c in f) Filtrados.Add(c);
        SinResultados = Filtrados.Count == 0;
    }

    [RelayCommand]
    private void Nuevo()
    {
        var dlg = new Views.TipoConcentradoDialog { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() == true && dlg.TipoElegido.HasValue)
            NavegarANuevo?.Invoke(dlg.TipoElegido.Value);
    }

    [RelayCommand]
    private async Task EditarAsync(ConcentradoResumen? r)
    {
        if (r != null && NavegarAEditar != null) await NavegarAEditar(r.Id);
    }

    [RelayCommand]
    private async Task EliminarAsync(ConcentradoResumen? r)
    {
        if (r == null) return;
        if (MessageBox.Show($"Â¿Eliminar el Concentrado NÂ° {r.NumeroConcentrado} ({r.ClienteNombre})?\n\nNo se puede deshacer.",
            "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var c = await db.Concentrados.FindAsync(r.Id);
            if (c != null) { db.Concentrados.Remove(c); await db.SaveChangesAsync(); await CargarDatosAsync(); }
        }
        catch (Exception ex)
        { MessageBox.Show($"Error: {ex.InnerException?.Message ?? ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    [RelayCommand]
    private async Task GenerarReciboAsync(ConcentradoResumen? r)
    {
        if (r == null) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var c = await db.Concentrados.FirstOrDefaultAsync(x => x.Id == r.Id);
            if (c == null || string.IsNullOrWhiteSpace(c.ParametrosJson))
            { MessageBox.Show("Concentrado sin datos para el recibo.", "SILF"); return; }

            var input = JsonSerializer.Deserialize<ConcentradoInput>(c.ParametrosJson)!;
            var x = ConcentradoCalculator.Calcular(input);

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            byte[]? logo = null;
            if (empresa != null && !string.IsNullOrEmpty(empresa.LogoPath) && File.Exists(empresa.LogoPath))
                logo = await File.ReadAllBytesAsync(empresa.LogoPath);

            var data = new ConcentradoReciboData
            {
                EmpresaNombre = empresa?.RazonSocial ?? "",
                EmpresaNit = empresa?.NIT ?? "",
                EmpresaMunicipio = empresa?.Municipio ?? "",
                NombreLiquidador = empresa?.NombreLiquidador ?? "",
                Logo = logo,
                TipoConcentrado = c.Tipo == TipoConcentrado.ZnAg ? "ZN-AG" : "AG-PB",
                NumeroLiquidacion = string.IsNullOrWhiteSpace(c.NumeroLiquidacion) ? "â€”" : c.NumeroLiquidacion,
                ClienteNombre = c.ClienteNombre, ClienteCi = c.ClienteCi ?? "",
                Procedencia = c.Procedencia ?? "",
                FechaEntrega = c.FechaEntrega, FechaLiquidacion = c.FechaLiquidacion,
                PesoBruto = c.Tmh, PesoNeto = x.Tms,
                LeyZn = c.LeyZn, LeyAg = c.LeyAg, LeyPb = c.LeyPb,
                LiquidoPagableBs = x.LiquidoPagableBs, Anticipo = x.Anticipo, SaldoPagarBs = x.SaldoPagarBs,
                RegaliaMinera = x.RegaliaZn + x.RegaliaAg + x.RegaliaPb,
                Cns = x.Cns, Comibol = x.Comibol, Fedecomin = x.Fedecomin,
                Fencomin = x.Fencomin, Wilstermann = x.Wilstermann, AporteCoop = x.AporteCoop,
                TotalRetenciones = x.TotalRetenciones
            };

            var sfd = new SaveFileDialog
            {
                FileName = $"ReciboConcentrado_{data.TipoConcentrado}_{c.NumeroConcentrado:000}.pdf",
                Filter = "PDF (*.pdf)|*.pdf"
            };
            if (sfd.ShowDialog() != true) return;
            ConcentradoReciboPdfGenerator.Generar(data, sfd.FileName);
            Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        { MessageBox.Show($"Error al generar recibo: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}

public class ConcentradoResumen
{
    public int Id { get; set; }
    public int NumeroConcentrado { get; set; }
    public string TipoTexto { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public string NumeroLiquidacion { get; set; } = "";
    public DateTime FechaLiquidacion { get; set; }
    public decimal SaldoPagarBs { get; set; }
}
