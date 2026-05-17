// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\InicioViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SILF.Core.Enums;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class InicioViewModel : BaseViewModel
{
    /// <summary>Callback para navegar a cualquier módulo incluyendo "NuevoLote".</summary>
    public Func<string, Task>? NavegarCallback { get; set; }

    public InicioViewModel() { Titulo = "Dashboard"; }

    [ObservableProperty] private int _totalRegistrados;
    [ObservableProperty] private int _totalEnLaboratorio;
    [ObservableProperty] private int _totalPorLiquidar;
    [ObservableProperty] private int _totalCompletadosMes;
    [ObservableProperty] private decimal _pesoTotalMes;
    [ObservableProperty] private int _totalLotes;
    [ObservableProperty] private bool _sinLotes = true;

    public ObservableCollection<LoteResumen> UltimosLotes { get; } = new();

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            TotalRegistrados = await db.Lotes.CountAsync(l => l.Estado == EstadoLote.Registrado || l.Estado == EstadoLote.AnticipoPagado);
            TotalEnLaboratorio = await db.Lotes.CountAsync(l => l.Estado == EstadoLote.EnLaboratorio);
            TotalPorLiquidar = await db.Lotes.CountAsync(l => l.Estado == EstadoLote.LeyesRegistradas);
            TotalCompletadosMes = await db.Lotes.CountAsync(l => l.Estado == EstadoLote.Completado && l.FechaCompletado >= inicioMes);
            PesoTotalMes = await db.Lotes.Where(l => l.FechaRegistro >= inicioMes).SumAsync(l => l.PesoNeto);
            TotalLotes = await db.Lotes.CountAsync();

            var lotes = await db.Lotes.Include(l => l.Proveedor).Include(l => l.Mina)
                .OrderByDescending(l => l.FechaRegistro).Take(15)
                .Select(l => new LoteResumen
                {
                    Id = l.Id, NumeroLote = l.NumeroLote, NombreProveedor = l.Proveedor.NombreCompleto,
                    NombreMina = l.Mina.Nombre, PesoNeto = l.PesoNeto, Estado = l.Estado,
                    TipoMineral = l.TipoMineral, FechaRegistro = l.FechaRegistro
                }).ToListAsync();

            UltimosLotes.Clear();
            foreach (var l in lotes) UltimosLotes.Add(l);
            SinLotes = UltimosLotes.Count == 0;
        }
        catch (Exception ex)
        { MessageBox.Show($"Error al cargar dashboard:\n{ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task NuevoLoteAsync() => NavegarCallback?.Invoke("NuevoLote");

    [RelayCommand]
    private async Task NuevoReciboAsync() => NavegarCallback?.Invoke("Caja Chica");
}
