// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\ReciboCaja.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Recibo de caja chica. Nº autoincremental DENTRO de su talonario.
/// Existen dos talonarios independientes: INGRESOS y SALIDAS. Cada uno
/// tiene su propio correlativo. El identificador único del recibo es
/// la combinación (TipoMovimiento, NumeroRecibo).
///
/// Se imprime duplicado (2 copias en hoja carta, recortable).
/// Incluye conversión de número a letras.
/// </summary>
public class ReciboCaja
{
    public int Id { get; set; }

    /// <summary>
    /// Número correlativo del recibo DENTRO de su talonario (Ingresos o Salidas).
    /// El "Nº visible" en el recibo impreso es "INGRESO #001" o "SALIDA #001"
    /// (ver <see cref="NumeroFormateado"/>).
    /// </summary>
    public int NumeroRecibo { get; set; }

    /// <summary>Fecha del recibo.</summary>
    public DateTime Fecha { get; set; } = DateTime.Now;

    /// <summary>Nombre del beneficiario.</summary>
    [Required, MaxLength(200)]
    public string Beneficiario { get; set; } = string.Empty;

    /// <summary>Monto en bolivianos.</summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal Monto { get; set; }

    /// <summary>Monto en letras, generado automáticamente.</summary>
    [MaxLength(500)]
    public string? MontoEnLetras { get; set; }

    /// <summary>Por concepto de…</summary>
    [Required, MaxLength(500)]
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Categoría contable: APORTE, TRANSPORTE, ANTICIPOS, etc.</summary>
    [MaxLength(100)]
    public string? Cuenta { get; set; }

    /// <summary>
    /// Tipo de movimiento: "Entrada" o "Salida".
    /// Determina a qué talonario pertenece el correlativo.
    /// </summary>
    [MaxLength(20)]
    public string TipoMovimiento { get; set; } = "Salida";

    /// <summary>Observaciones adicionales.</summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Si es false, no aparece en reportes ni listados normales.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Persona que entrega conforme (firma del recibo).</summary>
    [MaxLength(200)]
    public string? EntregadoPor { get; set; }

    /// <summary>
    /// Número visible del recibo con prefijo del talonario.
    /// Ejemplos: "INGRESO #001", "SALIDA #015".
    /// NO mapea a una columna; se calcula en tiempo de ejecución.
    /// </summary>
    [NotMapped]
    public string NumeroFormateado =>
        $"{TipoMovimiento.ToUpperInvariant()} #{NumeroRecibo:D3}";
}
