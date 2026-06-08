// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\ArqueoCaja.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

/// <summary>
/// Arqueo de caja chica. Conciliación entre saldo contable y efectivo físico.
/// Cada arqueo tiene un identificador único global (Guid) que permite
/// transferirlo entre PCs sin colisión de Ids autoincrementales.
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

    // ══════════════════════════════════════════
    // CAMPOS DE EXPORTACIÓN/IMPORTACIÓN
    // ══════════════════════════════════════════

    /// <summary>
    /// Identificador único global del arqueo (Guid). Se genera al crear el arqueo
    /// y NUNCA cambia. Es la clave que usamos al transferir el arqueo entre PCs
    /// para detectar duplicados al importar.
    /// </summary>
    [Required, MaxLength(36)]
    public string IdentificadorUnico { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// True si este arqueo ya fue exportado a un archivo .silf-arqueo.
    /// Permite que el Contador filtre "arqueos pendientes de enviar al Admin".
    /// </summary>
    public bool Exportado { get; set; } = false;

    /// <summary>
    /// Fecha en que se exportó (null si nunca fue exportado).
    /// </summary>
    public DateTime? FechaExportacion { get; set; }

    /// <summary>
    /// Si el arqueo vino importado de otra PC, esta cadena describe el origen
    /// (ej: "PC-CONTADOR · Juan Pérez · 2026-05-24 15:30"). Null si el arqueo
    /// se creó localmente en esta PC.
    /// </summary>
    [MaxLength(200)]
    public string? OrigenImportacion { get; set; }

    // ══════════════════════════════════════════
    // PROPIEDADES DERIVADAS (no persisten en BD)
    // ══════════════════════════════════════════

    /// <summary>true si el arqueo fue importado desde otra PC.</summary>
    [NotMapped]
    public bool EsImportado => !string.IsNullOrWhiteSpace(OrigenImportacion);
}
