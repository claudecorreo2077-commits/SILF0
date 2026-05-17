// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\MainViewModel.cs
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SILF.Core.Enums;

namespace SILF.App.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly SesionService _sesion;

    public MainViewModel(SesionService sesion)
    {
        _sesion = sesion;
        Titulo = "SILF — Sistema Integral de Liquidación y Flotación";
        ModuloSeleccionado = "Inicio";
        SidebarActivo = "Inicio";
        IconoModulo = "Home";
        Breadcrumb = "Inicio";
        _ = NavegarAsync("Inicio");
    }

    public string NombreUsuario => _sesion.UsuarioActual?.NombreCompleto ?? "Usuario";
    public string RolUsuario => _sesion.UsuarioActual?.Rol.ToString() ?? "";
    public bool EsAdmin => _sesion.EsAdmin;
    public string InicialUsuario => _sesion.UsuarioActual?.NombreCompleto?.Substring(0, 1).ToUpper() ?? "U";

    public Visibility VisibilidadLiquidacion => _sesion.EsAdmin ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadFlotacion => _sesion.EsAdmin ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisibilidadConfig => _sesion.EsAdmin ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty] private object? _vistaActual;
    [ObservableProperty] private string _moduloSeleccionado = "Inicio";
    [ObservableProperty] private string _iconoModulo = "Home";
    [ObservableProperty] private string _breadcrumb = "Inicio";
    [ObservableProperty] private bool _cargandoVista;

    /// <summary>Qué ítem del sidebar debe estar resaltado. Los RadioButtons bindean IsChecked a esto.</summary>
    [ObservableProperty] private string _sidebarActivo = "Inicio";

    [RelayCommand]
    public async Task NavegarAsync(string modulo)
    {
        // Determinar qué sidebar resaltar
        SidebarActivo = modulo switch
        {
            "NuevoLote" or "EditarLote" => "Lotes",
            _ => modulo
        };

        ModuloSeleccionado = modulo switch
        {
            "NuevoLote" => "Nuevo Lote",
            "EditarLote" => "Editar Lote",
            _ => modulo
        };

        IconoModulo = modulo switch
        {
            "Inicio"                    => "Home",
            "Lotes" or "NuevoLote" or "EditarLote" => "PackageVariant",
            "Liquidación"               => "Calculator",
            "Flotación"                 => "Flask",
            "Caja Chica"                => "CashRegister",
            "Reportes"                  => "ChartBar",
            "Configuración"             => "Cog",
            _ => "Home"
        };

        Breadcrumb = modulo switch
        {
            "Inicio"     => "Inicio",
            "NuevoLote"  => "SILF  ›  Lotes  ›  Nuevo Lote",
            "EditarLote" => "SILF  ›  Lotes  ›  Editar",
            _            => $"SILF  ›  {ModuloSeleccionado}"
        };

        CargandoVista = true;
        await Task.Delay(100);

        VistaActual = modulo switch
        {
            "Inicio"    => CrearVistaDashboard(),
            "Lotes"     => CrearVistaLotes(),
            "NuevoLote" => await CrearVistaFormularioLoteAsync(null),
            _ => null
        };

        CargandoVista = false;
    }

    /// <summary>Navega al formulario de edición con un lote específico.</summary>
    public async Task NavegarAEditarLoteAsync(int loteId)
    {
        SidebarActivo = "Lotes";
        ModuloSeleccionado = "Editar Lote";
        IconoModulo = "PackageVariant";
        Breadcrumb = "SILF  ›  Lotes  ›  Editar";

        CargandoVista = true;
        await Task.Delay(100);
        VistaActual = await CrearVistaFormularioLoteAsync(loteId);
        CargandoVista = false;
    }

    // ══════════════════════════════════════════
    // FÁBRICAS DE VISTAS
    // ══════════════════════════════════════════

    private Views.InicioView CrearVistaDashboard()
    {
        var vm = new InicioViewModel
        {
            NavegarCallback = async (m) => await NavegarAsync(m)
        };
        var vista = new Views.InicioView { DataContext = vm };
        _ = vm.CargarDatosCommand.ExecuteAsync(null);
        return vista;
    }

    private Views.LotesView CrearVistaLotes()
    {
        var vm = new LotesViewModel
        {
            NavegarAFormulario = async (loteId) =>
            {
                if (loteId.HasValue)
                    await NavegarAEditarLoteAsync(loteId.Value);
                else
                    await NavegarAsync("NuevoLote");
            }
        };
        var vista = new Views.LotesView { DataContext = vm };
        _ = vm.CargarDatosCommand.ExecuteAsync(null);
        return vista;
    }

    private async Task<Views.LoteFormView> CrearVistaFormularioLoteAsync(int? loteId)
    {
        var vm = new LoteFormViewModel
        {
            OnGuardado = async () => await NavegarAsync("Lotes"),
            OnCancelado = async () => await NavegarAsync("Lotes")
        };

        await vm.CargarCatalogosAsync();
        if (loteId.HasValue)
            await vm.CargarLoteParaEditarAsync(loteId.Value);

        return new Views.LoteFormView { DataContext = vm };
    }

    [RelayCommand]
    private void CerrarSesion()
    {
        _sesion.CerrarSesion();
        var login = new Views.LoginView();
        login.Show();
        Application.Current.MainWindow?.Close();
        Application.Current.MainWindow = login;
    }
}
