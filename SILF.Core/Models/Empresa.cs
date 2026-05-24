// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\Empresa.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SILF.Core.Models;

public class Empresa
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string RazonSocial { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NIT { get; set; }

    [MaxLength(300)]
    public string? Direccion { get; set; }

    [MaxLength(100)]
    public string? Telefono { get; set; }

    [MaxLength(100)]
    public string? Municipio { get; set; }

    [MaxLength(100)]
    public string? Ingenio { get; set; }

    [MaxLength(500)]
    public string? LogoPath { get; set; }

    /// <summary>Nombre del liquidador para firmas en reportes PDF.</summary>
    [MaxLength(200)]
    public string? NombreLiquidador { get; set; }

    /// <summary>
    /// Tipo de cambio USD → Bs usado SOLO para el cálculo de Regalías Mineras (6%).
    /// Valor referencial: 6.96 (puede modificarse desde Configuración).
    /// </summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal TipoCambioRegalias { get; set; } = 6.96m;

    /// <summary>
    /// Tipo de cambio USD → Bs usado para el resto de cálculos:
    /// valor comercial, CNS, COMIBOL, FENCOMIN, FEDECOMIN, Cooperativa, IUE.
    /// Valor referencial: 6.90 (puede modificarse desde Configuración).
    /// </summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal TipoCambioGeneral { get; set; } = 6.90m;
}
