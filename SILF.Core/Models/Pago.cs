using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Pago asociado a un lote. Relación 1:1 con Lote.
/// El anticipo se paga al registrar el lote (opcional, no se arrastra).
/// El saldo se paga al completar la liquidación.
/// </summary>
public class Pago
{
    public int Id { get; set; }

    /// <summary>Monto adelantado al proveedor. No se arrastra entre lotes.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Anticipo { get; set; }

    public DateTime? FechaAnticipo { get; set; }

    /// <summary>Saldo = LiquidoPagable - Anticipo.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Saldo { get; set; }

    public DateTime? FechaPagoSaldo { get; set; }

    /// <summary>True cuando se presionó el botón COMPLETADO.</summary>
    public bool Completado { get; set; }

    public DateTime? FechaCompletado { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
