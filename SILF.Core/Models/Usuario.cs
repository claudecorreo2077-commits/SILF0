using System.ComponentModel.DataAnnotations;
using SILF.Core.Enums;

namespace SILF.Core.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Hash SHA256 de la contraseña.</summary>
    [Required, MaxLength(128)]
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Contador;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
