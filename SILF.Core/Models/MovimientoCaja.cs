using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Movimiento del libro diario de caja chica.
/// Registra entradas y salidas con saldo acumulado.
/// </summary>
public class MovimientoCaja
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required, MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Column(TypeName = "decimal(14,2)")]
    public decimal Entrada { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Salida { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Saldo { get; set; }

    /// <summary>FK opcional al recibo que originó este movimiento.</summary>
    public int? ReciboCajaId { get; set; }
    public ReciboCaja? ReciboCaja { get; set; }

    public bool Visible { get; set; } = true;
}
