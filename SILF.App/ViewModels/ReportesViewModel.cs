// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ReportesViewModel.cs
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

// ══════════════════════════════════════════
// DTOs para combos
// ══════════════════════════════════════════

public class ProveedorComboItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string CiNit { get; set; } = "";
    public string Display => $"{Nombre} ({CiNit})";
}

public class LoteComboItem
{
    public int Id { get; set; }
    public string NumeroLote { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string Display => $"Lote {NumeroLote} — {Proveedor}";
}

public partial class ReportesViewModel : BaseViewModel
{
    private readonly bool _esAdmin;

    public ReportesViewModel(bool esAdmin)
    {
        _esAdmin = esAdmin;
        Titulo = "Reportes";
    }

    // ══════════════════════════════════════════
    // FILTROS COMUNES
    // ══════════════════════════════════════════

    [ObservableProperty] private DateTime _fechaDesde = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    [ObservableProperty] private DateTime _fechaHasta = DateTime.Now;

    // ── Para reporte por proveedor ──
    public ObservableCollection<ProveedorComboItem> Proveedores { get; } = new();
    [ObservableProperty] private ProveedorComboItem? _proveedorSeleccionado;

    // ── Para liquidación individual ──
    public ObservableCollection<LoteComboItem> LotesLiquidados { get; } = new();
    [ObservableProperty] private LoteComboItem? _loteSeleccionado;

    // ── Estado de generación ──
    [ObservableProperty] private bool _generando;
    [ObservableProperty] private string _mensajeEstado = "";

    // ══════════════════════════════════════════
    // CARGAR DATOS INICIALES
    // ══════════════════════════════════════════

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            // Proveedores
            var provs = await db.Proveedores.OrderBy(p => p.NombreCompleto).ToListAsync();
            Proveedores.Clear();
            foreach (var p in provs)
            {
                Proveedores.Add(new ProveedorComboItem
                {
                    Id = p.Id,
                    Nombre = p.NombreCompleto,
                    CiNit = p.CiNit
                });
            }

            // Lotes liquidados
            var lotes = await db.Lotes
                .Include(l => l.Proveedor)
                .Include(l => l.Liquidacion)
                .Where(l => l.Liquidacion != null)
                .OrderByDescending(l => l.FechaRegistro)
                .ToListAsync();

