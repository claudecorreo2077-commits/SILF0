// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LiquidacionViewModel.cs
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

public partial class LiquidacionViewModel : BaseViewModel
{
    public Action? OnGuardado { get; set; }
    public Action? OnCancelado { get; set; }

    [ObservableProperty] private string _tituloFormulario = "Nueva Liquidación";

    // Info lote
    [ObservableProperty] private string _infoProveedor = "";
    [ObservableProperty] private string _infoCiNit = "";
    [ObservableProperty] private string _infoMina = "";
    [ObservableProperty] private string _infoCooperativa = "";
    [ObservableProperty] private int _infoNumeroLote;
    [ObservableProperty] private string _infoTipoMineral = "";
    [ObservableProperty] private DateTime _infoFechaIngreso;
    [ObservableProperty] private DateTime _fechaLiquidacion = DateTime.Today;
    private int _loteId;

    [ObservableProperty] private decimal _infoPesoNeto;
    partial void OnInfoPesoNetoChanged(decimal value) => Recalcular();

    [ObservableProperty] private decimal _leyZn;
    partial void OnLeyZnChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _leyAg;
    partial void OnLeyAgChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _leyPb;
    partial void OnLeyPbChanged(decimal value) => Recalcular();

    [ObservableProperty] private decimal _humedad;
    partial void OnHumedadChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _cotizacionZn;
    partial void OnCotizacionZnChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _cotizacionAg;
    partial void OnCotizacionAgChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _cotizacionPb;
    partial void OnCotizacionPbChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _tipoCambio = 6.97m;
    partial void OnTipoCambioChanged(decimal value) => Recalcular();
    [ObservableProperty] private decimal _anticipoMonto;

    [ObservableProperty] private bool _aplicaCooperativa;
    partial void OnAplicaCooperativaChanged(bool value) { if (!value) PorcentajeCooperativa = 0; Recalcular(); }
    [ObservableProperty] private decimal _porcentajeCooperativa;
    partial void OnPorcentajeCooperativaChanged(decimal value) => Recalcular();

    [ObservableProperty] private bool _aplicaIue = true;
    partial void OnAplicaIueChanged(bool value) => Recalcular();

    [ObservableProperty] private decimal _costoLaboratorio;
    [ObservableProperty] private string _observaciones = "";

    private const decimal PctRegalias = 0.06m;
    private const decimal PctCns = 0.018m;
    private const decimal PctComibol = 0.01m;
    private const decimal PctFencomin = 0.004m;
    private const decimal PctFedecomin = 0.01m;
    private const decimal PctIue = 0.05m;

    [ObservableProperty] private decimal _pesoHumedad;
    [ObservableProperty] private decimal _pesoNetoSeco;
    [ObservableProperty] private decimal _valorBrutoZn;
    [ObservableProperty] private decimal _valorBrutoAg;
    [ObservableProperty] private decimal _valorBrutoPb;
    [ObservableProperty] private decimal _valorComercialUs;
    [ObservableProperty] private decimal _valorComercialBs;
    [ObservableProperty] private decimal _regalias;
    [ObservableProperty] private decimal _cns;
    [ObservableProperty] private decimal _comibol;
    [ObservableProperty] private decimal _totalDeduccionesLegales;
    [ObservableProperty] private decimal _fencomin;
    [ObservableProperty] private decimal _fedecomin;
    [ObservableProperty] private decimal _montoCooperativa;
    [ObservableProperty] private decimal _iue;
    [ObservableProperty] private decimal _totalOtrasDeducciones;
    [ObservableProperty] private decimal _totalDeducciones;
    [ObservableProperty] private decimal _liquidoPagable;
    [ObservableProperty] private decimal _liquidoPagableUs;
    [ObservableProperty] private decimal _saldoPagar;
    [ObservableProperty] private string _montoLiteral = "";
    [ObservableProperty] private string _mensajeError = "";
    [ObservableProperty] private bool _tieneError;

    // Datos empresa para PDF
    private string _empresaNombre = "";
    private string _empresaLiquidador = "";
    private byte[]? _empresaLogo;
    private decimal _bonoTransporte;

