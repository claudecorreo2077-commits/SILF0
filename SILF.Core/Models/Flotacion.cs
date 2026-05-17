using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Registro de inversión-flotación. Relación 1:1 con Lote.
/// Se genera simultáneamente con la liquidación.
/// </summary>
public class Flotacion
{
    public int Id { get; set; }

    // ── Datos del proceso ──
    [Column(TypeName = "decimal(12,2)")]
    public decimal? PesoConcentrado { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? RecuperacionZn { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? RecuperacionAg { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? RecuperacionPb { get; set; }

    // ── Costos de inversión ──
    [Column(TypeName = "decimal(14,2)")]
    public decimal CostoReactivos { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal CostoMolienda { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal CostoManoObra { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal OtrosCostos { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalInversion { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
