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
        ModuloSeleccionado = "Inicio"; SidebarActivo = "Inicio";
        IconoModulo = "Home"; Breadcrumb = "Inicio";
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
    [ObservableProperty] private string _sidebarActivo = "Inicio";

    [RelayCommand]
    public async Task NavegarAsync(string modulo)
    {
        SidebarActivo = modulo switch
        {
            "NuevoLote" or "EditarLote" => "Lotes",
            "LiquidarLote" or "VerLiquidacion" => "Liquidación",
            _ => modulo
        };
        ModuloSeleccionado = modulo switch
        {
            "NuevoLote" => "Nuevo Lote", "EditarLote" => "Editar Lote",
            "LiquidarLote" => "Liquidar Lote", "VerLiquidacion" => "Ver Liquidación",
            _ => modulo
        };
        IconoModulo = modulo switch
        {
            "Inicio" => "Home",
            "Lotes" or "NuevoLote" or "EditarLote" => "PackageVariant",
            "Liquidación" or "LiquidarLote" or "VerLiquidacion" => "Calculator",
            "Flotación" => "Flask",
            "Caja Chica" => "CashRegister", "Reportes" => "ChartBar",
            "Configuración" => "Cog", "Usuarios" => "AccountGroup",
            "Catálogos" => "BookOpenVariant", _ => "Home"
        };
        Breadcrumb = modulo switch
        {
            "Inicio" => "Inicio", "NuevoLote" => "SILF  ›  Lotes  ›  Nuevo Lote",
            "EditarLote" => "SILF  ›  Lotes  ›  Editar",
            "LiquidarLote" => "SILF  ›  Liquidación  ›  Liquidar",
            "VerLiquidacion" => "SILF  ›  Liquidación  ›  Detalle",
            _ => $"SILF  ›  {ModuloSeleccionado}"
        };

        CargandoVista = true; await Task.Delay(100);
        VistaActual = modulo switch
        {
            "Inicio"        => CrearVistaDashboard(),
            "Lotes"         => CrearVistaLotes(),
            "NuevoLote"     => await CrearVistaFormularioLoteAsync(null),
            "Liquidación"   => await CrearVistaLiquidacionListAsync(),
            "Flotación"     => await CrearVistaFlotacionAsync(),
            "Caja Chica"    => await CrearVistaCajaChicaAsync(),
            "Configuración" => await CrearVistaEmpresaAsync(),
            "Usuarios"      => await CrearVistaUsuariosAsync(),
            "Catálogos"     => await CrearVistaCatalogosAsync(),
            _ => null
        };
        CargandoVista = false;
    }

    public async Task NavegarAEditarLoteAsync(int loteId)
    {
        SidebarActivo = "Lotes"; ModuloSeleccionado = "Editar Lote";
        IconoModulo = "PackageVariant"; Breadcrumb = "SILF  ›  Lotes  ›  Editar";
        CargandoVista = true; await Task.Delay(100);
        VistaActual = await CrearVistaFormularioLoteAsync(loteId);
        CargandoVista = false;
    }

    public async Task NavegarALiquidarAsync(int loteId)
    {
        SidebarActivo = "Liquidación"; ModuloSeleccionado = "Liquidar Lote";
        IconoModulo = "Calculator"; Breadcrumb = "SILF  ›  Liquidación  ›  Liquidar";
        CargandoVista = true; await Task.Delay(100);
        VistaActual = await CrearVistaLiquidacionDetalleAsync(loteId);
        CargandoVista = false;
    }

    public async Task NavegarAReciboPreviewAsync(int reciboId)
    {
        SidebarActivo = "Caja Chica"; ModuloSeleccionado = "Recibo";
        IconoModulo = "Receipt"; Breadcrumb = "SILF  ›  Caja Chica  ›  Recibo";
        CargandoVista = true; await Task.Delay(100);
        var vm = new ReciboPreviewViewModel
        {
            OnVolver = async () => await NavegarAsync("Caja Chica")
        };
        await vm.CargarReciboAsync(reciboId);
        VistaActual = new Views.ReciboPreviewView { DataContext = vm };
        CargandoVista = false;
    }

    // ══════════════════════════════════════════
    // FÁBRICAS DE VISTAS
    // ══════════════════════════════════════════

    private Views.InicioView CrearVistaDashboard()
    {
        var vm = new InicioViewModel { NavegarCallback = async (m) => await NavegarAsync(m) };
        var vista = new Views.InicioView { DataContext = vm };
        _ = vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private Views.LotesView CrearVistaLotes()
    {
        var vm = new LotesViewModel { NavegarAFormulario = async (id) => { if (id.HasValue) await NavegarAEditarLoteAsync(id.Value); else await NavegarAsync("NuevoLote"); } };
        var vista = new Views.LotesView { DataContext = vm };
        _ = vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private async Task<Views.LoteFormView> CrearVistaFormularioLoteAsync(int? loteId)
    {
        var vm = new LoteFormViewModel { OnGuardado = async () => await NavegarAsync("Lotes"), OnCancelado = async () => await NavegarAsync("Lotes") };
        await vm.CargarCatalogosAsync();
        if (loteId.HasValue) await vm.CargarLoteParaEditarAsync(loteId.Value);
        return new Views.LoteFormView { DataContext = vm };
    }

    private async Task<Views.LiquidacionListView> CrearVistaLiquidacionListAsync()
    {
        var vm = new LiquidacionListViewModel { NavegarALiquidar = async (id) => await NavegarALiquidarAsync(id), NavegarAVer = async (id) => await NavegarALiquidarAsync(id) };
        var vista = new Views.LiquidacionListView { DataContext = vm };
        await vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private async Task<Views.LiquidacionView> CrearVistaLiquidacionDetalleAsync(int loteId)
    {
        var vm = new LiquidacionViewModel { OnGuardado = async () => await NavegarAsync("Liquidación"), OnCancelado = async () => await NavegarAsync("Liquidación") };
        await vm.CargarLoteAsync(loteId);
        return new Views.LiquidacionView { DataContext = vm };
    }

    private async Task<Views.FlotacionView> CrearVistaFlotacionAsync()
    {
        var vm = new FlotacionViewModel();
        var vista = new Views.FlotacionView { DataContext = vm };
        await vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private async Task<Views.CajaChicaView> CrearVistaCajaChicaAsync()
    {
        var vm = new CajaChicaViewModel(_sesion.EsAdmin)
        {
            NavegarARecibo = async (reciboId) => await NavegarAReciboPreviewAsync(reciboId)
        };
        var vista = new Views.CajaChicaView { DataContext = vm };
        await vm.CargarDatosCommand.ExecuteAsync(null);
        return vista;
    }

    private async Task<Views.EmpresaView> CrearVistaEmpresaAsync()
    {
        var vm = new EmpresaViewModel();
        var vista = new Views.EmpresaView { DataContext = vm };
        await vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private async Task<Views.UsuariosView> CrearVistaUsuariosAsync()
    {
        var id = _sesion.UsuarioActual?.Id ?? 0;
        var vm = new UsuariosViewModel(_sesion.EsAdmin, id);
        var vista = new Views.UsuariosView { DataContext = vm };
        await vm.CargarDatosCommand.ExecuteAsync(null); return vista;
    }

    private async Task<Views.CatalogosView> CrearVistaCatalogosAsync()
    {
        var vm = new CatalogosViewModel();
        var vista = new Views.CatalogosView { DataContext = vm };
        await vm.CargarTodoCommand.ExecuteAsync(null); return vista;
    }

    [RelayCommand]
    private void CerrarSesion()
    {
        _sesion.CerrarSesion();
        var login = new Views.LoginView(); login.Show();
        Application.Current.MainWindow?.Close();
        Application.Current.MainWindow = login;
    }
}
