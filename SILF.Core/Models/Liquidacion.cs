// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\Liquidacion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Liquidación del lote. Relación 1:1 con Lote.
/// Almacena snapshot completo: entradas, cálculos intermedios, deducciones y resultado.
/// Fórmulas extraídas del Excel FORMATO DE LIQUIDACIÓN.
/// </summary>
public class Liquidacion
{
    public int Id { get; set; }

    // ══════════════════════════════════════════
    // ENTRADAS DEL LABORATORIO
    // ══════════════════════════════════════════

    /// <summary>% Humedad del mineral.</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal Humedad { get; set; }

    // ══════════════════════════════════════════
    // COTIZACIONES DEL DÍA (precios por unidad)
    // ══════════════════════════════════════════

    /// <summary>Precio del Zinc (por unidad de ley).</summary>
    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionZn { get; set; }

    /// <summary>Precio de la Plata (por oz troy).</summary>
    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionAg { get; set; }

    /// <summary>Precio del Plomo (por unidad de ley).</summary>
    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionPb { get; set; }

    /// <summary>Tipo de cambio USD→Bs usado en esta liquidación (snapshot).</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal TipoCambio { get; set; }

    // ══════════════════════════════════════════
    // CÁLCULOS INTERMEDIOS
    // Fórmula Excel: F13=ROUND(D16*E16%,8), F16=D16-F13
    // ══════════════════════════════════════════

    /// <summary>PesoNeto × %Humedad / 100</summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal PesoHumedad { get; set; }

    /// <summary>PesoNeto - PesoHumedad</summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal PesoNetoSeco { get; set; }

    // ══════════════════════════════════════════
    // VALOR COMERCIAL
    // Fórmula Excel: K16=(F16*G16*I16)+(F16*H16*J16), L16=K16*L12
    // ══════════════════════════════════════════

    /// <summary>PesoSeco × LeyZN × PrecioZN</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoZn { get; set; }

    /// <summary>PesoSeco × LeyAG × PrecioAG</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoAg { get; set; }

    /// <summary>PesoSeco × LeyPB × PrecioPB</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoPb { get; set; }

    /// <summary>Valor comercial total en $US = Zn + Ag + Pb</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorComercialUs { get; set; }

    /// <summary>Valor comercial total en Bs = $US × T/C</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorComercialBs { get; set; }

    // ══════════════════════════════════════════
    // DEDUCCIONES LEGALES (sobre ValorComercialBs)
    // ══════════════════════════════════════════

    /// <summary>Regalías Mineras: 6% del valor comercial Bs.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Regalias { get; set; }

    /// <summary>CNS: 1.8% del valor comercial Bs.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal CNS { get; set; }

    /// <summary>COMIBOL: 1% del valor comercial Bs.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal COMIBOL { get; set; }

    /// <summary>Subtotal deducciones legales = Regalías + CNS + COMIBOL</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalDeduccionesLegales { get; set; }

    // ══════════════════════════════════════════
    // OTRAS DEDUCCIONES
    // ══════════════════════════════════════════

    /// <summary>FENCOMIN: 0.4% del valor comercial Bs.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal FENCOMIN { get; set; }

    /// <summary>FEDECOMIN: 1% del valor comercial Bs.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal FEDECOMIN { get; set; }

    /// <summary>% variable de cooperativa (puede ser 0).</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal PorcentajeCooperativa { get; set; }

    /// <summary>Monto cooperativa = ValorComercialBs × %Cooperativa.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal MontoCooperativa { get; set; }

    /// <summary>Anticipo ya pagado al proveedor (se descuenta).</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Anticipo { get; set; }

    /// <summary>Retenciones IUE sobre Bienes: 5% (puede ser 0 si no aplica).</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal IUE { get; set; }

    /// <summary>Subtotal otras deducciones.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalOtrasDeducciones { get; set; }

    // ══════════════════════════════════════════
    // RESULTADO FINAL
    // Fórmula Excel: L29=SUM(K19:K27), L30=L17-L29, L31=L30/L12
    // ══════════════════════════════════════════

    /// <summary>Total deducciones = legales + otras.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalDeducciones { get; set; }

    /// <summary>Líquido pagable en Bs = ValorComercialBs - TotalDeducciones.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal LiquidoPagable { get; set; }

    /// <summary>Líquido pagable en $US = LiquidoPagable / TipoCambio.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal LiquidoPagableUs { get; set; }

    // ══════════════════════════════════════════
    // METADATA
    // ══════════════════════════════════════════

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public DateTime? FechaCalculo { get; set; }

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
