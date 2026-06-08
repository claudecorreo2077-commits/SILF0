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
using SILF.Core.Enums;
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class FlotacionViewModel : BaseViewModel
{
    // ── Pool: liquidaciones disponibles (liquidadas, sin flotación) ──
    public ObservableCollection<LiquidacionDisponible> Disponibles { get; } = new();

    // ── Flotaciones ya creadas (resumen + acción eliminar) ──
    public ObservableCollection<FlotacionResumen> Flotaciones { get; } = new();

    // ── Detalle consolidado de lotes ya flotados (tabla + Excel) ──
    public ObservableCollection<FlotacionRegistro> Registros { get; } = new();
    public ICollectionView RegistrosView { get; }

    [ObservableProperty] private string _filtroProceso = "Todos";
    [ObservableProperty] private string _filtroMina = "Todos";
    [ObservableProperty] private string _textoBusqueda = "";

    public ObservableCollection<string> FiltrosProceso { get; } = new() { "Todos" };
    public ObservableCollection<string> FiltrosMina { get; } = new() { "Todos" };

    // ── Resumen general (banda superior) ──
    [ObservableProperty] private string _resumenTexto = "Cargando...";
    [ObservableProperty] private int _cantidadFlotaciones;
    [ObservableProperty] private int _cantidadDisponibles;

    // ── Selección del pool ──
    [ObservableProperty] private int _seleccionadasCount;
    [ObservableProperty] private decimal _seleccionadasTotal;

    // ── Totales del detalle ──
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

            // ── 1) POOL: liquidaciones disponibles (liquidadas, sin flotación) ──
            var disp = await db.Lotes
                .Include(l => l.Proveedor).ThenInclude(p => p.Cooperativa)
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Where(l => l.Visible && l.Liquidacion != null && l.ProcesoFlotacionId == null)
                .OrderBy(l => l.NumeroLote)
                .ToListAsync();

            Disponibles.Clear();
            foreach (var l in disp)
            {
                var d = new LiquidacionDisponible
                {
                    LoteId = l.Id,
                    NumeroLote = l.NumeroLote,
                    Proveedor = l.Proveedor.NombreCompleto,
                    Cooperativa = l.Proveedor.Cooperativa?.Nombre ?? "",
                    Mina = l.Mina.Nombre,
                    FechaLiquidacion = l.FechaLiquidacion ?? l.FechaRegistro,
                    PesoNeto = l.PesoNeto,
                    LiquidoPagable = l.Liquidacion!.LiquidoPagable
                };
                d.SelectionChanged = RecalcularSeleccion;
                Disponibles.Add(d);
            }
            CantidadDisponibles = Disponibles.Count;
            RecalcularSeleccion();

            // ── 2) Lotes ya flotados (para resumen de flotaciones + detalle) ──
            var flotados = await db.Lotes
                .Include(l => l.Proveedor).ThenInclude(p => p.Cooperativa)
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Include(l => l.BonoTransporte)
                .Include(l => l.ProcesoFlotacion)
                .Where(l => l.Visible && l.Liquidacion != null && l.ProcesoFlotacionId != null)
                .ToListAsync();

            // Agrupar por flotación, ordenadas de la más nueva a la más vieja
            var grupos = flotados
                .GroupBy(l => l.ProcesoFlotacionId!.Value)
                .OrderByDescending(g => g.First().ProcesoFlotacion!.NumeroProceso)
                .ToList();

            // 2a) Resumen de flotaciones (tarjetas / lista con eliminar)
            Flotaciones.Clear();
            foreach (var g in grupos)
            {
                var first = g.First();
                Flotaciones.Add(new FlotacionResumen
                {
                    ProcesoId = g.Key,
                    NumeroProceso = first.ProcesoFlotacion!.NumeroProceso,
                    Fecha = first.ProcesoFlotacion!.FechaApertura,
                    CantidadLotes = g.Count(),
                    TotalLiquidoPagable = g.Sum(x => x.Liquidacion!.LiquidoPagable)
                });
            }
            CantidadFlotaciones = Flotaciones.Count;

            // 2b) Detalle consolidado, con N° secuencial dentro de cada flotación
            var minas = new HashSet<string>();
            var procesos = new HashSet<string>();

            Registros.Clear();
            foreach (var g in grupos)
            {
                var numProc = g.First().ProcesoFlotacion!.NumeroProceso;
                var procesoLabel = $"Flotación {numProc}";
                procesos.Add(procesoLabel);

                int seq = 1;
                foreach (var l in g.OrderBy(x => x.NumeroLote))
                {
                    var liq = l.Liquidacion!;
                    minas.Add(l.Mina.Nombre);

                    Registros.Add(new FlotacionRegistro
                    {
                        LoteId = l.Id,
                        Proceso = numProc,
                        ProcesoLabel = procesoLabel,
                        NumeroEnProceso = seq++,
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
            }

            // Filtros
            FiltrosProceso.Clear();
            FiltrosProceso.Add("Todos");
            foreach (var p in procesos.OrderByDescending(p => p)) FiltrosProceso.Add(p);
            if (!FiltrosProceso.Contains(FiltroProceso)) FiltroProceso = "Todos";

            FiltrosMina.Clear();
            FiltrosMina.Add("Todos");
            foreach (var m in minas.OrderBy(m => m)) FiltrosMina.Add(m);
            if (!FiltrosMina.Contains(FiltroMina)) FiltroMina = "Todos";

            RegistrosView.Refresh();
            CalcularTotales();

            ResumenTexto =
                $"{CantidadFlotaciones} flotaci{(CantidadFlotaciones == 1 ? "ón creada" : "ones creadas")}  ·  " +
                $"{CantidadDisponibles} liquidaci{(CantidadDisponibles == 1 ? "ón disponible" : "ones disponibles")}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Cargando = false; }
    }

    private void RecalcularSeleccion()
    {
        var sel = Disponibles.Where(d => d.IsSelected).ToList();
        SeleccionadasCount = sel.Count;
        SeleccionadasTotal = sel.Sum(d => d.LiquidoPagable);
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

    // ══════════════════════════════════════════
    // CREAR FLOTACIÓN con las liquidaciones seleccionadas
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task CrearFlotacionAsync()
    {
        var sel = Disponibles.Where(d => d.IsSelected).ToList();
        if (sel.Count == 0)
        {
            MessageBox.Show("Seleccioná al menos una liquidación para armar la flotación.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var total = sel.Sum(s => s.LiquidoPagable);
        var msg =
            $"¿Crear una flotación con {sel.Count} liquidaci{(sel.Count == 1 ? "ón" : "ones")}?\n\n" +
            $"Total Líquido Pagable: Bs {total:N2}";
        if (MessageBox.Show(msg, "Crear Flotación",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var nuevoNum = (await db.ProcesosFlotacion.MaxAsync(p => (int?)p.NumeroProceso) ?? 0) + 1;

            var proc = new ProcesoFlotacion
            {
                NumeroProceso = nuevoNum,
                FechaApertura = DateTime.Now,
                FechaCierre = DateTime.Now,
                Estado = EstadoProcesoFlotacion.Cerrado,
                Observaciones = null
            };
            db.ProcesosFlotacion.Add(proc);
            await db.SaveChangesAsync();

            var ids = sel.Select(s => s.LoteId).ToList();
            var lotes = await db.Lotes.Where(l => ids.Contains(l.Id)).ToListAsync();
            foreach (var l in lotes) l.ProcesoFlotacionId = proc.Id;
            await db.SaveChangesAsync();

            MessageBox.Show(
                $"✓ Flotación {nuevoNum} creada con {sel.Count} liquidaci{(sel.Count == 1 ? "ón" : "ones")}.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);

            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al crear la flotación: {ex.InnerException?.Message ?? ex.Message}",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ══════════════════════════════════════════
    // ELIMINAR FLOTACIÓN — devuelve sus liquidaciones al pool
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task EliminarFlotacionAsync(FlotacionResumen? f)
    {
        if (f == null) return;

        var msg =
            $"¿Eliminar la {f.Label}?\n\n" +
            $"Sus {f.CantidadLotes} liquidaci{(f.CantidadLotes == 1 ? "ón volverá" : "ones volverán")} a quedar " +
            $"disponibles para armar otra flotación.\n\n" +
            $"NO se elimina ninguna liquidación.";
        if (MessageBox.Show(msg, "Eliminar Flotación",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            // 1) Desvincular los lotes (vuelven al pool)
            var lotes = await db.Lotes.Where(l => l.ProcesoFlotacionId == f.ProcesoId).ToListAsync();
            foreach (var l in lotes) l.ProcesoFlotacionId = null;
            await db.SaveChangesAsync();

            // 2) Borrar el registro de flotación (ya sin lotes vinculados)
            var proc = await db.ProcesosFlotacion.FirstOrDefaultAsync(p => p.Id == f.ProcesoId);
            if (proc != null)
            {
                db.ProcesosFlotacion.Remove(proc);
                await db.SaveChangesAsync();
            }

            MessageBox.Show(
                $"✓ {f.Label} eliminada. {lotes.Count} liquidaci{(lotes.Count == 1 ? "ón disponible" : "ones disponibles")} nuevamente.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);

            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al eliminar la flotación: {ex.InnerException?.Message ?? ex.Message}",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

// ══════════════════════════════════════════
// Liquidación disponible (pool, seleccionable con checkbox)
// ══════════════════════════════════════════
public partial class LiquidacionDisponible : ObservableObject
{
    public int LoteId { get; set; }
    public int NumeroLote { get; set; }
    public string Proveedor { get; set; } = "";
    public string Cooperativa { get; set; } = "";
    public string Mina { get; set; } = "";
    public DateTime FechaLiquidacion { get; set; }
    public decimal PesoNeto { get; set; }
    public decimal LiquidoPagable { get; set; }

    [ObservableProperty] private bool _isSelected;

    /// <summary>Callback que el ViewModel engancha para recalcular el resumen de selección.</summary>
    public Action? SelectionChanged;
    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke();
}

// ══════════════════════════════════════════
// Resumen de una flotación creada (lista con eliminar)
// ══════════════════════════════════════════
public class FlotacionResumen
{
    public int ProcesoId { get; set; }
    public int NumeroProceso { get; set; }
    public string Label => $"Flotación {NumeroProceso}";
    public DateTime Fecha { get; set; }
    public int CantidadLotes { get; set; }
    public decimal TotalLiquidoPagable { get; set; }
}

// ══════════════════════════════════════════
// Fila del detalle consolidado
// ══════════════════════════════════════════
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
