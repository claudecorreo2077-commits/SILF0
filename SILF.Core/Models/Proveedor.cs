using System.ComponentModel.DataAnnotations;

namespace SILF.Core.Models;

/// <summary>
/// Proveedor (socio minero) que entrega mineral.
/// Si el CI/NIT ya existe, se autocompletan los datos.
/// Un proveedor puede tener N lotes.
/// </summary>
public class Proveedor
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Cédula de identidad o NIT. Clave para autocompletar.</summary>
    [Required, MaxLength(50)]
    public string CiNit { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Telefono { get; set; }

    // FK Cooperativa
    public int? CooperativaId { get; set; }
    public Cooperativa? Cooperativa { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
}
