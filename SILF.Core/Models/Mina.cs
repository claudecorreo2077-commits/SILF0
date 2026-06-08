using System.ComponentModel.DataAnnotations;

namespace SILF.Core.Models;

/// <summary>
/// Mina o paraje de origen del mineral.
/// Valores conocidos: CERRO, PORCO R.L., HUAYNA PORCO.
/// Catálogo editable con botón +AGREGAR.
/// </summary>
public class Mina
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Paraje { get; set; }

    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
}