    public async Task CargarLoteAsync(int loteId)
    {
        _loteId = loteId;
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

        var empresa = await db.Empresas.FirstOrDefaultAsync();
        if (empresa != null)
        {
            TipoCambio = empresa.TipoCambio;
            _empresaNombre = empresa.RazonSocial ?? "";
            _empresaLiquidador = empresa.NombreLiquidador ?? "";
            if (!string.IsNullOrEmpty(empresa.LogoPath) && File.Exists(empresa.LogoPath))
                _empresaLogo = File.ReadAllBytes(empresa.LogoPath);
        }

        var lote = await db.Lotes.Include(l => l.Proveedor).ThenInclude(p => p.Cooperativa)
            .Include(l => l.Mina).Include(l => l.Liquidacion).Include(l => l.Pago)
            .Include(l => l.BonoTransporte)
            .FirstOrDefaultAsync(l => l.Id == loteId);
        if (lote == null) return;

        InfoProveedor = lote.Proveedor.NombreCompleto;
        InfoCiNit = lote.Proveedor.CiNit;
        InfoMina = lote.Mina.Nombre;
        InfoCooperativa = lote.Proveedor.Cooperativa?.Nombre ?? "—";
        InfoNumeroLote = lote.NumeroLote;
        InfoTipoMineral = lote.TipoMineral?.ToString()?.ToUpper() ?? "";
        InfoPesoNeto = lote.PesoNeto;
        InfoFechaIngreso = lote.FechaRegistro;
        LeyZn = lote.LeyZn ?? 0; LeyAg = lote.LeyAg ?? 0; LeyPb = lote.LeyPb ?? 0;
        AnticipoMonto = lote.Pago?.Anticipo ?? 0;
        _bonoTransporte = lote.BonoTransporte?.Monto ?? 0;

        if (lote.Liquidacion != null)
        {
            TituloFormulario = $"Liquidación Lote #{lote.NumeroLote}";
            var liq = lote.Liquidacion;
            Humedad = liq.Humedad; CotizacionZn = liq.CotizacionZn;
            CotizacionAg = liq.CotizacionAg; CotizacionPb = liq.CotizacionPb;
            PorcentajeCooperativa = liq.PorcentajeCooperativa;
            AplicaCooperativa = liq.PorcentajeCooperativa > 0;
            AplicaIue = liq.IUE > 0;
            CostoLaboratorio = liq.CostoLaboratorio;
            Observaciones = liq.Observaciones ?? "";
            FechaLiquidacion = liq.FechaCalculo ?? DateTime.Today;
        }
        else
        {
            TituloFormulario = $"Liquidar Lote #{lote.NumeroLote}";
            Humedad = 0; CotizacionZn = 0; CotizacionAg = 0; CotizacionPb = 0;
            PorcentajeCooperativa = 0; AplicaCooperativa = false;
            AplicaIue = true; CostoLaboratorio = 0;
            Observaciones = ""; FechaLiquidacion = DateTime.Today;
        }
        Recalcular();
    }

