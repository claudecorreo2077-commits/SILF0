using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Arqueo de caja chica. Conciliación entre saldo contable y efectivo físico.
/// </summary>
public class ArqueoCaja
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(14,2)")]
    public decimal SaldoContable { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal SaldoFisico { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal Diferencia { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(100)]
    public string? RealizadoPor { get; set; }
}
