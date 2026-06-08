// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ConcentradoFormViewModel.cs
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
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class ConcentradoFormViewModel : BaseViewModel
{
    private int? _editandoId;
    public Action? OnGuardado { get; set; }
    public Action? OnCancelado { get; set; }

    public ConcentradoFormViewModel(TipoConcentrado tipo)
    {
        Tipo = tipo;
        TipoTexto = tipo == TipoConcentrado.ZnAg ? "ZN-AG" : "AG-PB";
        EsZnAg = tipo == TipoConcentrado.ZnAg;
        TituloFormulario = $"Nuevo Concentrado {TipoTexto}";
        CargarDesdeInput(tipo == TipoConcentrado.ZnAg ? ConcentradoCalculator.DefaultsZnAg() : ConcentradoCalculator.DefaultsAg());
        LimpiarDatosLote();   // los parámetros técnicos quedan; los datos del lote arrancan vacíos
        Recalcular();
    }

    public TipoConcentrado Tipo { get; }

    [ObservableProperty] private string _tituloFormulario = "";
    [ObservableProperty] private string _tipoTexto = "";
    [ObservableProperty] private bool _esZnAg;
    public bool EsAg => !EsZnAg;

    // Asistente: 1=Cliente, 2=Análisis, 3=Cotizaciones/Deducciones, 4=Resultado
    [ObservableProperty] private int _paso = 1;

    public bool MuestraAtras => Paso >= 2;
    public bool MuestraSiguiente => Paso == 1 || Paso == 2;

    partial void OnPasoChanged(int value)
    {
        OnPropertyChanged(nameof(MuestraAtras));
        OnPropertyChanged(nameof(MuestraSiguiente));
    }

    // ── Cabecera ──
    [ObservableProperty] private string _numeroLiquidacion = "";
    [ObservableProperty] private string _clienteNombre = "";
    [ObservableProperty] private string _clienteCi = "";
    [ObservableProperty] private string _procedencia = "PARTICULAR";
    [ObservableProperty] private string _municipio = "POTOSI";
    [ObservableProperty] private string _concesionMinera = "";
    [ObservableProperty] private DateTime _fechaEntrega = DateTime.Today;
    [ObservableProperty] private DateTime _fechaLiquidacion = DateTime.Today;

    // ── Leyes / análisis ──
    [ObservableProperty] private decimal _leyZn;
    [ObservableProperty] private decimal _leyAg;
    [ObservableProperty] private decimal _leyPb;
    [ObservableProperty] private decimal _fe;
    [ObservableProperty] private decimal _as;
    [ObservableProperty] private decimal _sb;
    [ObservableProperty] private decimal _sn;
    [ObservableProperty] private decimal _bi;
    [ObservableProperty] private decimal _siO2;
    [ObservableProperty] private decimal _znImpureza;

    // ── Pesos ──
    [ObservableProperty] private decimal _tmh;
    [ObservableProperty] private decimal _porcentajeHumedad;
    [ObservableProperty] private decimal _porcentajeMerma;

    // ── Cotizaciones / T/C ──
    [ObservableProperty] private decimal _cotizZnLb;
    [ObservableProperty] private decimal _cotizAgOz;
    [ObservableProperty] private decimal _cotizPbLb;
    [ObservableProperty] private decimal _tcOficial;
    [ObservableProperty] private decimal _tcComercial;
    [ObservableProperty] private decimal _cotizRegaliaZn;
    [ObservableProperty] private decimal _cotizRegaliaAg;
    [ObservableProperty] private decimal _cotizRegaliaPb;

    // ── Alícuotas / factores ──
    [ObservableProperty] private decimal _alicuotaZn;
    [ObservableProperty] private decimal _alicuotaAg;
    [ObservableProperty] private decimal _alicuotaPb;
    [ObservableProperty] private decimal _fcLb;
    [ObservableProperty] private decimal _fcOz;

    // ── Pagables ──
    [ObservableProperty] private decimal _znLibre;
    [ObservableProperty] private decimal _znFactor;
    [ObservableProperty] private decimal _agLibreOz;
    [ObservableProperty] private decimal _agFactor;
    [ObservableProperty] private decimal _pbLibre;
    [ObservableProperty] private decimal _pbFactor;

    // ── Maquila / refinación / otros ──
    [ObservableProperty] private decimal _maquilaBase;
    [ObservableProperty] private decimal _maquilaFijo;
    [ObservableProperty] private decimal _maquilaEscalador;
    [ObservableProperty] private decimal _refinacionAgPorOz;
    [ObservableProperty] private decimal _otrosAjusteUs;

    // ── Fletes ──
    [ObservableProperty] private decimal _rollback;
    [ObservableProperty] private decimal _transporteTerrestre;
    [ObservableProperty] private decimal _fletePotosiArica;
    [ObservableProperty] private decimal _ahk;
    [ObservableProperty] private decimal _molienda;
    [ObservableProperty] private decimal _comisionBancariaTasa;
    [ObservableProperty] private bool _aplicaComisionBancaria;
    [ObservableProperty] private bool _aplicaAhk;
    [ObservableProperty] private bool _aplicaRollback = true;
    [ObservableProperty] private bool _aplicaTransporte = true;
    [ObservableProperty] private bool _aplicaMolienda = true;

    // ── Retenciones (toggles + tasas) ──
    [ObservableProperty] private bool _aplicaRegaliaZn;
    [ObservableProperty] private bool _aplicaRegaliaAg;
    [ObservableProperty] private bool _aplicaRegaliaPb;
    [ObservableProperty] private bool _aplicaComibol;     [ObservableProperty] private decimal _tasaComibol;
    [ObservableProperty] private bool _aplicaCns;         [ObservableProperty] private decimal _tasaCns;
    [ObservableProperty] private bool _aplicaFedecomin;   [ObservableProperty] private decimal _tasaFedecomin;
    [ObservableProperty] private bool _aplicaFencomin;    [ObservableProperty] private decimal _tasaFencomin;
    [ObservableProperty] private bool _aplicaWilstermann; [ObservableProperty] private decimal _tasaWilstermann;
    [ObservableProperty] private bool _aplicaAporteCoop;  [ObservableProperty] private decimal _tasaAporteCoop;
    [ObservableProperty] private bool _aplicaM02;         [ObservableProperty] private decimal _tasaM02;

    [ObservableProperty] private decimal _anticipo;
    [ObservableProperty] private string _observaciones = "";

    public ObservableCollection<PenalidadItem> Penalidades { get; } = new();

    // ── Resultados (solo lectura para mostrar) ──
    [ObservableProperty] private decimal _resTms;
    [ObservableProperty] private decimal _resPagableZn;
    [ObservableProperty] private decimal _resPagableAg;
    [ObservableProperty] private decimal _resPagablePb;
    [ObservableProperty] private decimal _resTotalPagable;
    [ObservableProperty] private decimal _resMaquila;
    [ObservableProperty] private decimal _resRefinacion;
    [ObservableProperty] private decimal _resPenalidades;
    [ObservableProperty] private decimal _resPrecioPorTms;
    [ObservableProperty] private decimal _resValorFobUs;
    [ObservableProperty] private decimal _resValorFobBs;
    [ObservableProperty] private decimal _resRegaliaZn;
    [ObservableProperty] private decimal _resRegaliaAg;
    [ObservableProperty] private decimal _resRegaliaPb;
    [ObservableProperty] private decimal _resComibol;
    [ObservableProperty] private decimal _resCns;
    [ObservableProperty] private decimal _resFedecomin;
    [ObservableProperty] private decimal _resFencomin;
    [ObservableProperty] private decimal _resWilstermann;
    [ObservableProperty] private decimal _resAporteCoop;
    [ObservableProperty] private decimal _resM02;
    [ObservableProperty] private decimal _resTotalRetenciones;
    [ObservableProperty] private decimal _resLiquidoPagableBs;
    [ObservableProperty] private decimal _resLiquidoPagableUs;
    [ObservableProperty] private decimal _resSaldoPagarBs;
    [ObservableProperty] private decimal _resSaldoPagarUs;

    [ObservableProperty] private string _mensajeError = "";
    [ObservableProperty] private bool _tieneError;

    // ══════════════════════════════════════════
    // Construye el input desde los campos
    // ══════════════════════════════════════════
    private ConcentradoInput ConstruirInput()
    {
        var i = new ConcentradoInput
        {
            Tipo = Tipo,
            Tmh = Tmh, PorcentajeHumedad = PorcentajeHumedad, PorcentajeMerma = PorcentajeMerma,
            LeyZn = LeyZn, LeyAg = LeyAg, LeyPb = LeyPb,
            Fe = Fe, As = As, Sb = Sb, Sn = Sn, Bi = Bi, SiO2 = SiO2,
            CotizZnLb = CotizZnLb, CotizAgOz = CotizAgOz, CotizPbLb = CotizPbLb,
            CotizRegaliaZn = CotizRegaliaZn, CotizRegaliaAg = CotizRegaliaAg, CotizRegaliaPb = CotizRegaliaPb,
            TcOficial = TcOficial, TcComercial = TcComercial,
            AlicuotaZn = AlicuotaZn, AlicuotaAg = AlicuotaAg, AlicuotaPb = AlicuotaPb,
            FcLb = FcLb, FcOz = FcOz,
            ZnLibre = ZnLibre, ZnFactor = ZnFactor, AgLibreOz = AgLibreOz, AgFactor = AgFactor,
            PbLibre = PbLibre, PbFactor = PbFactor,
            MaquilaBase = MaquilaBase, MaquilaFijo = MaquilaFijo, MaquilaEscalador = MaquilaEscalador,
            RefinacionAgPorOz = RefinacionAgPorOz, OtrosAjusteUs = OtrosAjusteUs,
            Rollback = AplicaRollback ? Rollback : 0m, TransporteTerrestre = AplicaTransporte ? TransporteTerrestre : 0m,
            FletePotosiArica = FletePotosiArica, Ahk = Ahk, Molienda = AplicaMolienda ? Molienda : 0m,
            ComisionBancariaTasa = ComisionBancariaTasa, AplicaComisionBancaria = AplicaComisionBancaria,
            AplicaAhk = AplicaAhk,
            AplicaRegaliaZn = AplicaRegaliaZn, AplicaRegaliaAg = AplicaRegaliaAg, AplicaRegaliaPb = AplicaRegaliaPb,
            AplicaComibol = AplicaComibol, TasaComibol = TasaComibol,
            AplicaCns = AplicaCns, TasaCns = TasaCns,
            AplicaFedecomin = AplicaFedecomin, TasaFedecomin = TasaFedecomin,
            AplicaFencomin = AplicaFencomin, TasaFencomin = TasaFencomin,
            AplicaWilstermann = AplicaWilstermann, TasaWilstermann = TasaWilstermann,
            AplicaAporteCoop = AplicaAporteCoop, TasaAporteCoop = TasaAporteCoop,
            AplicaM02 = AplicaM02, TasaM02 = TasaM02,
            Anticipo = Anticipo,
            Penalidades = Penalidades.Select(p => new PenalidadItem
            { Nombre = p.Nombre, Libre = p.Libre, Tarifa = p.Tarifa }).ToList()
        };
        AsignarActualPenalidades(i);
        return i;
    }

    // El "actual" de cada penalidad sale del análisis, según el tipo.
    private void AsignarActualPenalidades(ConcentradoInput i)
    {
        foreach (var p in i.Penalidades)
        {
            p.Actual = p.Nombre switch
            {
                "Fe" => Fe, "As" => As, "Sb" => Sb, "Sn" => Sn,
                "Bi" => Bi, "SiO2" => SiO2, "Zn" => ZnImpureza,
                "As+Sb" => As + Sb,
                _ => 0m
            };
        }
    }

    private void CargarDesdeInput(ConcentradoInput i)
    {
        Tmh = i.Tmh; PorcentajeHumedad = i.PorcentajeHumedad; PorcentajeMerma = i.PorcentajeMerma;
        LeyZn = i.LeyZn; LeyAg = i.LeyAg; LeyPb = i.LeyPb;
        Fe = i.Fe; As = i.As; Sb = i.Sb; Sn = i.Sn; Bi = i.Bi; SiO2 = i.SiO2;
        CotizZnLb = i.CotizZnLb; CotizAgOz = i.CotizAgOz; CotizPbLb = i.CotizPbLb;
        CotizRegaliaZn = i.CotizRegaliaZn; CotizRegaliaAg = i.CotizRegaliaAg; CotizRegaliaPb = i.CotizRegaliaPb;
        TcOficial = i.TcOficial; TcComercial = i.TcComercial;
        AlicuotaZn = i.AlicuotaZn; AlicuotaAg = i.AlicuotaAg; AlicuotaPb = i.AlicuotaPb;
        FcLb = i.FcLb; FcOz = i.FcOz;
        ZnLibre = i.ZnLibre; ZnFactor = i.ZnFactor; AgLibreOz = i.AgLibreOz; AgFactor = i.AgFactor;
        PbLibre = i.PbLibre; PbFactor = i.PbFactor;
        MaquilaBase = i.MaquilaBase; MaquilaFijo = i.MaquilaFijo; MaquilaEscalador = i.MaquilaEscalador;
        RefinacionAgPorOz = i.RefinacionAgPorOz; OtrosAjusteUs = i.OtrosAjusteUs;
        Rollback = i.Rollback; TransporteTerrestre = i.TransporteTerrestre;
        FletePotosiArica = i.FletePotosiArica; Ahk = i.Ahk; Molienda = i.Molienda;
        ComisionBancariaTasa = i.ComisionBancariaTasa; AplicaComisionBancaria = i.AplicaComisionBancaria;
        AplicaAhk = i.AplicaAhk;
        AplicaRegaliaZn = i.AplicaRegaliaZn; AplicaRegaliaAg = i.AplicaRegaliaAg; AplicaRegaliaPb = i.AplicaRegaliaPb;
        AplicaComibol = i.AplicaComibol; TasaComibol = i.TasaComibol;
        AplicaCns = i.AplicaCns; TasaCns = i.TasaCns;
        AplicaFedecomin = i.AplicaFedecomin; TasaFedecomin = i.TasaFedecomin;
        AplicaFencomin = i.AplicaFencomin; TasaFencomin = i.TasaFencomin;
        AplicaWilstermann = i.AplicaWilstermann; TasaWilstermann = i.TasaWilstermann;
        AplicaAporteCoop = i.AplicaAporteCoop; TasaAporteCoop = i.TasaAporteCoop;
        AplicaM02 = i.AplicaM02; TasaM02 = i.TasaM02;
        Anticipo = i.Anticipo;
        Penalidades.Clear();
        foreach (var p in i.Penalidades)
            Penalidades.Add(new PenalidadItem { Nombre = p.Nombre, Libre = p.Libre, Tarifa = p.Tarifa });
    }

    /// <summary>Vacía solo los datos propios del lote; conserva los parámetros técnicos.</summary>
    private void LimpiarDatosLote()
    {
        Tmh = 0; PorcentajeHumedad = 0;
        LeyZn = 0; LeyAg = 0; LeyPb = 0;
        Fe = 0; As = 0; Sb = 0; Sn = 0; Bi = 0; SiO2 = 0; ZnImpureza = 0;
        Anticipo = 0;
    }

    /// <summary>Genera el N° de liquidación automático, correlativo independiente por tipo.</summary>
    public async Task InicializarNuevoAsync()
    {
        var prefijo = Tipo == TipoConcentrado.ZnAg ? "CZN-TMT-" : "CPB-TMT-";
        int max = 0;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var nums = await db.Concentrados.Where(c => c.Tipo == Tipo)
                .Select(c => c.NumeroLiquidacion).ToListAsync();
            foreach (var n in nums)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                var digitos = new string(n.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
                if (int.TryParse(digitos, out var v) && v > max) max = v;
            }
        }
        catch { /* sin datos previos */ }
        NumeroLiquidacion = $"{prefijo}{max + 1:000}";
    }

    [RelayCommand]
    private void Calcular() => Recalcular();

    // ── Navegación del asistente ──
    [RelayCommand]
    private void Siguiente()
    {
        if (Paso == 1 && string.IsNullOrWhiteSpace(ClienteNombre))
        { MostrarErr("Ingresá el nombre del cliente."); return; }
        if (Paso == 2)
        {
            if (Tmh <= 0) { MostrarErr("Ingresá la TMH (peso húmedo)."); return; }
            if (EsZnAg && (LeyZn <= 0 || LeyAg <= 0)) { MostrarErr("Ingresá las leyes ZN y AG."); return; }
            if (EsAg && (LeyAg <= 0 || LeyPb <= 0)) { MostrarErr("Ingresá las leyes AG y PB."); return; }
        }
        MensajeError = ""; TieneError = false;
        if (Paso < 3) Paso++;
        else { Recalcular(); Paso = 4; }
    }

    [RelayCommand]
    private void Atras()
    {
        MensajeError = ""; TieneError = false;
        if (Paso == 4) Paso = 3;
        else if (Paso > 1) Paso--;
    }

    [RelayCommand]
    private void EditarDatos() { MensajeError = ""; TieneError = false; Paso = 1; }

    /// <summary>Rellena el formulario con el ejemplo del Excel para comparar resultados.</summary>
    [RelayCommand]
    private void CargarMuestra()
    {
        CargarDesdeInput(Tipo == TipoConcentrado.ZnAg
            ? ConcentradoCalculator.DefaultsZnAg()
            : ConcentradoCalculator.DefaultsAg());
        ZnImpureza = 0m;   // el escenario validado usa Zn impureza en 0
        Recalcular();
    }

    private void Recalcular()
    {
        try
        {
            var input = ConstruirInput();
            var x = ConcentradoCalculator.Calcular(input);
            ResTms = x.Tms;
            ResPagableZn = x.PagableZn; ResPagableAg = x.PagableAg; ResPagablePb = x.PagablePb;
            ResTotalPagable = x.TotalPagable; ResMaquila = x.Maquila; ResRefinacion = x.Refinacion;
            ResPenalidades = x.TotalPenalidades; ResPrecioPorTms = x.PrecioPorTms;
            ResValorFobUs = x.ValorFobUs; ResValorFobBs = x.ValorFobBs;
            ResRegaliaZn = x.RegaliaZn; ResRegaliaAg = x.RegaliaAg; ResRegaliaPb = x.RegaliaPb;
            ResComibol = x.Comibol; ResCns = x.Cns; ResFedecomin = x.Fedecomin; ResFencomin = x.Fencomin;
            ResWilstermann = x.Wilstermann; ResAporteCoop = x.AporteCoop; ResM02 = x.M02;
            ResTotalRetenciones = x.TotalRetenciones;
            ResLiquidoPagableBs = x.LiquidoPagableBs; ResLiquidoPagableUs = x.LiquidoPagableUs;
            ResSaldoPagarBs = x.SaldoPagarBs; ResSaldoPagarUs = x.SaldoPagarUs;
            TieneError = false;
        }
        catch (Exception ex) { MensajeError = $"Error de cálculo: {ex.Message}"; TieneError = true; }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(ClienteNombre)) { MostrarErr("Ingrese el nombre del cliente."); return; }
        if (Tmh <= 0) { MostrarErr("El peso (TMH) debe ser mayor a 0."); return; }
        Recalcular();

        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var input = ConstruirInput();
            var json = JsonSerializer.Serialize(input);

            Concentrado c;
            if (_editandoId.HasValue)
            {
                c = await db.Concentrados.FirstAsync(x => x.Id == _editandoId.Value);
            }
            else
            {
                c = new Concentrado
                {
                    NumeroConcentrado = (await db.Concentrados.MaxAsync(x => (int?)x.NumeroConcentrado) ?? 0) + 1,
                    FechaRegistro = DateTime.Now
                };
                db.Concentrados.Add(c);
            }

            c.Tipo = Tipo;
            c.NumeroLiquidacion = NumeroLiquidacion;
            c.ClienteNombre = ClienteNombre.Trim().ToUpper();
            c.ClienteCi = ClienteCi; c.Procedencia = Procedencia; c.Municipio = Municipio;
            c.ConcesionMinera = ConcesionMinera; c.MineralTexto = TipoTexto;
            c.FechaEntrega = FechaEntrega; c.FechaLiquidacion = FechaLiquidacion;
            c.Tmh = Tmh; c.Tms = ResTms; c.LeyZn = LeyZn; c.LeyAg = LeyAg; c.LeyPb = LeyPb;
            c.ValorFobBs = ResValorFobBs; c.TotalRetenciones = ResTotalRetenciones;
            c.Anticipo = Anticipo; c.LiquidoPagableBs = ResLiquidoPagableBs;
            c.SaldoPagarBs = ResSaldoPagarBs; c.SaldoPagarUs = ResSaldoPagarUs;
            c.ParametrosJson = json; c.Observaciones = Observaciones;

            await db.SaveChangesAsync();
            MessageBox.Show($"Concentrado N° {c.NumeroConcentrado} guardado.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OnGuardado?.Invoke();
        }
        catch (Exception ex) { MostrarErr($"Error al guardar: {ex.InnerException?.Message ?? ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task GenerarReciboAsync()
    {
        Recalcular();
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
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
                TipoConcentrado = TipoTexto,
                NumeroLiquidacion = string.IsNullOrWhiteSpace(NumeroLiquidacion) ? "—" : NumeroLiquidacion,
                ClienteNombre = ClienteNombre, ClienteCi = ClienteCi ?? "",
                Procedencia = Procedencia, FechaEntrega = FechaEntrega, FechaLiquidacion = FechaLiquidacion,
                PesoBruto = Tmh, PesoNeto = ResTms,
                LeyZn = LeyZn, LeyAg = LeyAg, LeyPb = LeyPb,
                LiquidoPagableBs = ResLiquidoPagableBs, Anticipo = Anticipo,
                SaldoPagarBs = ResSaldoPagarBs,
                RegaliaMinera = ResRegaliaZn + ResRegaliaAg + ResRegaliaPb,
                Cns = ResCns, Comibol = ResComibol, Fedecomin = ResFedecomin,
                Fencomin = ResFencomin, Wilstermann = ResWilstermann, AporteCoop = ResAporteCoop,
                TotalRetenciones = ResTotalRetenciones
            };

            var sfd = new SaveFileDialog
            {
                FileName = $"ReciboConcentrado_{TipoTexto}_{(string.IsNullOrWhiteSpace(NumeroLiquidacion) ? "SN" : NumeroLiquidacion)}.pdf",
                Filter = "PDF (*.pdf)|*.pdf", Title = "Guardar Recibo de Concentrado"
            };
            if (sfd.ShowDialog() != true) return;
            ConcentradoReciboPdfGenerator.Generar(data, sfd.FileName);
            Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        { MessageBox.Show($"Error al generar recibo: {ex.Message}", "SILF", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    public async Task CargarParaEditarAsync(int id)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
        var c = await db.Concentrados.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return;
        _editandoId = c.Id;
        TituloFormulario = $"Editar Concentrado N° {c.NumeroConcentrado}";
        NumeroLiquidacion = c.NumeroLiquidacion ?? "";
        ClienteNombre = c.ClienteNombre; ClienteCi = c.ClienteCi ?? "";
        Procedencia = c.Procedencia ?? ""; Municipio = c.Municipio ?? "";
        ConcesionMinera = c.ConcesionMinera ?? "";
        FechaEntrega = c.FechaEntrega; FechaLiquidacion = c.FechaLiquidacion;
        Observaciones = c.Observaciones ?? "";
        if (!string.IsNullOrWhiteSpace(c.ParametrosJson))
        {
            var input = JsonSerializer.Deserialize<ConcentradoInput>(c.ParametrosJson);
            if (input != null) CargarDesdeInput(input);
        }
        Recalcular();
        Paso = 4;   // al editar, mostrar directamente el resultado
    }

    [RelayCommand] private void Cancelar() => OnCancelado?.Invoke();
    private void MostrarErr(string m) { MensajeError = m; TieneError = true; }
}
