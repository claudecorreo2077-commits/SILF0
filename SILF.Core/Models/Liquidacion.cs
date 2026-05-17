using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Liquidación del lote. Relación 1:1 con Lote.
/// Contiene los cálculos de valor comercial, deducciones y líquido pagable.
/// </summary>
public class Liquidacion
{
    public int Id { get; set; }

    // ── Cotizaciones del día ──
    [Column(TypeName = "decimal(12,4)")]
    public decimal? CotizacionZn { get; set; }

    [Column(TypeName = "decimal(12,4)")]
    public decimal? CotizacionAg { get; set; }

    [Column(TypeName = "decimal(12,4)")]
    public decimal? CotizacionPb { get; set; }

    // ── Valores calculados ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoZn { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoAg { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoPb { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal ValorBrutoTotal { get; set; }

    // ── Deducciones ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalDeducciones { get; set; }

    // ── Resultado ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal LiquidoPagable { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public DateTime? FechaCalculo { get; set; }

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
