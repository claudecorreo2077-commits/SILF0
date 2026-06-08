using SILF.Core.Models;
using SILF.Core.Enums;

namespace SILF.App.ViewModels;

/// <summary>
/// Servicio de sesión. Mantiene el usuario logueado y sus permisos.
/// Se inyecta como Singleton.
/// </summary>
public class SesionService
{
    public Usuario? UsuarioActual { get; private set; }

    public bool EstaLogueado => UsuarioActual != null;

    public bool EsAdmin => UsuarioActual?.Rol == RolUsuario.Administrador;

    public void IniciarSesion(Usuario usuario)
    {
        UsuarioActual = usuario;
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
    }
}
