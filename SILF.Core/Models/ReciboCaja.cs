using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Recibo de caja chica. Nº autoincremental.
/// Se imprime duplicado (2 copias en hoja carta, recortable).
/// Incluye conversión de número a letras.
/// </summary>
public class ReciboCaja
{
    public int Id { get; set; }

    /// <summary>Número correlativo del recibo.</summary>
    public int NumeroRecibo { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required, MaxLength(200)]
    public string Beneficiario { get; set; } = string.Empty;

    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    /// <summary>Monto en letras, generado automáticamente.</summary>
    [MaxLength(500)]
    public string? MontoEnLetras { get; set; }

    [Required, MaxLength(500)]
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Si es false, no aparece en reportes.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Tipo: Entrada o Salida.</summary>
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = "Salida";

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
