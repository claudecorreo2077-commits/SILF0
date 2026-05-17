using System.ComponentModel.DataAnnotations;

namespace SILF.Core.Models;

/// <summary>
/// Cooperativa minera. Catálogo editable con botón +AGREGAR.
/// </summary>
public class Cooperativa
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
}
