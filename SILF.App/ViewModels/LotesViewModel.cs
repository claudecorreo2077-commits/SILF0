// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LotesViewModel.cs
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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

    // ── Proceso de Flotación actual (solo informativo en esta vista) ──
    // El botón FLOTAR (cortar proceso) vive en el módulo Inv. Flotación.
    [ObservableProperty] private int _numeroProcesoActual;
    [ObservableProperty] private DateTime _fechaAperturaProceso;
    [ObservableProperty] private string _indicadorProcesoTexto = "Cargando...";

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

            // Cargar proceso activo (para mostrar en la banda informativa)
            var procesoActivo = await db.ProcesosFlotacion
                .Where(p => p.Estado == EstadoProcesoFlotacion.Abierto)
                .OrderByDescending(p => p.NumeroProceso)
                .FirstOrDefaultAsync();

            if (procesoActivo != null)
            {
                NumeroProcesoActual = procesoActivo.NumeroProceso;
                FechaAperturaProceso = procesoActivo.FechaApertura;
                IndicadorProcesoTexto =
                    $"Trabajando en el Proceso #{procesoActivo.NumeroProceso}  ·  Abierto desde {procesoActivo.FechaApertura:dd/MM/yyyy HH:mm}  ·  El corte se hace desde Inv. Flotación";
            }
            else
            {
                IndicadorProcesoTexto = "⚠ Sin proceso abierto — abrir uno desde Inv. Flotación";
            }

            // Cargar lotes del proceso activo
            _todosLosLotes = await db.Lotes
                .Include(l => l.Proveedor).Include(l => l.Mina)
                .Where(l => procesoActivo == null || l.ProcesoFlotacionId == procesoActivo.Id)
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

    // ══════════════════════════════════════════
    // RECIBO DE ANTICIPO (PDF)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarReciboAnticipoAsync(LoteResumen? r)
    {
        if (r == null) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var lote = await db.Lotes
                .Include(l => l.Proveedor)
                .Include(l => l.Mina)
                .Include(l => l.Pago)
                .Include(l => l.ProcesoFlotacion)
                .FirstOrDefaultAsync(l => l.Id == r.Id);

            if (lote == null)
            {
                MessageBox.Show("Lote no encontrado.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (lote.Pago == null || lote.Pago.Anticipo <= 0)
            {
                MessageBox.Show("Este lote no tiene anticipo registrado.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();

            byte[]? logo = null;
            if (empresa != null && !string.IsNullOrEmpty(empresa.LogoPath) && File.Exists(empresa.LogoPath))
                logo = await File.ReadAllBytesAsync(empresa.LogoPath);

            var data = new ReciboAnticipoData
            {
                EmpresaNombre = empresa?.RazonSocial ?? "",
                EmpresaNit = empresa?.NIT ?? "",
                EmpresaMunicipio = empresa?.Municipio ?? "",
                NombreLiquidador = empresa?.NombreLiquidador ?? "",
                Logo = logo,
                NumeroProceso = lote.ProcesoFlotacion?.NumeroProceso ?? 0,
                NumeroLote = lote.NumeroLote,
                ProveedorNombre = lote.Proveedor.NombreCompleto,
                ProveedorCi = lote.Proveedor.CiNit,
                Mina = lote.Mina.Nombre,
                Monto = lote.Pago.Anticipo,
                Fecha = lote.Pago.FechaAnticipo ?? lote.FechaRegistro
            };

            var sfd = new SaveFileDialog
            {
                FileName = $"ReciboAnticipo_P{data.NumeroProceso:00}-L{data.NumeroLote:000}.pdf",
                Filter = "PDF (*.pdf)|*.pdf",
                Title = "Guardar Recibo de Anticipo"
            };

            if (sfd.ShowDialog() != true) return;

            ReciboAnticipoPdfGenerator.Generar(data, sfd.FileName);

            // Abrir el PDF
            Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar el recibo: {ex.InnerException?.Message ?? ex.Message}",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
