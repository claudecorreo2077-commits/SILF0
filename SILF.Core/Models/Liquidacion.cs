// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\Liquidacion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

public class Liquidacion
{
    public int Id { get; set; }

    // ── ENTRADAS DEL LABORATORIO ──
    [Column(TypeName = "decimal(8,4)")]
    public decimal Humedad { get; set; }

    // ── COTIZACIONES ──
    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionZn { get; set; }

    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionAg { get; set; }

    [Column(TypeName = "decimal(12,4)")]
    public decimal CotizacionPb { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal TipoCambio { get; set; }

    // ── CÁLCULOS INTERMEDIOS ──
    [Column(TypeName = "decimal(14,4)")]
    public decimal PesoHumedad { get; set; }

    [Column(TypeName = "decimal(14,4)")]
    public decimal PesoNetoSeco { get; set; }

    // ── VALOR COMERCIAL ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoZn { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoAg { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoPb { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorComercialUs { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorComercialBs { get; set; }

    // ── DEDUCCIONES LEGALES ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal Regalias { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal CNS { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal COMIBOL { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalDeduccionesLegales { get; set; }

    // ── OTRAS DEDUCCIONES ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal FENCOMIN { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal FEDECOMIN { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal PorcentajeCooperativa { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal MontoCooperativa { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Anticipo { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal IUE { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalOtrasDeducciones { get; set; }

    // ── RESULTADO ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalDeducciones { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal LiquidoPagable { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal LiquidoPagableUs { get; set; }

    // ── COSTO LABORATORIO ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal CostoLaboratorio { get; set; }

    // ── METADATA ──
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public DateTime? FechaCalculo { get; set; }

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
