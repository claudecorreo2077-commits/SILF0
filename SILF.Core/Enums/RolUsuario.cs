namespace SILF.Core.Enums;

/// <summary>
/// Roles del sistema.
/// Admin: acceso completo a todos los módulos.
/// Contador: solo caja chica (crear y consultar, sin editar ni eliminar).
/// </summary>
public enum RolUsuario
{
    Administrador = 0,
    Contador = 1
}
