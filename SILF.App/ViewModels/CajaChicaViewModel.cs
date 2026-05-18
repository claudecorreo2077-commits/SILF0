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

    /// <summary>Admin puede editar y eliminar. Contador solo crear y ver.</summary>
    public bool PuedeEditarEliminar => _esAdmin;

    /// <summary>Callback para navegar a la vista previa del recibo.</summary>
    public Func<int, Task>? NavegarARecibo { get; set; }

    // ══════════════════════════════════════════
    // TAB CONTROL
    // ══════════════════════════════════════════

    [ObservableProperty] private int _tabSeleccionado;

    // ══════════════════════════════════════════
    // TAB 1: LISTA DE RECIBOS
    // ══════════════════════════════════════════

    public ObservableCollection<ReciboItem> Recibos { get; } = new();
    private ICollectionView? _vistaRecibos;

    [ObservableProperty] private string _textoBusqueda = "";
    [ObservableProperty] private int _totalRecibos;
    [ObservableProperty] private bool _sinResultados;

    partial void OnTextoBusquedaChanged(string value)
    {
        _vistaRecibos?.Refresh();
        TotalRecibos = _vistaRecibos?.Cast<object>().Count() ?? 0;
        SinResultados = TotalRecibos == 0 && Recibos.Count > 0;
    }

    private bool FiltrarRecibo(object obj)
    {
        if (obj is not ReciboItem r) return false;
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        var t = TextoBusqueda.Trim().ToUpperInvariant();

        // Buscar por número de recibo
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
    [ObservableProperty] private decimal _totalSalidas;
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

    public List<string> CuentasDisponibles { get; }
    public List<string> TiposMovimiento { get; }
    public ObservableCollection<string> BeneficiariosAnteriores { get; } = new();

    partial void OnFormMontoChanged(decimal value)
    {
        FormMontoLetras = value > 0
            ? NumeroALetras.Convertir(value)
            : "";
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

            // Cargar recibos
            var recibos = await db.RecibosCaja
                .Where(r => r.Visible)
                .OrderByDescending(r => r.NumeroRecibo)
                .ToListAsync();

            Recibos.Clear();
            foreach (var r in recibos)
            {
                Recibos.Add(new ReciboItem
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
                });
            }

            _vistaRecibos = CollectionViewSource.GetDefaultView(Recibos);
            _vistaRecibos.Filter = FiltrarRecibo;
            TotalRecibos = Recibos.Count;
            SinResultados = false;

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

        if (dbExterno != null)
        {
            db = dbExterno;
        }
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

            // Calcular saldo anterior (todos los recibos antes de DiarioDesde)
            var recibosAnteriores = await db.RecibosCaja
                .Where(r => r.Visible && r.Fecha < DiarioDesde)
                .ToListAsync();

            decimal saldoAnterior = 0;
            foreach (var ra in recibosAnteriores)
            {
                if (ra.TipoMovimiento == "Entrada")
                    saldoAnterior += ra.Monto;
                else
                    saldoAnterior -= ra.Monto;
            }

            MovimientosDiario.Clear();

            // Fila de saldo anterior si hay
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
                    Detalle = $"Recibo Nº {r.NumeroRecibo} - {r.Beneficiario}",
                    Entrada = entrada,
                    Salida = salida,
                    Saldo = saldo,
                    Concepto = r.Concepto,
                    Cuenta = r.Cuenta,
                    NumeroRecibo = r.NumeroRecibo
                });
            }

            TotalEntradas = totalEntradas;
            TotalSalidas = totalSalidas;
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

    [RelayCommand]
    private async Task NuevoReciboAsync()
    {
        _editandoId = null;
        DialogoTitulo = "Nuevo Recibo";

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        var maxNum = await db.RecibosCaja.MaxAsync(r => (int?)r.NumeroRecibo) ?? 0;

        FormNumeroRecibo = maxNum + 1;
        FormFecha = DateTime.Now;
        FormBeneficiario = "";
        FormMonto = 0;
        FormMontoLetras = "";
        FormConcepto = "";
        FormCuenta = "";
        FormTipoMovimiento = "Salida";
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
            DialogoTitulo = $"Editar Recibo Nº {recibo.NumeroRecibo}";
            FormNumeroRecibo = recibo.NumeroRecibo;
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
        // Validaciones
        if (string.IsNullOrWhiteSpace(FormBeneficiario))
        {
            MensajeError = "Ingrese el beneficiario";
            TieneError = true; return;
        }
        if (FormMonto <= 0)
        {
            MensajeError = "El monto debe ser mayor a cero";
            TieneError = true; return;
        }
        if (string.IsNullOrWhiteSpace(FormConcepto))
        {
            MensajeError = "Ingrese el concepto";
            TieneError = true; return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            ReciboCaja recibo;
            if (_editandoId.HasValue)
            {
                recibo = await db.RecibosCaja.FindAsync(_editandoId.Value)
                    ?? throw new Exception("Recibo no encontrado");
            }
            else
            {
                recibo = new ReciboCaja();
                recibo.NumeroRecibo = FormNumeroRecibo;
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
            MensajeError = $"Error al guardar: {ex.Message}";
            TieneError = true;
        }
    }

    // ── Eliminar ──

    [RelayCommand]
    private void PedirEliminarRecibo(int reciboId)
    {
        if (!_esAdmin) return;
        var recibo = Recibos.FirstOrDefault(r => r.Id == reciboId);
        if (recibo == null) return;

        _eliminarId = reciboId;
        MensajeConfirmacion = $"¿Desea eliminar el Recibo Nº {recibo.NumeroRecibo}?\n" +
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
    }

    // ── Imprimir Recibo (navega a vista previa) ──

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

    // ══════════════════════════════════════════
    // DATOS PARA IMPRESIÓN DE RECIBO
    // ══════════════════════════════════════════

    /// <summary>
    /// Devuelve los datos de un recibo para imprimir/generar PDF.
    /// </summary>
    public async Task<ReciboCaja?> ObtenerReciboParaImprimirAsync(int reciboId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        return await db.RecibosCaja.FindAsync(reciboId);
    }
}
