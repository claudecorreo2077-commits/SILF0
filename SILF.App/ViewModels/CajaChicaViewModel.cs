// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\CajaChicaViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SILF.Core.Helpers;
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

// ══════════════════════════════════════════
// DTOs para la vista
// ══════════════════════════════════════════

public class ReciboItem
{
    public int Id { get; set; }
    public int NumeroRecibo { get; set; }
    public DateTime Fecha { get; set; }
    public string Beneficiario { get; set; } = "";
    public decimal Monto { get; set; }
    public string Concepto { get; set; } = "";
    public string? Cuenta { get; set; }
    public string TipoMovimiento { get; set; } = "Salida";
    public string? Observaciones { get; set; }
    public string NumeroFormateado =>
        $"{TipoMovimiento.ToUpperInvariant()} #{NumeroRecibo:D3}";
}

public class DiarioItem
{
    public DateTime Fecha { get; set; }
    public string Detalle { get; set; } = "";
    public decimal Entrada { get; set; }
    public decimal Salida { get; set; }
    public decimal Saldo { get; set; }
    public string? Concepto { get; set; }
    public string? Cuenta { get; set; }
    public int? NumeroRecibo { get; set; }
}

/// <summary>
/// Item del historial de arqueos para mostrar en el DataGrid.
/// </summary>
public class ArqueoHistorialItem
{
    public int Id { get; set; }
    public string IdentificadorUnico { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal SaldoContable { get; set; }
    public decimal SaldoFisico { get; set; }
    public decimal Diferencia { get; set; }
    public string? Observaciones { get; set; }
    public string? RealizadoPor { get; set; }
    public bool Exportado { get; set; }
    public DateTime? FechaExportacion { get; set; }
    public string? OrigenImportacion { get; set; }

    public bool EsImportado => !string.IsNullOrWhiteSpace(OrigenImportacion);

    /// <summary>"Pendiente", "Exportado", "Importado"</summary>
    public string EstadoTexto => EsImportado ? "Importado"
        : Exportado ? "Exportado"
        : "Pendiente";

    /// <summary>Color hex para el chip del estado.</summary>
    public string EstadoColor => EsImportado ? "#2E7D32"     // verde
        : Exportado ? "#1565C0"                              // azul
        : "#757575";                                         // gris
}

public partial class CajaChicaViewModel : BaseViewModel
{
    private readonly bool _esAdmin;
    private readonly string _nombreUsuario;

    public CajaChicaViewModel(bool esAdmin, string nombreUsuario)
    {
        _esAdmin = esAdmin;
        _nombreUsuario = string.IsNullOrWhiteSpace(nombreUsuario) ? "Usuario" : nombreUsuario;
        Titulo = "Caja Chica";
        CuentasDisponibles = new()
        {
            "APORTE", "TRANSPORTE", "ANTICIPOS", "SUELDOS Y SALARIOS",
            "MATERIALES", "SERVICIOS", "COMBUSTIBLE", "OTROS"
        };
        TiposMovimiento = new() { "Entrada", "Salida" };
    }

    // ══════════════════════════════════════════
    // PERMISOS
    // ══════════════════════════════════════════

    public bool PuedeEditarEliminar => _esAdmin;

    /// <summary>Solo Admin puede importar arqueos desde otra PC.</summary>
    public bool PuedeImportar => _esAdmin;

    public Func<int, Task>? NavegarARecibo { get; set; }

    // ══════════════════════════════════════════
    // TAB CONTROL PRINCIPAL
    // ══════════════════════════════════════════

    [ObservableProperty] private int _tabSeleccionado;
    [ObservableProperty] private int _subTabRecibos;
    partial void OnSubTabRecibosChanged(int value) { }

    // ══════════════════════════════════════════
    // TAB 1: LISTAS DE RECIBOS
    // ══════════════════════════════════════════

    public ObservableCollection<ReciboItem> RecibosIngresos { get; } = new();
    public ObservableCollection<ReciboItem> RecibosSalidas { get; } = new();
    private ICollectionView? _vistaIngresos;
    private ICollectionView? _vistaSalidas;

    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private int _totalIngresos;
    [ObservableProperty] private int _totalSalidas;
    [ObservableProperty] private bool _sinResultadosIngresos;
    [ObservableProperty] private bool _sinResultadosSalidas;

