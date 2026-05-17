// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\MinasViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SILF.Core.Models;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class MinasViewModel : BaseViewModel
{
    public ObservableCollection<CatalogoItem> Items { get; } = new();

    [ObservableProperty] private int _total;

    // ── Diálogo crear/editar ──
    [ObservableProperty] private bool _dialogoAbierto;
    [ObservableProperty] private string _dialogoTitulo = "Nueva Mina / Paraje";
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
        var lista = await db.Minas.OrderBy(m => m.Nombre).ToListAsync();

        Items.Clear();
        foreach (var m in lista)
        {
            Items.Add(new CatalogoItem
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Cantidad = await db.Lotes.CountAsync(l => l.MinaId == m.Id)
            });
        }
        Total = Items.Count;
    }

    [RelayCommand]
    private void Nuevo()
    {
        _editandoId = null;
        DialogoTitulo = "Nueva Mina / Paraje";
        EditNombre = string.Empty;
        Error = string.Empty; HayError = false;
        DialogoAbierto = true;
    }

    [RelayCommand]
    private void Editar(CatalogoItem? item)
    {
        if (item is null) return;
        _editandoId = item.Id;
        DialogoTitulo = "Editar Mina / Paraje";
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

        var duplicado = await db.Minas
            .AnyAsync(m => m.Nombre == EditNombre.Trim() && m.Id != (_editandoId ?? 0));
        if (duplicado)
        { Error = $"Ya existe la mina '{EditNombre.Trim()}'."; HayError = true; return; }

        if (_editandoId.HasValue)
        {
            var mina = await db.Minas.FindAsync(_editandoId.Value);
            if (mina is not null) mina.Nombre = EditNombre.Trim();
        }
        else
        {
            db.Minas.Add(new Mina { Nombre = EditNombre.Trim() });
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
            MensajeConfirmar = $"No se puede eliminar «{item.Nombre}» porque tiene {item.Cantidad} lote(s) asociado(s).";
            PuedeEliminar = false;
        }
        else
        {
            MensajeConfirmar = $"¿Eliminar la mina «{item.Nombre}»?";
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
            var mina = await db.Minas.FindAsync(_eliminandoId.Value);
            if (mina is not null) { db.Minas.Remove(mina); await db.SaveChangesAsync(); }
        }
        ConfirmarAbierto = false; _eliminandoId = null;
        await CargarDatos();
    }

    [RelayCommand]
    private void CancelarEliminar() { ConfirmarAbierto = false; _eliminandoId = null; }
}
