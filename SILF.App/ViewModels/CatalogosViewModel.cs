// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\CatalogosViewModel.cs
using CommunityToolkit.Mvvm.Input;

namespace SILF.App.ViewModels;

public partial class CatalogosViewModel : BaseViewModel
{
    public ProveedoresViewModel Proveedores { get; } = new();
    public CooperativasViewModel Cooperativas { get; } = new();
    public MinasViewModel Minas { get; } = new();

    [RelayCommand]
    public async Task CargarTodo()
    {
        await Proveedores.CargarDatosCommand.ExecuteAsync(null);
        await Cooperativas.CargarDatosCommand.ExecuteAsync(null);
        await Minas.CargarDatosCommand.ExecuteAsync(null);
    }
}
