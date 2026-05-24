// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\CajaChicaViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SILF.Core.Helpers;
using SILF.Core.Models;
using SILF.Data;

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

    /// <summary>Texto visible del recibo: "INGRESO #001" o "SALIDA #001".</summary>
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

public partial class CajaChicaViewModel : BaseViewModel
{
    private readonly bool _esAdmin;

    public CajaChicaViewModel(bool esAdmin)
    {
        _esAdmin = esAdmin;
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

    public Func<int, Task>? NavegarARecibo { get; set; }

    // ══════════════════════════════════════════
    // TAB CONTROL PRINCIPAL (Recibos / Diario / Arqueo)
    // ══════════════════════════════════════════

    [ObservableProperty] private int _tabSeleccionado;

    /// <summary>Sub-tab dentro de "Recibos": 0 = INGRESOS, 1 = SALIDAS.</summary>
    [ObservableProperty] private int _subTabRecibos;

    partial void OnSubTabRecibosChanged(int value)
    {
        // Si está abierto el diálogo de "Nuevo recibo" cuando el usuario cambia
        // de sub-tab, NO actualizamos (sería confuso). Solo afecta a próximas creaciones.
    }

    // ══════════════════════════════════════════
    // TAB 1: LISTAS DE RECIBOS (separadas)
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
    // TAB 3: ARQUEO
    // ══════════════════════════════════════════

    [ObservableProperty] private decimal _arqueoCaja;
    [ObservableProperty] private decimal _arqueoFisico;
    [ObservableProperty] private decimal _arqueoDiferencia;
    [ObservableProperty] private string _arqueoObservaciones = "";

    partial void OnArqueoFisicoChanged(decimal value)
    {
        ArqueoDiferencia = ArqueoCaja - value;
    }

    // ══════════════════════════════════════════
    // DIÁLOGO: CREAR / EDITAR RECIBO
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nuevo Recibo";

    [ObservableProperty] private int _formNumeroRecibo;
    [ObservableProperty] private string _formNumeroFormateado = "";
    [ObservableProperty] private DateTime _formFecha = DateTime.Now;
    [ObservableProperty] private string _formBeneficiario = "";
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
        // Si el usuario cambia el tipo durante la creación/edición, recalcular
        // el número correlativo del nuevo talonario.
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
                // Editando y NO cambió el tipo: mantener el número original
                var existente = await db.RecibosCaja.FindAsync(_editandoId.Value);
                if (existente != null)
                {
                    FormNumeroRecibo = existente.NumeroRecibo;
                    FormNumeroFormateado = $"{existente.TipoMovimiento.ToUpperInvariant()} #{existente.NumeroRecibo:D3}";
                }
                return;
            }

            // Creando, o editando con cambio de tipo: tomar próximo correlativo del tipo actual
            var max = await db.RecibosCaja
                .Where(r => r.TipoMovimiento == FormTipoMovimiento)
                .MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;
            FormNumeroRecibo = max + 1;
            FormNumeroFormateado = $"{FormTipoMovimiento.ToUpperInvariant()} #{FormNumeroRecibo:D3}";
        }
        catch { /* silencioso para el bind */ }
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

            // Cargar recibos separados por tipo
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

            // Cargar beneficiarios únicos para autocompletar
            var beneficiarios = await db.RecibosCaja
                .Where(r => r.Visible)
                .Select(r => r.Beneficiario)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            BeneficiariosAnteriores.Clear();
            foreach (var b in beneficiarios)
                BeneficiariosAnteriores.Add(b);

            // Cargar libro diario
            await CargarLibroDiarioAsync(db);

            // Cargar saldo para arqueo
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

    // ── Nuevo Recibo ──
    // Pre-selecciona el tipo según el sub-tab activo (0=Entrada, 1=Salida).

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

    // ── Editar Recibo ──

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

    // ── Guardar ──

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

                // Si el tipo cambió, reasignar el número (próximo del nuevo talonario)
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
                // Recalcular el número justo antes de insertar (evita race conditions)
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

    // ── Eliminar ──

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

    // ── Cancelar diálogo ──

    [RelayCommand]
    private void CancelarDialogo()
    {
        DialogoAbierto = false;
        _editandoId = null;
        _tipoOriginalAlEditar = "";
    }

    // ── Imprimir Recibo ──

    [RelayCommand]
    private async Task ImprimirReciboAsync(int reciboId)
    {
        if (NavegarARecibo != null)
            await NavegarARecibo(reciboId);
    }

    // ── Guardar Arqueo ──

    [RelayCommand]
    private async Task GuardarArqueoAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var arqueo = new ArqueoCaja
            {
                Fecha = DateTime.Now,
                SaldoContable = ArqueoCaja,
                SaldoFisico = ArqueoFisico,
                Diferencia = ArqueoDiferencia,
                Observaciones = string.IsNullOrWhiteSpace(ArqueoObservaciones) ? null : ArqueoObservaciones.Trim(),
                RealizadoPor = "Admin"
            };

            db.ArqueosCaja.Add(arqueo);
            await db.SaveChangesAsync();

            MessageBox.Show("Arqueo guardado correctamente.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
