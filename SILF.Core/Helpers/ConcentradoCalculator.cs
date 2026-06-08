// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Helpers\ConcentradoCalculator.cs
using SILF.Core.Enums;

namespace SILF.Core.Helpers;

// ════════════════════════════════════════════════════════════════════════════
//  MOTOR DE CÁLCULO DE CONCENTRADOS
//  Reproduce AL CENTAVO la fórmula de los Excel del cliente
//  (CALCULO_ZN.xlsx -> ZN-AG  y  CALCULO_PB.xlsx -> AG-PB).
//
//  Validado contra las planillas:
//    ZN-AG : Líquido Pagable Bs 2792.32 / $us 320.47
//    AG-PB : Saldo a Pagar     Bs 31893.29 / $us 4582.37
//
//  REGLA DE REDONDEO (igual que Excel):
//   - Solo se redondea donde la planilla aplica ROUND(); los intermedios
//     (pagables, precio/TMS, valor bruto, FOB) van en precisión completa.
//   - La REGALÍA usa su propia cotización (cotización oficial quincenal),
//     distinta de la cotización pagable negociada.
//   - Regalía Zn/Pb redondea el paso final a 3 decimales; Ag a 2.
//   - Las retenciones (COMIBOL, CNS, etc.) se calculan sobre el FOB Bs
//     SIN redondear.
//
//  TODO lo que no es fórmula entra como dato editable (ver ConcentradoInput).
//  Cada concentrado guardado persiste una FOTO (snapshot) de estos parámetros.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Una penalidad por impureza (editable: límite libre y tarifa).</summary>
public class PenalidadItem
{
    public string Nombre { get; set; } = "";
    public decimal Actual { get; set; }   // viene del análisis
    public decimal Libre { get; set; }    // límite libre (editable)
    public decimal Tarifa { get; set; }   // $us por unidad (editable)
    public decimal Monto { get; set; }    // calculado
}

/// <summary>Datos de entrada del cálculo. Todos los campos son editables en pantalla.</summary>
public class ConcentradoInput
{
    public TipoConcentrado Tipo { get; set; }

    // ── Pesos ──
    public decimal Tmh { get; set; }
    public decimal PorcentajeHumedad { get; set; }   // 0.02 = 2%
    public decimal PorcentajeMerma { get; set; }     // 0.01 = 1%

    // ── Leyes / análisis ──
    public decimal LeyZn { get; set; }   // fracción (0.53). Solo ZN-AG.
    public decimal LeyAg { get; set; }   // ZN-AG: "Ag DMs" (5). AG: ley Ag (70). ×100 -> g/Tm
    public decimal LeyPb { get; set; }   // fracción (0.30). Solo AG-PB.

    // ── Impurezas (fracción) ──
    public decimal Fe { get; set; }
    public decimal As { get; set; }
    public decimal Sb { get; set; }
    public decimal Sn { get; set; }
    public decimal Bi { get; set; }
    public decimal SiO2 { get; set; }

    // ── Cotizaciones PAGABLES ──
    public decimal CotizZnLb { get; set; }   // $us/lb
    public decimal CotizAgOz { get; set; }   // $us/oz
    public decimal CotizPbLb { get; set; }   // $us/lb

    // ── Cotizaciones para REGALÍA (oficial quincenal; puede diferir del pagable) ──
    public decimal CotizRegaliaZn { get; set; } = 1.6m;
    public decimal CotizRegaliaAg { get; set; } = 75.27m;
    public decimal CotizRegaliaPb { get; set; } = 0.9m;

    // ── Tipos de cambio ──
    public decimal TcOficial { get; set; } = 6.96m;     // regalías + conversión final $us
    public decimal TcComercial { get; set; } = 6.90m;   // Valor FOB -> Bs (ZN usaba 8.5)

    // ── Alícuotas Regalía Minera ──
    public decimal AlicuotaZn { get; set; } = 0.03m;
    public decimal AlicuotaAg { get; set; } = 0.036m;
    public decimal AlicuotaPb { get; set; } = 0.03m;

