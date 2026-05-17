// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\UsuariosViewModel.cs
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SILF.Core.Enums;
using SILF.Core.Models;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class UsuariosViewModel : BaseViewModel
{
    private readonly bool _esAdmin;
    private readonly int _usuarioActualId;

    public UsuariosViewModel(bool esAdmin, int usuarioActualId)
    {
        _esAdmin = esAdmin;
        _usuarioActualId = usuarioActualId;
    }

    public bool EsAdmin => _esAdmin;

    public ObservableCollection<UsuarioItem> Usuarios { get; } = new();

    // ══════════════════════════════════════════
    // DIÁLOGO: CREAR / EDITAR USUARIO
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nuevo Usuario";
    [ObservableProperty] private string _editNombreCompleto = string.Empty;
    [ObservableProperty] private string _editNombreUsuario = string.Empty;
    [ObservableProperty] private string _editPassword = string.Empty;
    [ObservableProperty] private string _editRol = "Contador";
    [ObservableProperty] private bool _mostrarPassword = true;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _tieneError;

    public List<string> RolesDisponibles { get; } = new() { "Administrador", "Contador" };

    private int? _editandoId;

    // ══════════════════════════════════════════
    // DIÁLOGO: CAMBIAR CONTRASEÑA
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _cambioPassAbierto;
    [ObservableProperty] private string _cambioPassNombre = string.Empty;
    [ObservableProperty] private string _nuevaPassword = string.Empty;
    [ObservableProperty] private string _confirmarPassword = string.Empty;
    [ObservableProperty] private string _passError = string.Empty;
    [ObservableProperty] private bool _passHayError;
    private int? _cambioPassUsuarioId;

    // ══════════════════════════════════════════
    // DIÁLOGO: CONFIRMAR ELIMINAR
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _confirmarEliminarAbierto;
    [ObservableProperty] private string _mensajeConfirmar = string.Empty;
    [ObservableProperty] private bool _puedeEliminar;
    private int? _eliminandoId;

    // ══════════════════════════════════════════
    // CARGAR
    // ══════════════════════════════════════════

    [RelayCommand]
    public async Task CargarDatos()
    {
        using var db = new SilfDbContext();
        var lista = await db.Usuarios.OrderBy(u => u.NombreCompleto).ToListAsync();

        Usuarios.Clear();
        foreach (var u in lista)
        {
            Usuarios.Add(new UsuarioItem
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                NombreUsuario = u.NombreUsuario,
                Rol = u.Rol.ToString(),
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            });
        }
    }

    // ══════════════════════════════════════════
    // NUEVO USUARIO (solo admin)
    // ══════════════════════════════════════════

    [RelayCommand]
    private void NuevoUsuario()
    {
        _editandoId = null;
        DialogoTitulo = "Nuevo Usuario";
        EditNombreCompleto = string.Empty;
        EditNombreUsuario = string.Empty;
        EditPassword = string.Empty;
        EditRol = "Contador";
        MostrarPassword = true;
        MensajeError = string.Empty;
        TieneError = false;
        DialogoAbierto = true;
    }

    // ══════════════════════════════════════════
    // EDITAR USUARIO (solo admin)
    // ══════════════════════════════════════════

    [RelayCommand]
    private void EditarUsuario(UsuarioItem? item)
    {
        if (item is null) return;

        _editandoId = item.Id;
        DialogoTitulo = "Editar Usuario";
        EditNombreCompleto = item.NombreCompleto ?? string.Empty;
        EditNombreUsuario = item.NombreUsuario ?? string.Empty;
        EditPassword = string.Empty;
        EditRol = item.Rol ?? "Contador";
        MostrarPassword = false;
        MensajeError = string.Empty;
        TieneError = false;
        DialogoAbierto = true;
    }

    // ══════════════════════════════════════════
    // GUARDAR USUARIO
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GuardarUsuario()
    {
        if (string.IsNullOrWhiteSpace(EditNombreCompleto))
        {
            MensajeError = "El nombre completo es obligatorio.";
            TieneError = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(EditNombreUsuario))
        {
            MensajeError = "El nombre de usuario es obligatorio.";
            TieneError = true;
            return;
        }

        using var db = new SilfDbContext();

        var duplicado = await db.Usuarios
            .AnyAsync(u => u.NombreUsuario == EditNombreUsuario.Trim() && u.Id != (_editandoId ?? 0));
        if (duplicado)
        {
            MensajeError = $"El usuario '{EditNombreUsuario.Trim()}' ya existe.";
            TieneError = true;
            return;
        }

        var rol = EditRol == "Administrador" ? RolUsuario.Administrador : RolUsuario.Contador;

        if (_editandoId.HasValue)
        {
            var usuario = await db.Usuarios.FindAsync(_editandoId.Value);
            if (usuario is null) return;

            usuario.NombreCompleto = EditNombreCompleto.Trim();
            usuario.NombreUsuario = EditNombreUsuario.Trim();
            usuario.Rol = rol;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(EditPassword))
            {
                MensajeError = "La contraseña es obligatoria para usuarios nuevos.";
                TieneError = true;
                return;
            }

            db.Usuarios.Add(new Usuario
            {
                NombreCompleto = EditNombreCompleto.Trim(),
                NombreUsuario = EditNombreUsuario.Trim(),
                PasswordHash = HashPassword(EditPassword),
                Rol = rol,
                Activo = true,
                FechaCreacion = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
        DialogoAbierto = false;
        await CargarDatos();
    }

    [RelayCommand]
    private void CancelarDialogo() => DialogoAbierto = false;

    // ══════════════════════════════════════════
    // CAMBIAR CONTRASEÑA
    // ══════════════════════════════════════════

    [RelayCommand]
    private void AbrirCambioPassword(UsuarioItem? item)
    {
        if (item is null) return;

        _cambioPassUsuarioId = item.Id;
        CambioPassNombre = item.NombreCompleto ?? "Usuario";
        NuevaPassword = string.Empty;
        ConfirmarPassword = string.Empty;
        PassError = string.Empty;
        PassHayError = false;
        CambioPassAbierto = true;
    }

    /// <summary>Para que el usuario actual cambie su propia contraseña.</summary>
    [RelayCommand]
    private void CambiarMiPassword()
    {
        _cambioPassUsuarioId = _usuarioActualId;
        CambioPassNombre = "Mi contraseña";
        NuevaPassword = string.Empty;
        ConfirmarPassword = string.Empty;
        PassError = string.Empty;
        PassHayError = false;
        CambioPassAbierto = true;
    }

    [RelayCommand]
    private async Task GuardarNuevaPassword()
    {
        if (string.IsNullOrWhiteSpace(NuevaPassword))
        {
            PassError = "La contraseña no puede estar vacía.";
            PassHayError = true;
            return;
        }
        if (NuevaPassword.Length < 4)
        {
            PassError = "La contraseña debe tener al menos 4 caracteres.";
            PassHayError = true;
            return;
        }
        if (NuevaPassword != ConfirmarPassword)
        {
            PassError = "Las contraseñas no coinciden.";
            PassHayError = true;
            return;
        }

        using var db = new SilfDbContext();
        var usuario = await db.Usuarios.FindAsync(_cambioPassUsuarioId);
        if (usuario is null) return;

        usuario.PasswordHash = HashPassword(NuevaPassword);
        await db.SaveChangesAsync();

        CambioPassAbierto = false;
    }

    [RelayCommand]
    private void CancelarCambioPassword() => CambioPassAbierto = false;

    // ══════════════════════════════════════════
    // ELIMINAR USUARIO
    // ══════════════════════════════════════════

    [RelayCommand]
    private void PedirEliminar(UsuarioItem? item)
    {
        if (item is null) return;

        if (item.Id == 1)
        {
            MensajeConfirmar = "No se puede eliminar al administrador principal.";
            PuedeEliminar = false;
        }
        else if (item.Id == _usuarioActualId)
        {
            MensajeConfirmar = "No podés eliminar tu propia cuenta.";
            PuedeEliminar = false;
        }
        else
        {
            MensajeConfirmar = $"¿Eliminar al usuario «{item.NombreCompleto}» ({item.NombreUsuario})?";
            _eliminandoId = item.Id;
            PuedeEliminar = true;
        }
        ConfirmarEliminarAbierto = true;
    }

    [RelayCommand]
    private async Task ConfirmarEliminar()
    {
        if (_eliminandoId is not null)
        {
            using var db = new SilfDbContext();
            var usuario = await db.Usuarios.FindAsync(_eliminandoId.Value);
            if (usuario is not null)
            {
                db.Usuarios.Remove(usuario);
                await db.SaveChangesAsync();
            }
        }

        ConfirmarEliminarAbierto = false;
        _eliminandoId = null;
        await CargarDatos();
    }

    [RelayCommand]
    private void CancelarEliminar()
    {
        ConfirmarEliminarAbierto = false;
        _eliminandoId = null;
    }

    // ══════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(bytes);
    }
}

public class UsuarioItem
{
    public int Id { get; set; }
    public string? NombreCompleto { get; set; }
    public string? NombreUsuario { get; set; }
    public string? Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
