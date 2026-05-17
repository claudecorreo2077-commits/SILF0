// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\CooperativasViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SILF.Core.Models;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class CooperativasViewModel : BaseViewModel
{
    public ObservableCollection<CatalogoItem> Items { get; } = new();

    [ObservableProperty] private int _total;

    // ── Diálogo crear/editar ──
    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nueva Cooperativa";
    [ObservableProperty] private string _editNombre = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hayError;
    private int? _editandoId;

    // ── Confirmar eliminar ──
    [ObservableProperty] private bool _confirmarAbierto;
    [ObservableProperty] private string _mensajeConfirmar = string.Empty;
    [ObservableProperty] private bool _puedeEliminar;
    private int? _eliminandoId;

    [RelayCommand]
    public async Task CargarDatos()
    {
        using var db = new SilfDbContext();
        var lista = await db.Cooperativas.OrderBy(c => c.Nombre).ToListAsync();

        Items.Clear();
        foreach (var c in lista)
        {
            Items.Add(new CatalogoItem
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Cantidad = await db.Proveedores.CountAsync(p => p.CooperativaId == c.Id)
            });
        }
        Total = Items.Count;
    }

    [RelayCommand]
    private void Nuevo()
    {
        _editandoId = null;
        DialogoTitulo = "Nueva Cooperativa";
        EditNombre = string.Empty;
        Error = string.Empty; HayError = false;
        DialogoAbierto = true;
    }

    [RelayCommand]
    private void Editar(CatalogoItem? item)
    {
        if (item is null) return;
        _editandoId = item.Id;
        DialogoTitulo = "Editar Cooperativa";
        EditNombre = item.Nombre ?? string.Empty;
        Error = string.Empty; HayError = false;
        DialogoAbierto = true;
    }

    [RelayCommand]
    private async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(EditNombre))
        { Error = "El nombre es obligatorio."; HayError = true; return; }

        using var db = new SilfDbContext();

        var duplicado = await db.Cooperativas
            .AnyAsync(c => c.Nombre == EditNombre.Trim() && c.Id != (_editandoId ?? 0));
        if (duplicado)
        { Error = $"Ya existe la cooperativa '{EditNombre.Trim()}'."; HayError = true; return; }

        if (_editandoId.HasValue)
        {
            var coop = await db.Cooperativas.FindAsync(_editandoId.Value);
            if (coop is not null) coop.Nombre = EditNombre.Trim();
        }
        else
        {
            db.Cooperativas.Add(new Cooperativa { Nombre = EditNombre.Trim() });
        }

        await db.SaveChangesAsync();
        DialogoAbierto = false;
        await CargarDatos();
    }

    [RelayCommand]
    private void CancelarDialogo() => DialogoAbierto = false;

    [RelayCommand]
    private void PedirEliminar(CatalogoItem? item)
    {
        if (item is null) return;

        if (item.Cantidad > 0)
        {
            MensajeConfirmar = $"No se puede eliminar «{item.Nombre}» porque tiene {item.Cantidad} proveedor(es) asociado(s).";
            PuedeEliminar = false;
        }
        else
        {
            MensajeConfirmar = $"¿Eliminar la cooperativa «{item.Nombre}»?";
            _eliminandoId = item.Id;
            PuedeEliminar = true;
        }
        ConfirmarAbierto = true;
    }

    [RelayCommand]
    private async Task ConfirmarEliminar()
    {
        if (_eliminandoId is not null)
        {
            using var db = new SilfDbContext();
            var coop = await db.Cooperativas.FindAsync(_eliminandoId.Value);
            if (coop is not null) { db.Cooperativas.Remove(coop); await db.SaveChangesAsync(); }
        }
        ConfirmarAbierto = false; _eliminandoId = null;
        await CargarDatos();
    }

    [RelayCommand]
    private void CancelarEliminar() { ConfirmarAbierto = false; _eliminandoId = null; }
}

/// <summary>DTO genérico para catálogos simples (Cooperativas, Minas).</summary>
public class CatalogoItem
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public int Cantidad { get; set; }
}
