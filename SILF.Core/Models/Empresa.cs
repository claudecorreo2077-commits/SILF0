using System.ComponentModel.DataAnnotations;

namespace SILF.Core.Models;

/// <summary>
/// Datos de la empresa minera. Solo existe un registro.
/// </summary>
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

    /// <summary>Ruta al archivo de logo en disco.</summary>
    [MaxLength(500)]
    public string? LogoPath { get; set; }
}
