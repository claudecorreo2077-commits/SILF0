// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LotesViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SILF.Core.Enums;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class LotesViewModel : BaseViewModel
{
    private List<LoteResumen> _todosLosLotes = new();

    /// <summary>Callback para navegar al formulario. null = nuevo, int = editar.</summary>
    public Func<int?, Task>? NavegarAFormulario { get; set; }

    public LotesViewModel()
    {
        Titulo = "Lotes";
        FiltrosEstado = new() { "Todos", "Registrado", "Anticipo", "Laboratorio", "Leyes OK", "Liquidado", "Completado" };
        _filtroEstadoSeleccionado = "Todos";
    }

    public ObservableCollection<LoteResumen> LotesFiltrados { get; } = new();
    public List<string> FiltrosEstado { get; }

    [ObservableProperty] private string _filtroEstadoSeleccionado;
    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private LoteResumen? _loteSeleccionado;
    [ObservableProperty] private bool _sinResultados;

    partial void OnFiltroEstadoSeleccionadoChanged(string value) => AplicarFiltros();
    partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            _todosLosLotes = await db.Lotes
                .Include(l => l.Proveedor).Include(l => l.Mina)
                .OrderByDescending(l => l.FechaRegistro)
                .Select(l => new LoteResumen
                {
                    Id = l.Id, NumeroLote = l.NumeroLote, NombreProveedor = l.Proveedor.NombreCompleto,
                    NombreMina = l.Mina.Nombre, PesoNeto = l.PesoNeto, Estado = l.Estado,
                    TipoMineral = l.TipoMineral, FechaRegistro = l.FechaRegistro
                }).ToListAsync();

            AplicarFiltros();
        }
        catch (Exception ex)
        { MessageBox.Show($"Error al cargar lotes:\n{ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { Cargando = false; }
    }

    private void AplicarFiltros()
    {
        var f = _todosLosLotes.AsEnumerable();
        if (FiltroEstadoSeleccionado != "Todos")
            f = f.Where(l => l.EstadoTexto == FiltroEstadoSeleccionado);
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var t = TextoBusqueda.ToUpper();
            f = f.Where(l => l.NombreProveedor.ToUpper().Contains(t) || l.NombreMina.ToUpper().Contains(t) || l.NumeroLote.ToString().Contains(t));
        }
        LotesFiltrados.Clear();
        foreach (var l in f) LotesFiltrados.Add(l);
        SinResultados = LotesFiltrados.Count == 0;
    }

    [RelayCommand]
    private async Task NuevoLoteAsync() => NavegarAFormulario?.Invoke(null);

    [RelayCommand]
    private async Task EditarLoteAsync(LoteResumen? r)
    {
        if (r != null) NavegarAFormulario?.Invoke(r.Id);
    }

    [RelayCommand]
    private async Task EliminarLoteAsync(LoteResumen? r)
    {
        if (r == null) return;
        if (MessageBox.Show($"¿Eliminar Lote #{r.NumeroLote} de {r.NombreProveedor}?\n\nEsta acción no se puede deshacer.",
            "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var lote = await db.Lotes.FindAsync(r.Id);
            if (lote != null) { db.Lotes.Remove(lote); await db.SaveChangesAsync(); await CargarDatosAsync(); }
        }
        catch (Exception ex)
        { MessageBox.Show($"Error: {ex.InnerException?.Message ?? ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
