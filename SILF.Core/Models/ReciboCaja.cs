// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\ReciboCaja.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Recibo de caja chica. Nº autoincremental.
/// Se imprime duplicado (2 copias en hoja carta, recortable).
/// Incluye conversión de número a letras.
/// Mapea la hoja "Base" del Excel original.
/// </summary>
public class ReciboCaja
{
    public int Id { get; set; }

    /// <summary>Número correlativo del recibo (columna A del Excel).</summary>
    public int NumeroRecibo { get; set; }

    /// <summary>Fecha del recibo (columna B).</summary>
    public DateTime Fecha { get; set; } = DateTime.Now;

    /// <summary>Nombre del beneficiario (columna C).</summary>
    [Required, MaxLength(200)]
    public string Beneficiario { get; set; } = string.Empty;

    /// <summary>Monto en bolivianos (columna D).</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    /// <summary>Monto en letras, generado automáticamente.</summary>
    [MaxLength(500)]
    public string? MontoEnLetras { get; set; }

    /// <summary>Por concepto de... (columna E).</summary>
    [Required, MaxLength(500)]
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Categoría contable: APORTE, TRANSPORTE, ANTICIPOS, SUELDOS Y SALARIOS, etc. (columna G).</summary>
    [MaxLength(100)]
    public string? Cuenta { get; set; }

    /// <summary>Tipo de movimiento: Entrada o Salida.</summary>
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = "Salida";

    /// <summary>Observaciones adicionales (columna H).</summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Si es false, no aparece en reportes ni listados normales.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Persona que entrega conforme (firma del recibo).</summary>
    [MaxLength(200)]
    public string? EntregadoPor { get; set; }
}