    partial void OnTextoBusquedaChanged(string value)
    {
        _vistaIngresos?.Refresh();
        _vistaSalidas?.Refresh();
        TotalIngresos = _vistaIngresos?.Cast<object>().Count() ?? 0;
        TotalSalidas = _vistaSalidas?.Cast<object>().Count() ?? 0;
        SinResultadosIngresos = TotalIngresos == 0 && RecibosIngresos.Count > 0;
        SinResultadosSalidas = TotalSalidas == 0 && RecibosSalidas.Count > 0;
    }

    private bool FiltrarRecibo(object obj)
    {
        if (obj is not ReciboItem r) return false;
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        var t = TextoBusqueda.Trim().ToUpperInvariant();

        if (int.TryParse(t, out int num))
            return r.NumeroRecibo == num;

        return r.Beneficiario.Contains(t, StringComparison.OrdinalIgnoreCase)
            || r.Concepto.Contains(t, StringComparison.OrdinalIgnoreCase)
            || (r.Cuenta?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    // ══════════════════════════════════════════
    // TAB 2: LIBRO DIARIO
    // ══════════════════════════════════════════

    public ObservableCollection<DiarioItem> MovimientosDiario { get; } = new();

    [ObservableProperty] private DateTime _diarioDesde = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    [ObservableProperty] private DateTime _diarioHasta = DateTime.Now;
    [ObservableProperty] private decimal _totalEntradas;
    [ObservableProperty] private decimal _totalSalidasDiario;
    [ObservableProperty] private decimal _saldoFinal;

    // ══════════════════════════════════════════
    // TAB 3: ARQUEO + HISTORIAL DE ARQUEOS
    // ══════════════════════════════════════════

    [ObservableProperty] private decimal _arqueoCaja;
    [ObservableProperty] private decimal _arqueoFisico;
    [ObservableProperty] private decimal _arqueoDiferencia;
    [ObservableProperty] private string _arqueoObservaciones = "";

    partial void OnArqueoFisicoChanged(decimal value)
    {
        ArqueoDiferencia = ArqueoCaja - value;
    }

    // ── Historial de arqueos ──
    public ObservableCollection<ArqueoHistorialItem> HistorialArqueos { get; } = new();

    [ObservableProperty] private DateTime _historialDesde = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    [ObservableProperty] private DateTime _historialHasta = DateTime.Now;
    [ObservableProperty] private int _totalHistorialPendientes;
    [ObservableProperty] private int _totalHistorialExportados;
    [ObservableProperty] private int _totalHistorialImportados;
    [ObservableProperty] private bool _historialSinResultados;

    // ══════════════════════════════════════════
    // DIÁLOGO: CREAR / EDITAR RECIBO
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nuevo Recibo";
    [ObservableProperty] private int _formNumeroRecibo;
    [ObservableProperty] private string _formNumeroFormateado = "";
    [ObservableProperty] private DateTime _formFecha = DateTime.Now;
    [ObservableProperty] private string _formBeneficiario = "";
    [ObservableProperty] private string _labelFormBeneficiario = "Recibido del Sr.(a)";
    [ObservableProperty] private decimal _formMonto;
    [ObservableProperty] private string _formMontoLetras = "";
    [ObservableProperty] private string _formConcepto = "";
    [ObservableProperty] private string _formCuenta = "";
    [ObservableProperty] private string _formTipoMovimiento = "Salida";
    [ObservableProperty] private string _formObservaciones = "";
    [ObservableProperty] private string _mensajeError = "";
    [ObservableProperty] private bool _tieneError;

    private int? _editandoId;
    private string _tipoOriginalAlEditar = "";

    public List<string> CuentasDisponibles { get; }
    public List<string> TiposMovimiento { get; }
    public ObservableCollection<string> BeneficiariosAnteriores { get; } = new();

    partial void OnFormMontoChanged(decimal value)
    {
        FormMontoLetras = value > 0 ? NumeroALetras.Convertir(value) : "";
    }

    partial void OnFormTipoMovimientoChanged(string value)
    {
        LabelFormBeneficiario = value == "Salida"
            ? "Páguese al Sr.(a)"
            : "Recibido del Sr.(a)";
        _ = ActualizarNumeroFormularioAsync();
    }

    private async Task ActualizarNumeroFormularioAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            if (_editandoId.HasValue && FormTipoMovimiento == _tipoOriginalAlEditar)
            {
                var existente = await db.RecibosCaja.FindAsync(_editandoId.Value);
                if (existente != null)
                {
                    FormNumeroRecibo = existente.NumeroRecibo;
                    FormNumeroFormateado = $"{existente.TipoMovimiento.ToUpperInvariant()} #{existente.NumeroRecibo:D3}";
                }
                return;
            }

            var max = await db.RecibosCaja
                .Where(r => r.TipoMovimiento == FormTipoMovimiento)
                .MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;
            FormNumeroRecibo = max + 1;
            FormNumeroFormateado = $"{FormTipoMovimiento.ToUpperInvariant()} #{FormNumeroRecibo:D3}";
        }
        catch { }
    }