    // ── Factores de conversión (constantes físicas) ──
    public decimal FcLb { get; set; } = 2.2046223m;  // lb por kg
    public decimal FcOz { get; set; } = 31.1035m;    // g por onza troy

    // ── Metales pagables ──
    public decimal ZnLibre { get; set; }
    public decimal ZnFactor { get; set; }
    public decimal AgLibreOz { get; set; }
    public decimal AgFactor { get; set; }
    public decimal PbLibre { get; set; }
    public decimal PbFactor { get; set; }

    // ── Maquila / Gastos de tratamiento ──
    public decimal MaquilaBase { get; set; }
    public decimal MaquilaFijo { get; set; }
    public decimal MaquilaEscalador { get; set; }

    // ── Refinación (solo AG-PB) ──
    public decimal RefinacionAgPorOz { get; set; }

    // ── Penalidades ──
    public List<PenalidadItem> Penalidades { get; set; } = new();

    // ── Ajuste manual de otros ($us) — ZN usa -25 ──
    public decimal OtrosAjusteUs { get; set; }

    // ── Fletes ($us por TMH) ──
    public decimal Rollback { get; set; }
    public decimal TransporteTerrestre { get; set; }  // ZN: 165
    public decimal FletePotosiArica { get; set; }      // AG: 90
    public decimal Ahk { get; set; }
    public decimal Molienda { get; set; }
    public decimal ComisionBancariaTasa { get; set; }  // 0.006
    public bool AplicaComisionBancaria { get; set; }
    public bool AplicaAhk { get; set; }

    // ── Retenciones (toggles + tasas) ──
    public bool AplicaRegaliaZn { get; set; }
    public bool AplicaRegaliaAg { get; set; }
    public bool AplicaRegaliaPb { get; set; }
    public bool AplicaComibol { get; set; } = true;   public decimal TasaComibol { get; set; } = 0.01m;
    public bool AplicaCns { get; set; } = true;       public decimal TasaCns { get; set; } = 0.018m;
    public bool AplicaFedecomin { get; set; } = true; public decimal TasaFedecomin { get; set; } = 0.01m;
    public bool AplicaFencomin { get; set; } = true;  public decimal TasaFencomin { get; set; } = 0.004m;
    public bool AplicaWilstermann { get; set; }       public decimal TasaWilstermann { get; set; } = 0.003m;
    public bool AplicaAporteCoop { get; set; }        public decimal TasaAporteCoop { get; set; } = 0.07m;
    public bool AplicaM02 { get; set; }               public decimal TasaM02 { get; set; } = 0m;

    // ── Descuentos ──
    public decimal Anticipo { get; set; }
}

/// <summary>Resultado completo del cálculo.</summary>
public class ConcentradoResultado
{
    public decimal TmsPrevio { get; set; }
    public decimal Tms { get; set; }
    public decimal AgOzPorTm { get; set; }

    public decimal PrecioZnPorTm { get; set; }
    public decimal PagableZn { get; set; }
    public decimal PagableAg { get; set; }
    public decimal PagablePb { get; set; }
    public decimal TotalPagable { get; set; }

    public decimal Maquila { get; set; }
    public decimal Refinacion { get; set; }
    public decimal TotalPenalidades { get; set; }
    public decimal OtrosAjuste { get; set; }

    public decimal PrecioPorTms { get; set; }
    public decimal ValorBruto { get; set; }

    public decimal MontoRollback { get; set; }
    public decimal MontoTransporte { get; set; }
    public decimal MontoFletePotosiArica { get; set; }
    public decimal MontoAhk { get; set; }
    public decimal MontoMolienda { get; set; }
    public decimal MontoComision { get; set; }

    public decimal ValorFobUs { get; set; }
    public decimal ValorFobBs { get; set; }

