using CommunityToolkit.Mvvm.ComponentModel;

namespace SILF.App.ViewModels;

/// <summary>
/// Clase base para todos los ViewModels.
/// Hereda de ObservableObject (CommunityToolkit.Mvvm) para notificación automática.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private string _titulo = string.Empty;
    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }

    private bool _cargando;
    public bool Cargando
    {
        get => _cargando;
        set => SetProperty(ref _cargando, value);
    }
}