    // ══════════════════════════════════════════
    // DIÁLOGO: CONFIRMAR ELIMINAR
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _confirmarEliminarAbierto;
    [ObservableProperty] private string _mensajeConfirmacion = "";
    private int? _eliminarId;

    // ══════════════════════════════════════════
    // COMANDOS
    // ══════════════════════════════════════════

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var recibos = await db.RecibosCaja
                .Where(r => r.Visible)
                .OrderByDescending(r => r.NumeroRecibo)
                .ToListAsync();

            RecibosIngresos.Clear();
            RecibosSalidas.Clear();
            foreach (var r in recibos)
            {
                var item = new ReciboItem
                {
                    Id = r.Id,
                    NumeroRecibo = r.NumeroRecibo,
                    Fecha = r.Fecha,
                    Beneficiario = r.Beneficiario,
                    Monto = r.Monto,
                    Concepto = r.Concepto,
                    Cuenta = r.Cuenta,
                    TipoMovimiento = r.TipoMovimiento,
                    Observaciones = r.Observaciones
                };
                if (r.TipoMovimiento == "Entrada") RecibosIngresos.Add(item);
                else RecibosSalidas.Add(item);
            }

            _vistaIngresos = CollectionViewSource.GetDefaultView(RecibosIngresos);
            _vistaIngresos.Filter = FiltrarRecibo;
            _vistaSalidas = CollectionViewSource.GetDefaultView(RecibosSalidas);
            _vistaSalidas.Filter = FiltrarRecibo;

            TotalIngresos = RecibosIngresos.Count;
            TotalSalidas = RecibosSalidas.Count;
            SinResultadosIngresos = false;
            SinResultadosSalidas = false;

            var beneficiarios = await db.RecibosCaja
                .Where(r => r.Visible)
                .Select(r => r.Beneficiario)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            BeneficiariosAnteriores.Clear();
            foreach (var b in beneficiarios)
                BeneficiariosAnteriores.Add(b);

            await CargarLibroDiarioAsync(db);
            await CargarHistorialArqueosInternoAsync(db);