    public decimal RegaliaZn { get; set; }
    public decimal RegaliaAg { get; set; }
    public decimal RegaliaPb { get; set; }
    public decimal Comibol { get; set; }
    public decimal Cns { get; set; }
    public decimal Fedecomin { get; set; }
    public decimal Fencomin { get; set; }
    public decimal Wilstermann { get; set; }
    public decimal AporteCoop { get; set; }
    public decimal M02 { get; set; }
    public decimal TotalRetenciones { get; set; }

    public decimal Anticipo { get; set; }
    public decimal LiquidoPagableBs { get; set; }
    public decimal LiquidoPagableUs { get; set; }
    public decimal SaldoPagarBs { get; set; }
    public decimal SaldoPagarUs { get; set; }
}

public static class ConcentradoCalculator
{
    // ROUND de Excel = mitad hacia afuera (AwayFromZero).
    private static decimal R(decimal v, int dec = 2) => Math.Round(v, dec, MidpointRounding.AwayFromZero);

    public static ConcentradoResultado Calcular(ConcentradoInput i)
        => i.Tipo == TipoConcentrado.ZnAg ? CalcularZnAg(i) : CalcularAg(i);

    // ─────────────────────────────────────────────────────────────────────────
    //  ZN-AG  (paga Zn y Ag; penalidades NO entran al FOB, se usa "OtrosAjuste")
    // ─────────────────────────────────────────────────────────────────────────
    private static ConcentradoResultado CalcularZnAg(ConcentradoInput i)
    {
        var x = new ConcentradoResultado();

        // Pesos (TMS redondeado a entero, como H16)
        x.TmsPrevio = i.Tmh - i.Tmh * i.PorcentajeHumedad;
        x.Tms = R(x.TmsPrevio - x.TmsPrevio * i.PorcentajeMerma, 0);

        x.AgOzPorTm = i.FcOz == 0 ? 0 : (i.LeyAg * 100m) / i.FcOz;

        // Precio Zn por TM = ROUNDDOWN(cotiz $/lb × 2.2046 × 1000)
        x.PrecioZnPorTm = Math.Floor(i.CotizZnLb * 2.2046m * 1000m);

        // Pagables (sin redondeo)
        var znPct = i.LeyZn <= 0.535m ? (i.LeyZn - i.ZnLibre) : (i.LeyZn * i.ZnFactor);
        x.PagableZn = (znPct * 100m) * x.PrecioZnPorTm / 100m;
        x.PagableAg = (x.AgOzPorTm - i.AgLibreOz) * i.AgFactor * i.CotizAgOz;
        x.TotalPagable = x.PagableZn + x.PagableAg;

        // Maquila (sin redondeo): escalador sobre precio + cargo fijo. Se RESTA.
        x.Maquila = (x.PrecioZnPorTm - i.MaquilaBase) * i.MaquilaEscalador + i.MaquilaFijo;

        // Penalidades (referencia; en ZN no entran al FOB). Sin redondeo por ítem (como el Excel).
        x.TotalPenalidades = CalcularPenalidadesZn(i);
        x.OtrosAjuste = i.OtrosAjusteUs;

        x.PrecioPorTms = x.TotalPagable - x.Maquila + x.OtrosAjuste;
        x.ValorBruto = x.PrecioPorTms * x.Tms / 1000m;

        x.MontoComision = i.AplicaComisionBancaria ? x.ValorBruto * i.ComisionBancariaTasa : 0m;
        x.MontoRollback = i.Tmh * i.Rollback / 1000m;
        x.MontoTransporte = i.Tmh * i.TransporteTerrestre / 1000m;
        x.MontoMolienda = i.Tmh * i.Molienda / 1000m;

        x.ValorFobUs = x.ValorBruto - x.MontoComision - x.MontoRollback - x.MontoTransporte - x.MontoMolienda;
        x.ValorFobBs = x.ValorFobUs * i.TcComercial;

        // Regalías (cotización propia; Zn redondeo final a 3 decimales, Ag a 2)
        x.RegaliaZn = i.AplicaRegaliaZn
            ? Regalia(x.Tms * i.LeyZn, i.FcLb, i.CotizRegaliaZn, i.AlicuotaZn, i.TcOficial, 3) : 0m;
        x.RegaliaAg = i.AplicaRegaliaAg
            ? Regalia(x.Tms * i.LeyAg / 10000m, i.FcOz, i.CotizRegaliaAg, i.AlicuotaAg, i.TcOficial, 2) : 0m;

        CalcularRetencionesYFinal(i, x);
        return x;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  AG-PB  (paga Ag y Pb, con refinación; penalidades SÍ entran al FOB)
    // ─────────────────────────────────────────────────────────────────────────
    private static ConcentradoResultado CalcularAg(ConcentradoInput i)
    {
        var x = new ConcentradoResultado();

        // Pesos (AG-PB no redondea el TMS: E16 = B16 - D16)
        x.TmsPrevio = i.Tmh - i.Tmh * i.PorcentajeHumedad;
        x.Tms = x.TmsPrevio - x.TmsPrevio * i.PorcentajeMerma;

        x.AgOzPorTm = i.FcOz == 0 ? 0 : (i.LeyAg * 100m) / i.FcOz;

        // Precio Pb por TM = cotiz $/lb × FcLb × 1000 (sin ROUNDDOWN)
        x.PrecioZnPorTm = (i.CotizPbLb * i.FcLb) * 1000m;

        // Pagables (sin redondeo)
        x.PagableAg = ((x.AgOzPorTm - i.AgLibreOz) * i.AgFactor) * i.CotizAgOz;
        x.PagablePb = (i.LeyPb - i.PbLibre) * x.PrecioZnPorTm * i.PbFactor;
        x.TotalPagable = x.PagableAg + x.PagablePb;

        // Maquila Pb (negativa): cargo fijo + escalador(redondeado a 2) sobre exceso
        var exceso = x.PrecioZnPorTm - i.MaquilaBase;
        var escalador = exceso < 0 ? 0m : R(exceso * i.MaquilaEscalador, 2);
        x.Maquila = -(i.MaquilaFijo + escalador);

        // Refinación Ag (negativa, redondeada a 2)
        x.Refinacion = R(-((x.AgOzPorTm - i.AgLibreOz) * i.RefinacionAgPorOz), 2);

        // Penalidades (SÍ entran al FOB; cada ítem redondeado a 2)
        x.TotalPenalidades = CalcularPenalidadesAg(i);
        x.OtrosAjuste = i.OtrosAjusteUs;

        x.PrecioPorTms = x.TotalPagable + x.Maquila + x.Refinacion + x.TotalPenalidades + x.OtrosAjuste;
        x.ValorBruto = x.PrecioPorTms * x.Tms / 1000m;

        x.MontoFletePotosiArica = i.Tmh * i.FletePotosiArica / 1000m;
        x.MontoRollback = i.Tmh * i.Rollback / 1000m;
        x.MontoAhk = i.AplicaAhk ? i.Tmh * i.Ahk / 1000m : 0m;
        x.MontoMolienda = i.Tmh * i.Molienda / 1000m;

        x.ValorFobUs = x.ValorBruto - (x.MontoFletePotosiArica + x.MontoRollback + x.MontoAhk + x.MontoMolienda);
        x.ValorFobBs = x.ValorFobUs * i.TcComercial;

        x.RegaliaAg = i.AplicaRegaliaAg
            ? Regalia(x.Tms * i.LeyAg / 10000m, i.FcOz, i.CotizRegaliaAg, i.AlicuotaAg, i.TcOficial, 2) : 0m;
        x.RegaliaPb = i.AplicaRegaliaPb
            ? Regalia(x.Tms * i.LeyPb, i.FcLb, i.CotizRegaliaPb, i.AlicuotaPb, i.TcOficial, 3) : 0m;

        CalcularRetencionesYFinal(i, x);
        return x;
    }

    // ── Regalía minera de un metal (mismo encadenamiento de ROUND que el Excel) ──
    //  kiloFino -> ×FC (lb u oz fino) -> ×cotización -> ×alícuota -> ×T/C
    private static decimal Regalia(decimal kiloFino, decimal factor, decimal cotiz,
        decimal alicuota, decimal tc, int finalDec)
    {
        var p = R(kiloFino, 2);
        var q = R(p * factor, 2);
        var v = R(q * cotiz, 2);
        var u = R(v * alicuota, 2);
        return R(u * tc, finalDec);
    }

    // ── Penalidades ZN: (actual - libre) × tarifa × 100  (sin redondeo por ítem) ──
    private static decimal CalcularPenalidadesZn(ConcentradoInput i)
    {
        decimal total = 0m;
        foreach (var p in i.Penalidades)
        {
            p.Monto = (p.Actual - p.Libre) * p.Tarifa * 100m;
            total += p.Monto;
        }
        return total;
    }

    // ── Penalidades AG: (actual - libre) × tarifa / 0.1%  (cada ítem redondeado a 2) ──
    private static decimal CalcularPenalidadesAg(ConcentradoInput i)
    {
        decimal total = 0m;
        foreach (var p in i.Penalidades)
        {
            var dif = p.Actual - p.Libre;
            p.Monto = dif <= 0 ? 0m : R(dif * p.Tarifa / 0.001m, 2);
            total += p.Monto;
        }
        return total;
    }

    private static void CalcularRetencionesYFinal(ConcentradoInput i, ConcentradoResultado x)
    {
        // Sobre el FOB Bs SIN redondear (como el Excel)
        x.Comibol = i.AplicaComibol ? x.ValorFobBs * i.TasaComibol : 0m;
        x.Cns = i.AplicaCns ? x.ValorFobBs * i.TasaCns : 0m;
        x.Fedecomin = i.AplicaFedecomin ? x.ValorFobBs * i.TasaFedecomin : 0m;
        x.Fencomin = i.AplicaFencomin ? x.ValorFobBs * i.TasaFencomin : 0m;
        x.Wilstermann = i.AplicaWilstermann ? x.ValorFobBs * i.TasaWilstermann : 0m;
        x.AporteCoop = i.AplicaAporteCoop ? x.ValorFobBs * i.TasaAporteCoop : 0m;
        x.M02 = i.AplicaM02 ? x.ValorFobBs * i.TasaM02 : 0m;

        x.TotalRetenciones = x.RegaliaZn + x.RegaliaAg + x.RegaliaPb + x.Comibol + x.Cns
            + x.Fedecomin + x.Fencomin + x.Wilstermann + x.AporteCoop + x.M02;

        x.Anticipo = i.Anticipo;

        x.LiquidoPagableBs = x.ValorFobBs - x.TotalRetenciones;
        x.LiquidoPagableUs = i.TcOficial == 0 ? 0 : x.ValorFobUs - x.TotalRetenciones / i.TcOficial;

        x.SaldoPagarBs = x.LiquidoPagableBs - x.Anticipo;
        x.SaldoPagarUs = i.TcOficial == 0 ? 0 : x.SaldoPagarBs / i.TcOficial;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DEFAULTS DE EJEMPLO (los que el usuario edita). Tomados de los Excel.
    // ═══════════════════════════════════════════════════════════════════════════
    public static ConcentradoInput DefaultsZnAg() => new()
    {
        Tipo = TipoConcentrado.ZnAg,
        Tmh = 270m, PorcentajeHumedad = 0.02m, PorcentajeMerma = 0.01m,
        LeyZn = 0.53m, LeyAg = 5m, LeyPb = 0m,
        Fe = 0.08m, As = 0.03m, Sb = 0.0013m, Sn = 0.0035m, SiO2 = 0.0448m,
        CotizZnLb = 1.6m, CotizAgOz = 75m, CotizPbLb = 0.9m,
        CotizRegaliaZn = 1.6m, CotizRegaliaAg = 75.27m, CotizRegaliaPb = 0.9m,
        TcOficial = 6.96m, TcComercial = 8.5m,
        AlicuotaZn = 0.03m, AlicuotaAg = 0.036m, AlicuotaPb = 0.03m,
        ZnLibre = 0.08m, ZnFactor = 0.85m, AgLibreOz = 3m, AgFactor = 0.70518m,
        MaquilaBase = 0m, MaquilaFijo = 90m, MaquilaEscalador = 0.15m,
        OtrosAjusteUs = -25m,
        Rollback = 70m, TransporteTerrestre = 165m, Molienda = 0m,
        ComisionBancariaTasa = 0.006m, AplicaComisionBancaria = false,
        AplicaRegaliaZn = true, AplicaRegaliaAg = true,
        AplicaComibol = true, AplicaCns = true, AplicaFedecomin = true, AplicaFencomin = true,
        AplicaWilstermann = false, AplicaAporteCoop = false,
        Penalidades = new()
        {
            new PenalidadItem { Nombre = "Fe",   Libre = 0.08m,  Tarifa = 1.5m },
            new PenalidadItem { Nombre = "As",   Libre = 0.005m, Tarifa = 1.5m },
            new PenalidadItem { Nombre = "Sb",   Libre = 0.005m, Tarifa = 1.5m },
            new PenalidadItem { Nombre = "Sn",   Libre = 0.005m, Tarifa = 1.5m },
            new PenalidadItem { Nombre = "SiO2", Libre = 0.03m,  Tarifa = 1.5m },
        }
    };

    public static ConcentradoInput DefaultsAg() => new()
    {
        Tipo = TipoConcentrado.Ag,
        Tmh = 1000m, PorcentajeHumedad = 0m, PorcentajeMerma = 0.01m,
        LeyAg = 70m, LeyPb = 0.30m, LeyZn = 0m,
        As = 0.004m, Sb = 0.004m, Bi = 0.0006m, Sn = 0.005m, SiO2 = 0.005m,
        CotizAgOz = 23m, CotizPbLb = 0.95m, CotizZnLb = 1.6m,
        CotizRegaliaZn = 1.6m, CotizRegaliaAg = 75.27m, CotizRegaliaPb = 0.9m,
        TcOficial = 6.96m, TcComercial = 6.90m,
        AlicuotaAg = 0.036m, AlicuotaPb = 0.03m,
        AgLibreOz = 1.5m, AgFactor = 0.95m, PbLibre = 0.03m, PbFactor = 0.95m,
        MaquilaBase = 2000m, MaquilaFijo = 130m, MaquilaEscalador = 0.15m,
        RefinacionAgPorOz = 1.3m,
        OtrosAjusteUs = 0m,
        FletePotosiArica = 90m, Rollback = 35m, Ahk = 652m, Molienda = 0m,
        AplicaAhk = false,
        AplicaRegaliaAg = false, AplicaRegaliaPb = false,   // la plantilla AG-PB no las calculaba: confirmar con cliente
        AplicaComibol = true, AplicaCns = true, AplicaFedecomin = true, AplicaFencomin = true,
        AplicaAporteCoop = false, AplicaM02 = false,
        Penalidades = new()
        {
            new PenalidadItem { Nombre = "As+Sb", Libre = 0.003m,  Tarifa = 2.5m },
            new PenalidadItem { Nombre = "Sn",    Libre = 0.005m,  Tarifa = 2.5m },
            new PenalidadItem { Nombre = "Bi",    Libre = 0.0005m, Tarifa = 2.5m },
            new PenalidadItem { Nombre = "Zn",    Libre = 0.04m,   Tarifa = 2.5m },
            new PenalidadItem { Nombre = "SiO2",  Libre = 0.005m,  Tarifa = 2.5m },
        }
    };
}
