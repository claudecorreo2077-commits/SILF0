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

    /// <summary>Tipo de cambio USD → Bs. Se usa en liquidaciones.</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal TipoCambio { get; set; } = 6.97m;
}
