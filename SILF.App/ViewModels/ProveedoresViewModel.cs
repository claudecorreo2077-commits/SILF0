// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ProveedoresViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SILF.Core.Models;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class ProveedoresViewModel : BaseViewModel
{
    // ══════════════════════════════════════════
    // LISTA PRINCIPAL
    // ══════════════════════════════════════════

    public ObservableCollection<ProveedorItem> Proveedores { get; } = new();
    private ICollectionView? _vistaFiltrada;

    [ObservableProperty] private string _textoBusqueda = string.Empty;
    [ObservableProperty] private int _totalProveedores;

    partial void OnTextoBusquedaChanged(string value)
    {
        _vistaFiltrada?.Refresh();
        TotalProveedores = _vistaFiltrada?.Cast<object>().Count() ?? 0;
    }

    // ══════════════════════════════════════════
    // DIÁLOGO DE EDICIÓN / CREACIÓN
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nuevo Proveedor";
    [ObservableProperty] private string _editCiNit = string.Empty;
    [ObservableProperty] private string _editNombre = string.Empty;
    [ObservableProperty] private Cooperativa? _editCooperativa;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _tieneError;

    public ObservableCollection<Cooperativa> Cooperativas { get; } = new();

    private int? _editandoId;

    // ══════════════════════════════════════════
    // DIÁLOGO DE CONFIRMACIÓN ELIMINAR
    // ══════════════════════════════════════════

    [ObservableProperty] private bool _confirmarEliminarAbierto;
    [ObservableProperty] private string _mensajeConfirmacion = string.Empty;
    [ObservableProperty] private bool _puedeEliminar;
    private int? _eliminandoId;

    // ══════════════════════════════════════════
    // CARGAR DATOS
    // ══════════════════════════════════════════

    [RelayCommand]
    public async Task CargarDatos()
    {
        using var db = new SilfDbContext();

        var lista = await db.Proveedores
            .Include(p => p.Cooperativa)
            .OrderBy(p => p.NombreCompleto)
            .ToListAsync();

        Proveedores.Clear();
        foreach (var p in lista)
        {
            Proveedores.Add(new ProveedorItem
            {
                Id = p.Id,
                CiNit = p.CiNit,
                NombreCompleto = p.NombreCompleto,
                CooperativaNombre = p.Cooperativa?.Nombre ?? "—",
                CantidadLotes = await db.Lotes.CountAsync(l => l.ProveedorId == p.Id)
            });
        }

        _vistaFiltrada = CollectionViewSource.GetDefaultView(Proveedores);
        _vistaFiltrada.Filter = FiltrarProveedor;
        TotalProveedores = Proveedores.Count;

        // Cargar cooperativas para el combo del diálogo
        var coops = await db.Cooperativas.OrderBy(c => c.Nombre).ToListAsync();
        Cooperativas.Clear();
        foreach (var c in coops) Cooperativas.Add(c);
    }

    private bool FiltrarProveedor(object item)
    {
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        if (item is not ProveedorItem p) return false;

        var texto = TextoBusqueda.ToLowerInvariant();
        return (p.CiNit?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)
            || (p.NombreCompleto?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)
            || (p.CooperativaNombre?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    // ══════════════════════════════════════════
    // NUEVO PROVEEDOR
    // ══════════════════════════════════════════

    [RelayCommand]
    private void NuevoProveedor()
    {
        _editandoId = null;
        DialogoTitulo = "Nuevo Proveedor";
        EditCiNit = string.Empty;
        EditNombre = string.Empty;
        EditCooperativa = null;
        MensajeError = string.Empty;
        TieneError = false;
        DialogoAbierto = true;
    }

    // ══════════════════════════════════════════
    // EDITAR PROVEEDOR
    // ══════════════════════════════════════════

    [RelayCommand]
    private void EditarProveedor(ProveedorItem? item)
    {
        if (item is null) return;

        _editandoId = item.Id;
        DialogoTitulo = "Editar Proveedor";
        EditCiNit = item.CiNit ?? string.Empty;
        EditNombre = item.NombreCompleto ?? string.Empty;
        EditCooperativa = Cooperativas.FirstOrDefault(c => c.Nombre == item.CooperativaNombre);
        MensajeError = string.Empty;
        TieneError = false;
        DialogoAbierto = true;
    }

    // ══════════════════════════════════════════
    // GUARDAR (crear o actualizar)
    // ══════════════════════════════════════════

    [RelayCommand]
    private async Task GuardarProveedor()
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(EditCiNit))
        {
            MostrarError("El CI/NIT es obligatorio.");
            return;
        }
        if (string.IsNullOrWhiteSpace(EditNombre))
        {
            MostrarError("El nombre es obligatorio.");
            return;
        }

        using var db = new SilfDbContext();

        // Verificar duplicado de CI/NIT
        var duplicado = await db.Proveedores
            .AnyAsync(p => p.CiNit == EditCiNit.Trim() && p.Id != (_editandoId ?? 0));

        if (duplicado)
        {
            MostrarError($"Ya existe un proveedor con CI/NIT '{EditCiNit.Trim()}'.");
            return;
        }

        if (_editandoId.HasValue)
        {
            // ── Actualizar ──
            var proveedor = await db.Proveedores.FindAsync(_editandoId.Value);
            if (proveedor is null) return;

            proveedor.CiNit = EditCiNit.Trim();
            proveedor.NombreCompleto = EditNombre.Trim();
            proveedor.CooperativaId = EditCooperativa?.Id;
        }
        else
        {
            // ── Crear ──
            db.Proveedores.Add(new Proveedor
            {
                CiNit = EditCiNit.Trim(),
                NombreCompleto = EditNombre.Trim(),
                CooperativaId = EditCooperativa?.Id
            });
        }

        await db.SaveChangesAsync();
        DialogoAbierto = false;
        await CargarDatos();
    }

    // ══════════════════════════════════════════
    // ELIMINAR PROVEEDOR
    // ══════════════════════════════════════════

    [RelayCommand]
    private void PedirEliminar(ProveedorItem? item)
    {
        if (item is null) return;

        if (item.CantidadLotes > 0)
        {
            MensajeConfirmacion = $"No se puede eliminar a «{item.NombreCompleto}» porque tiene {item.CantidadLotes} lote(s) registrado(s).";
            _eliminandoId = null;
            PuedeEliminar = false;
        }
        else
        {
            MensajeConfirmacion = $"¿Eliminar al proveedor «{item.NombreCompleto}» (CI: {item.CiNit})?";
            _eliminandoId = item.Id;
            PuedeEliminar = true;
        }
        ConfirmarEliminarAbierto = true;
    }

    [RelayCommand]
    private async Task ConfirmarEliminar()
    {
        if (_eliminandoId is null)
        {
            ConfirmarEliminarAbierto = false;
            return;
        }

        using var db = new SilfDbContext();
        var proveedor = await db.Proveedores.FindAsync(_eliminandoId.Value);
        if (proveedor is not null)
        {
            db.Proveedores.Remove(proveedor);
            await db.SaveChangesAsync();
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

    [RelayCommand]
    private void CancelarDialogo()
    {
        DialogoAbierto = false;
    }

    // ══════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════

    private void MostrarError(string msg)
    {
        MensajeError = msg;
        TieneError = true;
    }
}

/// <summary>DTO para la lista visual de proveedores.</summary>
public class ProveedorItem
{
    public int Id { get; set; }
    public string? CiNit { get; set; }
    public string? NombreCompleto { get; set; }
    public string? CooperativaNombre { get; set; }
    public int CantidadLotes { get; set; }
}
