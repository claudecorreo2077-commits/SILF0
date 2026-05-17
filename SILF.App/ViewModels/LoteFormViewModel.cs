// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LoteFormViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SILF.Core.Enums;
using SILF.Core.Models;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class LoteFormViewModel : BaseViewModel
{
    private int? _loteEditandoId;
    private int? _proveedorSeleccionadoId;

    public Action? OnGuardado { get; set; }
    public Action? OnCancelado { get; set; }

    public LoteFormViewModel()
    {
        TiposMineral = new() { "COMPLEJO", "BROSA" };
    }

    [ObservableProperty] private string _tituloFormulario = "Nuevo Lote";
    [ObservableProperty] private DateTime _formFechaIngreso = DateTime.Today;
    [ObservableProperty] private string _formCiNit = "";
    [ObservableProperty] private string _formNombreProveedor = "";
    [ObservableProperty] private bool _mostrarSugerencias;
    public ObservableCollection<ProveedorSugerencia> SugerenciasProveedor { get; } = new();
    private ProveedorSugerencia? _proveedorSugerido;
    public ProveedorSugerencia? ProveedorSugerido
    {
        get => _proveedorSugerido;
        set { if (SetProperty(ref _proveedorSugerido, value) && value != null) SeleccionarProveedor(value); }
    }

    public ObservableCollection<Cooperativa> Cooperativas { get; } = new();
    public ObservableCollection<Mina> Minas { get; } = new();
    [ObservableProperty] private Cooperativa? _formCooperativa;
    [ObservableProperty] private Mina? _formMina;

    public List<string> TiposMineral { get; }
    [ObservableProperty] private string? _formTipoMineral;

    [ObservableProperty] private decimal _formPesoBruto;
    [ObservableProperty] private decimal _formTara;
    [ObservableProperty] private decimal _formPesoNeto;
    partial void OnFormPesoBrutoChanged(decimal value) { if (value > 0) FormPesoNeto = value - FormTara; }
    partial void OnFormTaraChanged(decimal value) { if (FormPesoBruto > 0) FormPesoNeto = FormPesoBruto - value; }

    [ObservableProperty] private string _formChofer = "";
    [ObservableProperty] private string _formCiChofer = "";
    [ObservableProperty] private string _formPlaca = "";
    [ObservableProperty] private string _formTicket = "";
    [ObservableProperty] private decimal _formAnticipo;
    [ObservableProperty] private decimal _formBonoTransporte;
    [ObservableProperty] private bool _formVisible = true;
    [ObservableProperty] private string _formObservaciones = "";
    [ObservableProperty] private string _mensajeError = "";
    [ObservableProperty] private bool _tieneError;

    // ══════════════════════════════════════════
    // CARGAR
    // ══════════════════════════════════════════

    public async Task CargarCatalogosAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        var coops = await db.Cooperativas.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
        Cooperativas.Clear(); foreach (var c in coops) Cooperativas.Add(c);
        var minas = await db.Minas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();
        Minas.Clear(); foreach (var m in minas) Minas.Add(m);
    }

    public async Task CargarLoteParaEditarAsync(int loteId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        var lote = await db.Lotes.Include(l => l.Proveedor).Include(l => l.Pago).Include(l => l.BonoTransporte)
            .FirstOrDefaultAsync(l => l.Id == loteId);
        if (lote == null) return;

        _loteEditandoId = lote.Id;
        _proveedorSeleccionadoId = lote.ProveedorId;
        TituloFormulario = $"Editar Lote #{lote.NumeroLote}";
        FormFechaIngreso = lote.FechaRegistro;
        FormCiNit = lote.Proveedor.CiNit;
        FormNombreProveedor = lote.Proveedor.NombreCompleto;
        FormCooperativa = Cooperativas.FirstOrDefault(c => c.Id == lote.Proveedor.CooperativaId);
        FormMina = Minas.FirstOrDefault(m => m.Id == lote.MinaId);
        FormTipoMineral = lote.TipoMineral?.ToString()?.ToUpper();
        FormPesoBruto = lote.PesoBruto; FormTara = lote.Tara;
        FormChofer = lote.NombreChofer ?? ""; FormCiChofer = lote.CiChofer ?? "";
        FormPlaca = lote.Placa ?? ""; FormTicket = lote.Ticket ?? "";
        FormObservaciones = lote.Observaciones ?? ""; FormVisible = lote.Visible;
        FormAnticipo = lote.Pago?.Anticipo ?? 0;
        FormBonoTransporte = lote.BonoTransporte?.Monto ?? 0;
    }

    // ══════════════════════════════════════════
    // PROVEEDORES: DROPDOWN + AUTOCOMPLETADO
    // ══════════════════════════════════════════

    /// <summary>Muestra todos los proveedores al hacer clic en el campo CI/NIT.</summary>
    [RelayCommand]
    private async Task MostrarTodosProveedoresAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var todos = await db.Proveedores.Include(p => p.Cooperativa)
                .Where(p => p.Activo)
                .OrderBy(p => p.NombreCompleto)
                .Take(20)
                .Select(p => new ProveedorSugerencia
                {
                    Id = p.Id, NombreCompleto = p.NombreCompleto, CiNit = p.CiNit,
                    CooperativaNombre = p.Cooperativa != null ? p.Cooperativa.Nombre : "",
                    CooperativaId = p.CooperativaId
                }).ToListAsync();

            SugerenciasProveedor.Clear();
            foreach (var r in todos) SugerenciasProveedor.Add(r);
            MostrarSugerencias = SugerenciasProveedor.Count > 0;
        }
        catch { }
    }

    /// <summary>Filtra proveedores mientras el usuario escribe.</summary>
    [RelayCommand]
    private async Task BuscarProveedorAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCiNit) || FormCiNit.Length < 1)
        { SugerenciasProveedor.Clear(); MostrarSugerencias = false; return; }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var txt = FormCiNit.ToUpper();

            var res = await db.Proveedores.Include(p => p.Cooperativa)
                .Where(p => p.Activo && (p.CiNit.ToUpper().Contains(txt) || p.NombreCompleto.ToUpper().Contains(txt)))
                .Take(10)
                .Select(p => new ProveedorSugerencia
                {
                    Id = p.Id, NombreCompleto = p.NombreCompleto, CiNit = p.CiNit,
                    CooperativaNombre = p.Cooperativa != null ? p.Cooperativa.Nombre : "",
                    CooperativaId = p.CooperativaId
                }).ToListAsync();

            SugerenciasProveedor.Clear();
            foreach (var r in res) SugerenciasProveedor.Add(r);
            MostrarSugerencias = SugerenciasProveedor.Count > 0;
        }
        catch { }
    }

    private void SeleccionarProveedor(ProveedorSugerencia p)
    {
        _proveedorSeleccionadoId = p.Id; FormCiNit = p.CiNit; FormNombreProveedor = p.NombreCompleto;
        MostrarSugerencias = false;
        if (p.CooperativaId.HasValue) FormCooperativa = Cooperativas.FirstOrDefault(c => c.Id == p.CooperativaId.Value);
    }

    // ══════════════════════════════════════════
    // GUARDAR
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GuardarAsync()
    {
        TieneError = false;
        if (string.IsNullOrWhiteSpace(FormCiNit) || string.IsNullOrWhiteSpace(FormNombreProveedor))
        { MostrarErr("Ingrese el CI/NIT y nombre del proveedor."); return; }
        if (FormMina == null) { MostrarErr("Seleccione una mina o paraje."); return; }
        if (string.IsNullOrWhiteSpace(FormTipoMineral)) { MostrarErr("Seleccione el tipo de mineral."); return; }
        if (FormPesoNeto <= 0) { MostrarErr("El peso neto debe ser mayor a 0."); return; }
        if (FormAnticipo <= 0) { MostrarErr("El anticipo es obligatorio."); return; }
        if (FormBonoTransporte <= 0) { MostrarErr("El bono de transporte es obligatorio."); return; }

        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            int provId;
            if (_proveedorSeleccionadoId.HasValue) { provId = _proveedorSeleccionadoId.Value; }
            else
            {
                var n = new Proveedor { NombreCompleto = FormNombreProveedor.Trim().ToUpper(), CiNit = FormCiNit.Trim(), CooperativaId = FormCooperativa?.Id, Activo = true };
                db.Proveedores.Add(n); await db.SaveChangesAsync(); provId = n.Id;
            }

            var tipo = FormTipoMineral == "COMPLEJO" ? TipoMineral.Complejo : TipoMineral.Brosa;

            if (_loteEditandoId.HasValue)
            {
                var lote = await db.Lotes.Include(l => l.Pago).Include(l => l.BonoTransporte)
                    .FirstOrDefaultAsync(l => l.Id == _loteEditandoId.Value);
                if (lote != null)
                {
                    lote.FechaRegistro = FormFechaIngreso; lote.ProveedorId = provId; lote.MinaId = FormMina.Id;
                    lote.TipoMineral = tipo; lote.PesoBruto = FormPesoBruto; lote.Tara = FormTara; lote.PesoNeto = FormPesoNeto;
                    lote.NombreChofer = FormChofer.Trim().ToUpper(); lote.CiChofer = FormCiChofer.Trim();
                    lote.Placa = FormPlaca.Trim().ToUpper(); lote.Ticket = FormTicket.Trim();
                    lote.Observaciones = FormObservaciones.Trim(); lote.Visible = FormVisible;

                    if (lote.Pago != null) { lote.Pago.Anticipo = FormAnticipo; lote.Pago.FechaAnticipo = FormFechaIngreso; }
                    else { db.Pagos.Add(new Pago { LoteId = lote.Id, Anticipo = FormAnticipo, FechaAnticipo = FormFechaIngreso }); }

                    if (lote.BonoTransporte != null) { lote.BonoTransporte.Monto = FormBonoTransporte; }
                    else { db.BonosTransporte.Add(new BonoTransporte { LoteId = lote.Id, Monto = FormBonoTransporte, Fecha = FormFechaIngreso }); }
                }
            }
            else
            {
                var ult = await db.Lotes.MaxAsync(l => (int?)l.NumeroLote) ?? 0;
                var lote = new Lote
                {
                    NumeroLote = ult + 1, FechaRegistro = FormFechaIngreso, ProveedorId = provId, MinaId = FormMina.Id,
                    TipoMineral = tipo, PesoBruto = FormPesoBruto, Tara = FormTara, PesoNeto = FormPesoNeto,
                    NombreChofer = FormChofer.Trim().ToUpper(), CiChofer = FormCiChofer.Trim(),
                    Placa = FormPlaca.Trim().ToUpper(), Ticket = FormTicket.Trim(),
                    Observaciones = FormObservaciones.Trim(), Estado = EstadoLote.Registrado, Visible = FormVisible
                };
                db.Lotes.Add(lote); await db.SaveChangesAsync();

                db.Pagos.Add(new Pago { LoteId = lote.Id, Anticipo = FormAnticipo, FechaAnticipo = FormFechaIngreso });
                db.BonosTransporte.Add(new BonoTransporte { LoteId = lote.Id, Monto = FormBonoTransporte, Fecha = FormFechaIngreso });
            }

            await db.SaveChangesAsync();
            OnGuardado?.Invoke();
        }
        catch (Exception ex) { MostrarErr($"Error: {ex.InnerException?.Message ?? ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand] private void Cancelar() => OnCancelado?.Invoke();

    // ══════════════════════════════════════════
    // CATÁLOGOS
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task AgregarCooperativaAsync()
    {
        var d = new Views.AgregarItemDialog("Agregar Cooperativa") { Owner = Application.Current.MainWindow };
        if (d.ShowDialog() == true)
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            if (await db.Cooperativas.AnyAsync(c => c.Nombre == d.NombreIngresado)) return;
            var n = new Cooperativa { Nombre = d.NombreIngresado, Activo = true };
            db.Cooperativas.Add(n); await db.SaveChangesAsync();
            Cooperativas.Add(n); FormCooperativa = n;
        }
    }

    [RelayCommand]
    private async Task AgregarMinaAsync()
    {
        var d = new Views.AgregarItemDialog("Agregar Mina / Paraje") { Owner = Application.Current.MainWindow };
        if (d.ShowDialog() == true)
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            if (await db.Minas.AnyAsync(m => m.Nombre == d.NombreIngresado)) return;
            var n = new Mina { Nombre = d.NombreIngresado, Activo = true };
            db.Minas.Add(n); await db.SaveChangesAsync();
            Minas.Add(n); FormMina = n;
        }
    }

    [RelayCommand] private void AgregarProveedor() { _proveedorSeleccionadoId = null; FormNombreProveedor = ""; }

    private void MostrarErr(string m) { MensajeError = m; TieneError = true; }
}