            LotesLiquidados.Clear();
            foreach (var l in lotes)
            {
                LotesLiquidados.Add(new LoteComboItem
                {
                    Id = l.Id,
                    NumeroLote = l.NumeroLote.ToString(),
                    Proveedor = l.Proveedor?.NombreCompleto ?? "—"
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar datos: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Cargando = false; }
    }

    // ══════════════════════════════════════════
    // 1. LIQUIDACIÓN INDIVIDUAL (PDF)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarLiquidacionPdfAsync()
    {
        if (LoteSeleccionado == null)
        {
            MessageBox.Show("Seleccione un lote liquidado.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Guardar Liquidación PDF",
            Filter = "PDF|*.pdf",
            FileName = $"Liquidacion_Lote_{LoteSeleccionado.NumeroLote}.pdf"
        };

        if (sfd.ShowDialog() != true) return;

        Generando = true;
        MensajeEstado = "Generando liquidación PDF...";
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var lote = await db.Lotes
                .Include(l => l.Proveedor).ThenInclude(p => p!.Cooperativa)
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Include(l => l.BonoTransporte)
                .Include(l => l.Pago)
                .FirstOrDefaultAsync(l => l.Id == LoteSeleccionado.Id);

            if (lote?.Liquidacion == null)
            {
                MessageBox.Show("Lote sin liquidación.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            byte[]? logo = null;
            if (empresa?.LogoPath != null && File.Exists(empresa.LogoPath))
                logo = await File.ReadAllBytesAsync(empresa.LogoPath);

            var data = new LiquidacionPdfData
            {
                NumeroLote = lote.NumeroLote,
                FechaIngreso = lote.FechaRegistro,
                FechaLiquidacion = lote.FechaLiquidacion ?? DateTime.Now,
                Proveedor = lote.Proveedor?.NombreCompleto ?? "",
                CiNit = lote.Proveedor?.CiNit ?? "",
                Cooperativa = lote.Proveedor?.Cooperativa?.Nombre ?? "",
                Mina = lote.Mina?.Nombre ?? "",
                TipoMineral = lote.TipoMineral.ToString() ?? "",
                PesoNeto = lote.PesoNeto,
                Humedad = lote.Liquidacion.Humedad,
                PesoHumedad = lote.Liquidacion.PesoHumedad,
                PesoNetoSeco = lote.Liquidacion.PesoNetoSeco,
                LeyZn = lote.LeyZn ?? 0,
                LeyAg = lote.LeyAg ?? 0,
                LeyPb = lote.LeyPb ?? 0,
                CostoLaboratorio = lote.Liquidacion.CostoLaboratorio,
                TipoCambio = lote.Liquidacion.TipoCambio,
                ValorBrutoZn = lote.Liquidacion.ValorBrutoZn,
                ValorBrutoAg = lote.Liquidacion.ValorBrutoAg,
                ValorBrutoPb = lote.Liquidacion.ValorBrutoPb,
                ValorComercialUs = lote.Liquidacion.ValorComercialUs,
                ValorComercialBs = lote.Liquidacion.ValorComercialBs,
                Regalias = lote.Liquidacion.Regalias,
                CNS = lote.Liquidacion.CNS,
                COMIBOL = lote.Liquidacion.COMIBOL,
                TotalDeduccionesLegales = lote.Liquidacion.TotalDeduccionesLegales,
                FENCOMIN = lote.Liquidacion.FENCOMIN,
                FEDECOMIN = lote.Liquidacion.FEDECOMIN,
                PorcentajeCooperativa = lote.Liquidacion.PorcentajeCooperativa,
                MontoCooperativa = lote.Liquidacion.MontoCooperativa,
                IUE = lote.Liquidacion.IUE,
                TotalOtrasDeducciones = lote.Liquidacion.TotalOtrasDeducciones,
                TotalDeducciones = lote.Liquidacion.TotalDeducciones,
                LiquidoPagable = lote.Liquidacion.LiquidoPagable,
                LiquidoPagableUs = lote.Liquidacion.LiquidoPagableUs,
                Anticipo = lote.Pago?.Anticipo ?? 0,
                SaldoPagar = lote.Liquidacion.LiquidoPagable - (lote.Pago?.Anticipo ?? 0),
                BonoTransporte = lote.BonoTransporte?.Monto ?? 0,
                Observaciones = lote.Liquidacion.Observaciones ?? "",
                EmpresaNombre = empresa?.RazonSocial ?? "Empresa Minera",
                NombreLiquidador = empresa?.NombreLiquidador ?? "",
                EmpresaLogo = logo
            };

            var report = new LiquidacionPdfReport(data);
            var bytes = report.Generar();
            await File.WriteAllBytesAsync(sfd.FileName, bytes);
            MensajeEstado = "✓ Liquidación PDF generada";
            AbrirArchivo(sfd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = "";
        }
        finally { Generando = false; }
    }

    // ══════════════════════════════════════════
    // 2. LIQUIDACIÓN CONSOLIDADA (Excel)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarLiquidacionExcelAsync()
    {
        var sfd = new SaveFileDialog
        {
            Title = "Guardar Liquidaciones Excel",
            Filter = "Excel|*.xlsx",
            FileName = $"Liquidaciones_{FechaDesde:yyyyMMdd}_{FechaHasta:yyyyMMdd}.xlsx"
        };

        if (sfd.ShowDialog() != true) return;

        Generando = true;
        MensajeEstado = "Generando Excel de liquidaciones...";
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var lotes = await db.Lotes
                .Include(l => l.Proveedor).ThenInclude(p => p!.Cooperativa)
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Include(l => l.Pago)
                .Include(l => l.BonoTransporte)
                .Where(l => l.Liquidacion != null
                    && l.FechaRegistro >= FechaDesde
                    && l.FechaRegistro <= FechaHasta.AddDays(1))
                .OrderBy(l => l.FechaRegistro)
                .ToListAsync();

            if (!lotes.Any())
            {
                MessageBox.Show("No hay liquidaciones en el rango seleccionado.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                MensajeEstado = "";
                return;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();

            LiquidacionConsolidadaExcel.Generar(lotes, sfd.FileName,
                empresa?.RazonSocial ?? "Empresa Minera",
                FechaDesde, FechaHasta);

            MensajeEstado = $"✓ {lotes.Count} liquidaciones exportadas";
            AbrirArchivo(sfd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = "";
        }
        finally { Generando = false; }
    }

    // ══════════════════════════════════════════
    // 3. FLOTACIÓN CONSOLIDADA (Excel)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarFlotacionExcelAsync()
    {
        var sfd = new SaveFileDialog
        {
            Title = "Guardar Flotación Excel",
            Filter = "Excel|*.xlsx",
            FileName = $"Flotacion_{FechaDesde:yyyyMMdd}_{FechaHasta:yyyyMMdd}.xlsx"
        };

        if (sfd.ShowDialog() != true) return;

        Generando = true;
        MensajeEstado = "Generando Excel de flotación...";
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var lotes = await db.Lotes
                .Include(l => l.Proveedor)
                .Include(l => l.Mina)
                .Include(l => l.Flotacion)
                .Include(l => l.Liquidacion)
                .Where(l => l.Flotacion != null
                    && l.FechaRegistro >= FechaDesde
                    && l.FechaRegistro <= FechaHasta.AddDays(1))
                .OrderBy(l => l.FechaRegistro)
                .ToListAsync();

            if (!lotes.Any())
            {
                MessageBox.Show("No hay registros de flotación en el rango.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                MensajeEstado = "";
                return;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();

            FlotacionConsolidadaExcel.Generar(lotes, sfd.FileName,
                empresa?.RazonSocial ?? "Empresa Minera",
                FechaDesde, FechaHasta);

            MensajeEstado = $"✓ {lotes.Count} registros de flotación exportados";
            AbrirArchivo(sfd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = "";
        }
        finally { Generando = false; }
    }

    // ══════════════════════════════════════════
    // 4. LIBRO DIARIO DE CAJA (PDF)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarLibroDiarioPdfAsync()
    {
        var sfd = new SaveFileDialog
        {
            Title = "Guardar Libro Diario PDF",
            Filter = "PDF|*.pdf",
            FileName = $"LibroDiario_{FechaDesde:yyyyMMdd}_{FechaHasta:yyyyMMdd}.pdf"
        };

        if (sfd.ShowDialog() != true) return;

        Generando = true;
        MensajeEstado = "Generando Libro Diario PDF...";
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            // Recibos del rango
            var recibos = await db.RecibosCaja
                .Where(r => r.Visible && r.Fecha >= FechaDesde && r.Fecha <= FechaHasta.AddDays(1))
                .OrderBy(r => r.Fecha).ThenBy(r => r.NumeroRecibo)
                .ToListAsync();

            // Saldo anterior
            var anteriores = await db.RecibosCaja
                .Where(r => r.Visible && r.Fecha < FechaDesde)
                .ToListAsync();

            decimal saldoAnterior = 0;
            foreach (var ra in anteriores)
            {
                if (ra.TipoMovimiento == "Entrada") saldoAnterior += ra.Monto;
                else saldoAnterior -= ra.Monto;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            byte[]? logo = null;
            if (empresa?.LogoPath != null && File.Exists(empresa.LogoPath))
                logo = await File.ReadAllBytesAsync(empresa.LogoPath);

            LibroDiarioPdfReport.Generar(recibos, sfd.FileName,
                empresa?.RazonSocial ?? "Empresa Minera",
                FechaDesde, FechaHasta, saldoAnterior, logo);

            MensajeEstado = $"✓ Libro diario con {recibos.Count} movimientos";
            AbrirArchivo(sfd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = "";
        }
        finally { Generando = false; }
    }

    // ══════════════════════════════════════════
    // 5. HISTORIAL POR PROVEEDOR (PDF)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GenerarHistorialProveedorPdfAsync()
    {
        if (ProveedorSeleccionado == null)
        {
            MessageBox.Show("Seleccione un proveedor.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Guardar Historial Proveedor PDF",
            Filter = "PDF|*.pdf",
            FileName = $"Historial_{ProveedorSeleccionado.Nombre.Replace(" ", "_")}.pdf"
        };

        if (sfd.ShowDialog() != true) return;

        Generando = true;
        MensajeEstado = "Generando historial del proveedor...";
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var proveedor = await db.Proveedores
                .Include(p => p.Cooperativa)
                .FirstOrDefaultAsync(p => p.Id == ProveedorSeleccionado.Id);

            var lotes = await db.Lotes
                .Include(l => l.Mina)
                .Include(l => l.Liquidacion)
                .Include(l => l.Pago)
                .Include(l => l.BonoTransporte)
                .Where(l => l.ProveedorId == ProveedorSeleccionado.Id)
                .OrderByDescending(l => l.FechaRegistro)
                .ToListAsync();

            if (!lotes.Any())
            {
                MessageBox.Show("El proveedor no tiene lotes registrados.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                MensajeEstado = "";
                return;
            }

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            byte[]? logo = null;
            if (empresa?.LogoPath != null && File.Exists(empresa.LogoPath))
                logo = await File.ReadAllBytesAsync(empresa.LogoPath);

            HistorialProveedorPdfReport.Generar(proveedor!, lotes, sfd.FileName,
                empresa?.RazonSocial ?? "Empresa Minera", logo);

            MensajeEstado = $"✓ Historial con {lotes.Count} lotes";
            AbrirArchivo(sfd.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = "";
        }
        finally { Generando = false; }
    }

    // ══════════════════════════════════════════
    // PERMISOS (visibilidad de secciones)
    // ══════════════════════════════════════════

    /// <summary>Contador solo ve reportes de Caja Chica</summary>
    public Visibility VisibilidadLiquidacion => _esAdmin ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadFlotacion => _esAdmin ? Visibility.Visible : Visibility.Collapsed;

    // ══════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════

    private static void AbrirArchivo(string ruta)
    {
        try
        {
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch { /* silenciar si no puede abrir */ }
    }
}
