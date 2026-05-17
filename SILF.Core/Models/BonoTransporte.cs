using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Bono de transporte variable por lote. Recibo recortable media carta.
/// </summary>
public class BonoTransporte
{
    public int Id { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    [MaxLength(200)]
    public string? Beneficiario { get; set; }

    [MaxLength(500)]
    public string? Concepto { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    // ── FK Lote (1:1) ──
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
}