    private void Recalcular()
    {
        PesoHumedad = Math.Round(InfoPesoNeto * Humedad / 100m, 2);
        PesoNetoSeco = Math.Round(InfoPesoNeto - PesoHumedad, 2);
        ValorBrutoZn = Math.Round(PesoNetoSeco * LeyZn * CotizacionZn, 2);
        ValorBrutoAg = Math.Round(PesoNetoSeco * LeyAg * CotizacionAg, 2);
        ValorBrutoPb = Math.Round(PesoNetoSeco * LeyPb * CotizacionPb, 2);
        ValorComercialUs = Math.Round(ValorBrutoZn + ValorBrutoAg + ValorBrutoPb, 2);
        ValorComercialBs = Math.Round(ValorComercialUs * TipoCambio, 2);

        Regalias = Math.Round(ValorComercialBs * PctRegalias, 2);
        Cns = Math.Round(ValorComercialBs * PctCns, 2);
        Comibol = Math.Round(ValorComercialBs * PctComibol, 2);
        TotalDeduccionesLegales = Regalias + Cns + Comibol;

        Fencomin = Math.Round(ValorComercialBs * PctFencomin, 2);
        Fedecomin = Math.Round(ValorComercialBs * PctFedecomin, 2);
        MontoCooperativa = AplicaCooperativa ? Math.Round(ValorComercialBs * PorcentajeCooperativa / 100m, 2) : 0m;
        Iue = AplicaIue ? Math.Round(ValorComercialBs * PctIue, 2) : 0m;
        TotalOtrasDeducciones = Fencomin + Fedecomin + MontoCooperativa + Iue;

        TotalDeducciones = TotalDeduccionesLegales + TotalOtrasDeducciones;
        LiquidoPagable = Math.Round(ValorComercialBs - TotalDeducciones, 2);
        LiquidoPagableUs = TipoCambio > 0 ? Math.Round(LiquidoPagable / TipoCambio, 2) : 0;
        SaldoPagar = Math.Round(LiquidoPagable - AnticipoMonto, 2);
        MontoLiteral = NumeroALiteral(LiquidoPagable);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (InfoPesoNeto <= 0) { MostrarErr("El peso neto debe ser mayor a 0."); return; }
        if (LeyZn <= 0 && LeyAg <= 0 && LeyPb <= 0) { MostrarErr("Ingrese al menos una ley."); return; }
        if (Humedad < 0) { MostrarErr("La humedad no puede ser negativa."); return; }
        if (ValorComercialBs <= 0) { MostrarErr("Valor comercial debe ser mayor a 0."); return; }

        TieneError = false; Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var lote = await db.Lotes.Include(l => l.Liquidacion).FirstOrDefaultAsync(l => l.Id == _loteId);
            if (lote == null) { MostrarErr("Lote no encontrado."); return; }

            lote.PesoNeto = InfoPesoNeto; lote.PesoBruto = InfoPesoNeto + lote.Tara;
            lote.LeyZn = LeyZn; lote.LeyAg = LeyAg; lote.LeyPb = LeyPb;

            var liq = lote.Liquidacion ?? new Liquidacion { LoteId = lote.Id };
            var esNueva = liq.Id == 0;

            liq.Humedad = Humedad; liq.CotizacionZn = CotizacionZn;
            liq.CotizacionAg = CotizacionAg; liq.CotizacionPb = CotizacionPb;
            liq.TipoCambio = TipoCambio;
            liq.PesoHumedad = PesoHumedad; liq.PesoNetoSeco = PesoNetoSeco;
            liq.ValorBrutoZn = ValorBrutoZn; liq.ValorBrutoAg = ValorBrutoAg;
            liq.ValorBrutoPb = ValorBrutoPb; liq.ValorComercialUs = ValorComercialUs;
            liq.ValorComercialBs = ValorComercialBs;
            liq.Regalias = Regalias; liq.CNS = Cns; liq.COMIBOL = Comibol;
            liq.TotalDeduccionesLegales = TotalDeduccionesLegales;
            liq.FENCOMIN = Fencomin; liq.FEDECOMIN = Fedecomin;
            liq.PorcentajeCooperativa = PorcentajeCooperativa;
            liq.MontoCooperativa = MontoCooperativa; liq.Anticipo = AnticipoMonto;
            liq.IUE = Iue; liq.TotalOtrasDeducciones = TotalOtrasDeducciones;
            liq.TotalDeducciones = TotalDeducciones;
            liq.LiquidoPagable = LiquidoPagable; liq.LiquidoPagableUs = LiquidoPagableUs;
            liq.CostoLaboratorio = CostoLaboratorio;
            liq.Observaciones = Observaciones; liq.FechaCalculo = FechaLiquidacion;

            if (esNueva) db.Set<Liquidacion>().Add(liq);
            lote.Estado = EstadoLote.Liquidado; lote.FechaLiquidacion = FechaLiquidacion;
            await db.SaveChangesAsync();

            MessageBox.Show("Liquidación guardada correctamente.", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Information);
            OnGuardado?.Invoke();
        }
        catch (Exception ex) { MostrarErr($"Error: {ex.InnerException?.Message ?? ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand] private void Volver() => OnCancelado?.Invoke();

    // ══════════════════════════════════════════
    // EXPORTAR PDF
    // ══════════════════════════════════════════

    [RelayCommand]
    private void ExportarPdf()
    {
        try
        {
            var data = CrearDatosPdf();
            var pdf = new LiquidacionPdfReport(data).Generar();

            var dialog = new SaveFileDialog
            {
                FileName = $"Liquidacion_Lote_{InfoNumeroLote}.pdf",
                Filter = "PDF (*.pdf)|*.pdf",
                Title = "Guardar Liquidación como PDF"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, pdf);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar PDF: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Imprimir()
    {
        try
        {
            var data = CrearDatosPdf();
            var pdf = new LiquidacionPdfReport(data).Generar();

            var tempPath = Path.Combine(Path.GetTempPath(), $"Liquidacion_Lote_{InfoNumeroLote}.pdf");
            File.WriteAllBytes(tempPath, pdf);

            var psi = new ProcessStartInfo(tempPath)
            {
                UseShellExecute = true,
                Verb = "print"
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al imprimir: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private LiquidacionPdfData CrearDatosPdf() => new()
    {
        Proveedor = InfoProveedor, CiNit = InfoCiNit, Mina = InfoMina,
        Cooperativa = InfoCooperativa, TipoMineral = InfoTipoMineral,
        NumeroLote = InfoNumeroLote, PesoNeto = InfoPesoNeto,
        FechaIngreso = InfoFechaIngreso, FechaLiquidacion = FechaLiquidacion,
        TipoCambio = TipoCambio,
        LeyZn = LeyZn, LeyAg = LeyAg, LeyPb = LeyPb,
        Humedad = Humedad, CostoLaboratorio = CostoLaboratorio,
        PesoHumedad = PesoHumedad, PesoNetoSeco = PesoNetoSeco,
        ValorBrutoZn = ValorBrutoZn, ValorBrutoAg = ValorBrutoAg,
        ValorBrutoPb = ValorBrutoPb, ValorComercialUs = ValorComercialUs,
        ValorComercialBs = ValorComercialBs,
        Regalias = Regalias, CNS = Cns, COMIBOL = Comibol,
        TotalDeduccionesLegales = TotalDeduccionesLegales,
        FENCOMIN = Fencomin, FEDECOMIN = Fedecomin,
        PorcentajeCooperativa = PorcentajeCooperativa,
        MontoCooperativa = MontoCooperativa, IUE = Iue,
        TotalOtrasDeducciones = TotalOtrasDeducciones,
        TotalDeducciones = TotalDeducciones,
        LiquidoPagable = LiquidoPagable, LiquidoPagableUs = LiquidoPagableUs,
        MontoLiteral = MontoLiteral,
        Anticipo = AnticipoMonto, SaldoPagar = SaldoPagar,
        BonoTransporte = _bonoTransporte,
        Observaciones = Observaciones,
        EmpresaNombre = _empresaNombre, NombreLiquidador = _empresaLiquidador, EmpresaLogo = _empresaLogo
    };

    private void MostrarErr(string m) { MensajeError = m; TieneError = true; }

    // ══════════════════════════════════════════
    // NÚMERO A LITERAL
    // ══════════════════════════════════════════

    public static string NumeroALiteral(decimal monto)
    {
        if (monto < 0) return "MENOS " + NumeroALiteral(Math.Abs(monto));
        var entero = (long)Math.Truncate(Math.Abs(monto));
        var centavos = (int)Math.Round((Math.Abs(monto) - entero) * 100);
        return $"SON: {EnteroALetras(entero).ToUpper()} {centavos:00}/100 BOLIVIANOS";
    }

    private static string EnteroALetras(long n)
    {
        if (n == 0) return "CERO";
        var p = new List<string>();
        if (n >= 1_000_000) { var m = n / 1_000_000; p.Add(m == 1 ? "UN MILLÓN" : EnteroALetras(m) + " MILLONES"); n %= 1_000_000; }
        if (n >= 1000) { var k = n / 1000; p.Add(k == 1 ? "MIL" : EnteroALetras(k) + " MIL"); n %= 1000; }
        if (n >= 100) { if (n == 100) { p.Add("CIEN"); n = 0; } else { var c = new[] { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" }; p.Add(c[n / 100]); n %= 100; } }
        if (n >= 20) { if (n == 20) { p.Add("VEINTE"); n = 0; } else if (n < 30) { p.Add("VEINTI" + U(n % 10)); n = 0; } else { var d = new[] { "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" }; p.Add(n % 10 == 0 ? d[n / 10] : d[n / 10] + " Y " + U(n % 10)); n = 0; } }
        else if (n >= 10) { var e = new[] { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" }; p.Add(e[n - 10]); n = 0; }
        if (n > 0 && n < 10) p.Add(U(n));
        return string.Join(" ", p);
    }

    private static string U(long n) => n switch { 1 => "UN", 2 => "DOS", 3 => "TRES", 4 => "CUATRO", 5 => "CINCO", 6 => "SEIS", 7 => "SIETE", 8 => "OCHO", 9 => "NUEVE", _ => "" };
}