            ArqueoCaja = SaldoFinal;
            ArqueoDiferencia = ArqueoCaja - ArqueoFisico;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar datos: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Cargando = false; }
    }

    private async Task CargarLibroDiarioAsync(SilfDbContext? dbExterno = null)
    {
        SilfDbContext db;
        IServiceScope? scope = null;

        if (dbExterno != null) db = dbExterno;
        else
        {
            scope = App.Services.CreateScope();
            db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        }

        try
        {
            var recibos = await db.RecibosCaja
                .Where(r => r.Visible && r.Fecha >= DiarioDesde && r.Fecha <= DiarioHasta.AddDays(1))
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.NumeroRecibo)
                .ToListAsync();

            var recibosAnteriores = await db.RecibosCaja
                .Where(r => r.Visible && r.Fecha < DiarioDesde)
                .ToListAsync();

            decimal saldoAnterior = 0;
            foreach (var ra in recibosAnteriores)
            {
                if (ra.TipoMovimiento == "Entrada") saldoAnterior += ra.Monto;
                else saldoAnterior -= ra.Monto;
            }

            MovimientosDiario.Clear();

            if (saldoAnterior != 0 || recibosAnteriores.Count > 0)
            {
                MovimientosDiario.Add(new DiarioItem
                {
                    Fecha = DiarioDesde,
                    Detalle = "SALDO ANTERIOR",
                    Entrada = saldoAnterior > 0 ? saldoAnterior : 0,
                    Salida = 0,
                    Saldo = saldoAnterior,
                    Concepto = "",
                    Cuenta = ""
                });
            }

            decimal saldo = saldoAnterior;
            decimal totalEntradas = 0;
            decimal totalSalidas = 0;

            foreach (var r in recibos)
            {
                decimal entrada = r.TipoMovimiento == "Entrada" ? r.Monto : 0;
                decimal salida = r.TipoMovimiento == "Salida" ? r.Monto : 0;
                saldo += entrada - salida;
                totalEntradas += entrada;
                totalSalidas += salida;

                MovimientosDiario.Add(new DiarioItem
                {
                    Fecha = r.Fecha,
                    Detalle = $"{r.TipoMovimiento.ToUpperInvariant()} #{r.NumeroRecibo:D3} - {r.Beneficiario}",
                    Entrada = entrada,
                    Salida = salida,
                    Saldo = saldo,
                    Concepto = r.Concepto,
                    Cuenta = r.Cuenta,
                    NumeroRecibo = r.NumeroRecibo
                });
            }

            TotalEntradas = totalEntradas;
            TotalSalidasDiario = totalSalidas;
            SaldoFinal = saldo;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    [RelayCommand]
    private async Task FiltrarDiarioAsync()
    {
        await CargarLibroDiarioAsync();
        ArqueoCaja = SaldoFinal;
        ArqueoDiferencia = ArqueoCaja - ArqueoFisico;
    }

    // ══════════════════════════════════════════
    // EXPORTAR LIBRO DIARIO A EXCEL
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task ExportarLibroDiarioExcelAsync()
    {
        if (MovimientosDiario.Count == 0)
        {
            MessageBox.Show("No hay movimientos en el rango seleccionado para exportar.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var empresa = await db.Empresas.FirstOrDefaultAsync();
            var nombreEmpresa = empresa?.RazonSocial ?? "Empresa Minera";

            var dialog = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"LibroDiario_{DiarioDesde:yyyyMMdd}_{DiarioHasta:yyyyMMdd}.xlsx",
                Title = "Exportar Libro Diario a Excel"
            };
            if (dialog.ShowDialog() != true) return;

            var filas = MovimientosDiario.Select(m => new LibroDiarioExcelRow
            {
                Fecha = m.Fecha,
                Detalle = m.Detalle,
                Entrada = m.Entrada,
                Salida = m.Salida,
                Saldo = m.Saldo,
                Concepto = m.Concepto,
                Cuenta = m.Cuenta
            }).ToList();

            var rangoTexto = $"Del {DiarioDesde:dd/MM/yyyy} al {DiarioHasta:dd/MM/yyyy}";

            var bytes = LibroDiarioExcelReport.Generar(
                filas, rangoTexto, nombreEmpresa,
                TotalEntradas, TotalSalidasDiario, SaldoFinal);

            await File.WriteAllBytesAsync(dialog.FileName, bytes);

            MessageBox.Show($"Excel guardado en:\n{dialog.FileName}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ══════════════════════════════════════════
    // HISTORIAL DE ARQUEOS
    // ══════════════════════════════════════════

    private async Task CargarHistorialArqueosInternoAsync(SilfDbContext db)
    {
        var lista = await db.ArqueosCaja
            .Where(a => a.Fecha >= HistorialDesde && a.Fecha <= HistorialHasta.AddDays(1))
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

        HistorialArqueos.Clear();
        foreach (var a in lista)
        {
            HistorialArqueos.Add(new ArqueoHistorialItem
            {
                Id = a.Id,
                IdentificadorUnico = a.IdentificadorUnico,
                Fecha = a.Fecha,
                SaldoContable = a.SaldoContable,
                SaldoFisico = a.SaldoFisico,
                Diferencia = a.Diferencia,
                Observaciones = a.Observaciones,
                RealizadoPor = a.RealizadoPor,
                Exportado = a.Exportado,
                FechaExportacion = a.FechaExportacion,
                OrigenImportacion = a.OrigenImportacion
            });
        }

        TotalHistorialImportados = HistorialArqueos.Count(h => h.EsImportado);
        TotalHistorialExportados = HistorialArqueos.Count(h => h.Exportado && !h.EsImportado);
        TotalHistorialPendientes = HistorialArqueos.Count(h => !h.Exportado && !h.EsImportado);
        HistorialSinResultados = HistorialArqueos.Count == 0;
    }

    [RelayCommand]
    private async Task FiltrarHistorialArqueosAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        await CargarHistorialArqueosInternoAsync(db);
    }

    // ══════════════════════════════════════════
    // EXPORTAR ARQUEOS PENDIENTES (.silf-arqueo)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task ExportarArqueosAsync()
    {
        // Exporta los arqueos pendientes (no exportados todavía y no importados de otra PC).
        var pendientes = HistorialArqueos.Where(h => !h.Exportado && !h.EsImportado).ToList();

        if (pendientes.Count == 0)
        {
            MessageBox.Show(
                "No hay arqueos pendientes de exportar en el rango actual.\n\n" +
                "Sugerencia: ajustá el filtro de fechas si querés exportar arqueos antiguos.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmar = MessageBox.Show(
            $"Se exportarán {pendientes.Count} arqueo(s) pendiente(s) a un archivo .silf-arqueo.\n\n" +
            "El archivo está encriptado y solo se puede abrir desde SILF.\n\n" +
            "¿Desea continuar?",
            "SILF — Exportar Arqueos",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            var nombreEmpresa = empresa?.RazonSocial ?? "Empresa Minera";

            var ids = pendientes.Select(p => p.Id).ToList();
            var arqueos = await db.ArqueosCaja.Where(a => ids.Contains(a.Id)).ToListAsync();

            // Generar archivo
            var bytes = ArqueoExportService.Generar(arqueos, _nombreUsuario, nombreEmpresa);
            var nombreSugerido = ArqueoExportService.SugerirNombreArchivo(_nombreUsuario);

            var dialog = new SaveFileDialog
            {
                Filter = "Archivo SILF Arqueo (*.silf-arqueo)|*.silf-arqueo",
                FileName = nombreSugerido,
                Title = "Guardar archivo de arqueos para el Administrador"
            };
            if (dialog.ShowDialog() != true) return;

            await File.WriteAllBytesAsync(dialog.FileName, bytes);

            // Marcar los arqueos como exportados
            var ahora = DateTime.Now;
            foreach (var a in arqueos)
            {
                a.Exportado = true;
                a.FechaExportacion = ahora;
            }
            await db.SaveChangesAsync();

            MessageBox.Show(
                $"✓ Se exportaron {arqueos.Count} arqueo(s) correctamente.\n\n" +
                $"Archivo: {Path.GetFileName(dialog.FileName)}\n\n" +
                "Entregá el archivo al Administrador para que lo importe.",
                "SILF — Exportación exitosa",
                MessageBoxButton.OK, MessageBoxImage.Information);

            await CargarHistorialArqueosInternoAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar arqueos: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ══════════════════════════════════════════
    // IMPORTAR ARQUEOS (.silf-arqueo) — solo Admin
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task ImportarArqueosAsync()
    {
        if (!_esAdmin)
        {
            MessageBox.Show("Solo el Administrador puede importar arqueos.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Archivo SILF Arqueo (*.silf-arqueo)|*.silf-arqueo|Todos los archivos (*.*)|*.*",
            Title = "Seleccione el archivo de arqueos a importar"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var payload = ArqueoImportService.LeerArchivo(dialog.FileName);

            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            // Obtener identificadores existentes para deduplicar
            var existentes = await db.ArqueosCaja
                .Select(a => a.IdentificadorUnico)
                .ToListAsync();
            var setExistentes = new HashSet<string>(existentes, StringComparer.OrdinalIgnoreCase);

            var (aImportar, duplicados) = ArqueoImportService.Deduplicar(payload, setExistentes);

            if (aImportar.Count == 0)
            {
                MessageBox.Show(
                    $"El archivo contiene {payload.Cantidad} arqueo(s), pero todos ya existen en la base de datos.\n\n" +
                    "No se importó ninguno (todos son duplicados).",
                    "SILF — Importación",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmar = MessageBox.Show(
                $"Archivo: {Path.GetFileName(dialog.FileName)}\n" +
                $"Origen: {ArqueoImportService.GenerarTextoOrigen(payload)}\n\n" +
                $"Total en el archivo:     {payload.Cantidad}\n" +
                $"Se importarán:           {aImportar.Count}\n" +
                $"Duplicados (se omiten):  {duplicados.Count}\n\n" +
                "¿Desea continuar?",
                "SILF — Confirmar Importación",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmar != MessageBoxResult.Yes) return;

            var textoOrigen = ArqueoImportService.GenerarTextoOrigen(payload);
            foreach (var a in aImportar)
            {
                db.ArqueosCaja.Add(new ArqueoCaja
                {
                    IdentificadorUnico = a.IdentificadorUnico,
                    Fecha = a.Fecha,
                    SaldoContable = a.SaldoContable,
                    SaldoFisico = a.SaldoFisico,
                    Diferencia = a.Diferencia,
                    Observaciones = a.Observaciones,
                    RealizadoPor = a.RealizadoPor,
                    Exportado = true,                  // ya fue exportado por el origen
                    FechaExportacion = payload.FechaExportacion,
                    OrigenImportacion = textoOrigen
                });
            }

            await db.SaveChangesAsync();

            MessageBox.Show(
                $"✓ Importación completada.\n\n" +
                $"Arqueos importados: {aImportar.Count}\n" +
                $"Duplicados omitidos: {duplicados.Count}",
                "SILF — Importación exitosa",
                MessageBoxButton.OK, MessageBoxImage.Information);

            await CargarHistorialArqueosInternoAsync(db);
        }
        catch (SilfArqueoFormatException ex)
        {
            MessageBox.Show(
                $"El archivo no es válido o está corrupto:\n\n{ex.Message}",
                "SILF — Archivo inválido",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al importar: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ══════════════════════════════════════════
    // CRUD RECIBOS
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task NuevoReciboAsync()
    {
        _editandoId = null;
        _tipoOriginalAlEditar = "";
        DialogoTitulo = "Nuevo Recibo";

        FormTipoMovimiento = SubTabRecibos == 0 ? "Entrada" : "Salida";
        await ActualizarNumeroFormularioAsync();

        FormFecha = DateTime.Now;
        FormBeneficiario = "";
        FormMonto = 0;
        FormMontoLetras = "";
        FormConcepto = "";
        FormCuenta = "";
        FormObservaciones = "";
        MensajeError = "";
        TieneError = false;
        DialogoAbierto = true;
    }

    [RelayCommand]
    private async Task EditarReciboAsync(int reciboId)
    {
        if (!_esAdmin) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var recibo = await db.RecibosCaja.FindAsync(reciboId);
            if (recibo == null) return;

            _editandoId = recibo.Id;
            _tipoOriginalAlEditar = recibo.TipoMovimiento;
            DialogoTitulo = $"Editar {recibo.TipoMovimiento.ToUpperInvariant()} #{recibo.NumeroRecibo:D3}";
            FormNumeroRecibo = recibo.NumeroRecibo;
            FormNumeroFormateado = $"{recibo.TipoMovimiento.ToUpperInvariant()} #{recibo.NumeroRecibo:D3}";
            FormFecha = recibo.Fecha;
            FormBeneficiario = recibo.Beneficiario;
            FormMonto = recibo.Monto;
            FormMontoLetras = recibo.MontoEnLetras ?? "";
            FormConcepto = recibo.Concepto;
            FormCuenta = recibo.Cuenta ?? "";
            FormTipoMovimiento = recibo.TipoMovimiento;
            FormObservaciones = recibo.Observaciones ?? "";
            MensajeError = "";
            TieneError = false;
            DialogoAbierto = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task GuardarReciboAsync()
    {
        if (string.IsNullOrWhiteSpace(FormBeneficiario))
        { MensajeError = "Ingrese el beneficiario"; TieneError = true; return; }
        if (FormMonto <= 0)
        { MensajeError = "El monto debe ser mayor a cero"; TieneError = true; return; }
        if (string.IsNullOrWhiteSpace(FormConcepto))
        { MensajeError = "Ingrese el concepto"; TieneError = true; return; }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            ReciboCaja recibo;
            if (_editandoId.HasValue)
            {
                recibo = await db.RecibosCaja.FindAsync(_editandoId.Value)
                    ?? throw new Exception("Recibo no encontrado");

                if (FormTipoMovimiento != _tipoOriginalAlEditar)
                {
                    var max = await db.RecibosCaja
                        .Where(r => r.TipoMovimiento == FormTipoMovimiento && r.Id != recibo.Id)
                        .MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;
                    recibo.NumeroRecibo = max + 1;
                }
            }
            else
            {
                var max = await db.RecibosCaja
                    .Where(r => r.TipoMovimiento == FormTipoMovimiento)
                    .MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;
                recibo = new ReciboCaja { NumeroRecibo = max + 1 };
                db.RecibosCaja.Add(recibo);
            }

            recibo.Fecha = FormFecha;
            recibo.Beneficiario = FormBeneficiario.Trim().ToUpperInvariant();
            recibo.Monto = FormMonto;
            recibo.MontoEnLetras = NumeroALetras.Convertir(FormMonto);
            recibo.Concepto = FormConcepto.Trim().ToUpperInvariant();
            recibo.Cuenta = string.IsNullOrWhiteSpace(FormCuenta) ? null : FormCuenta.Trim();
            recibo.TipoMovimiento = FormTipoMovimiento;
            recibo.Observaciones = string.IsNullOrWhiteSpace(FormObservaciones) ? null : FormObservaciones.Trim();

            await db.SaveChangesAsync();
            DialogoAbierto = false;
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}";
            TieneError = true;
        }
    }

    [RelayCommand]
    private void PedirEliminarRecibo(int reciboId)
    {
        if (!_esAdmin) return;
        var recibo = RecibosIngresos.Concat(RecibosSalidas).FirstOrDefault(r => r.Id == reciboId);
        if (recibo == null) return;

        _eliminarId = reciboId;
        MensajeConfirmacion = $"¿Desea eliminar el recibo {recibo.NumeroFormateado}?\n" +
                              $"Beneficiario: {recibo.Beneficiario}\n" +
                              $"Monto: {recibo.Monto:N2} Bs";
        ConfirmarEliminarAbierto = true;
    }

    [RelayCommand]
    private async Task ConfirmarEliminarAsync()
    {
        if (!_eliminarId.HasValue) return;

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var recibo = await db.RecibosCaja.FindAsync(_eliminarId.Value);
            if (recibo != null)
            {
                db.RecibosCaja.Remove(recibo);
                await db.SaveChangesAsync();
            }
            ConfirmarEliminarAbierto = false;
            _eliminarId = null;
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al eliminar: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CancelarEliminar()
    {
        ConfirmarEliminarAbierto = false;
        _eliminarId = null;
    }

    [RelayCommand]
    private void CancelarDialogo()
    {
        DialogoAbierto = false;
        _editandoId = null;
        _tipoOriginalAlEditar = "";
    }

    [RelayCommand]
    private async Task ImprimirReciboAsync(int reciboId)
    {
        if (NavegarARecibo != null)
            await NavegarARecibo(reciboId);
    }

    // ══════════════════════════════════════════
    // GUARDAR ARQUEO
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GuardarArqueoAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var arqueo = new ArqueoCaja
            {
                IdentificadorUnico = Guid.NewGuid().ToString(),
                Fecha = DateTime.Now,
                SaldoContable = ArqueoCaja,
                SaldoFisico = ArqueoFisico,
                Diferencia = ArqueoDiferencia,
                Observaciones = string.IsNullOrWhiteSpace(ArqueoObservaciones) ? null : ArqueoObservaciones.Trim(),
                RealizadoPor = _nombreUsuario,
                Exportado = false,
                FechaExportacion = null,
                OrigenImportacion = null
            };

            db.ArqueosCaja.Add(arqueo);
            await db.SaveChangesAsync();

            MessageBox.Show($"Arqueo guardado correctamente.\nRealizado por: {_nombreUsuario}",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);

            // Limpiar formulario para próximo arqueo
            ArqueoFisico = 0;
            ArqueoObservaciones = "";
            ArqueoDiferencia = ArqueoCaja;

            // Refrescar historial
            await CargarHistorialArqueosInternoAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar arqueo: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task<ReciboCaja?> ObtenerReciboParaImprimirAsync(int reciboId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        return await db.RecibosCaja.FindAsync(reciboId);
    }
}
