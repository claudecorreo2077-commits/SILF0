// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\MainWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using SILF.App.ViewModels;

namespace SILF.App.Views;

public partial class MainWindow : Window
{
    private bool _sidebarExpanded = true;
    private bool _isDarkMode = false; // CLARO por defecto

    // URI del diccionario de paleta oscura. Se carga/descarga dinámicamente.
    private static readonly Uri DarkPaletteUri =
        new("/Themes/SilfPalette.Dark.xaml", UriKind.Relative);

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

        Loaded += (_, _) => ApplyTheme();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        { if (e.ClickCount == 2) ToggleMaximize(); else DragMove(); }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMaximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        { WindowState = WindowState.Normal; BtnMax.Content = "☐"; }
        else
        { WindowState = WindowState.Maximized; BtnMax.Content = "❐"; }
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        NavPanel.BeginAnimation(WidthProperty, new DoubleAnimation
        {
            To = _sidebarExpanded ? 230 : 65,
            Duration = new Duration(TimeSpan.FromMilliseconds(_sidebarExpanded ? 250 : 200)),
            EasingFunction = new CubicEase { EasingMode = _sidebarExpanded ? EasingMode.EaseOut : EasingMode.EaseIn }
        });
        if (BtnToggle.Template.FindName("chevron", BtnToggle) is PackIcon icon)
            icon.Kind = _sidebarExpanded ? PackIconKind.ChevronLeft : PackIconKind.ChevronRight;

        bool collapsed = !_sidebarExpanded;
        foreach (var rb in FindVisualChildren<RadioButton>(NavPanel))
            ToolTipService.SetIsEnabled(rb, collapsed);
        ToolTipService.SetIsEnabled(UserPanel, collapsed);
        ToolTipService.SetIsEnabled(BtnTheme, collapsed);
    }

    private void BtnTheme_Click(object sender, RoutedEventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme();
    }

    /// <summary>
    /// Aplica el tema actual. Hace dos cosas:
    ///   1. Cambia el BaseTheme de MaterialDesign (afecta DataGrid, ComboBox, etc).
    ///   2. Sobrescribe los tokens SILF cargando/descargando SilfPalette.Dark.xaml
    ///      en Application.Resources.MergedDictionaries.
    /// </summary>
    private void ApplyTheme()
    {
        // ── 1. MaterialDesign theme ──
        var ph = new PaletteHelper();
        var t = ph.GetTheme();
        t.SetBaseTheme(_isDarkMode ? BaseTheme.Dark : BaseTheme.Light);
        ph.SetTheme(t);

        // ── 2. Swap de paleta SILF: agregar o quitar SilfPalette.Dark.xaml ──
        SwapPaletaSilf(_isDarkMode);

        // ── 3. Ajustar Background del Window. Alineado a la nueva escala:
        //      el marco/ventana es el nivel más profundo (SilfBgWindow),
        //      apenas por debajo del sidebar, para que el contenido "flote". ──
        if (_isDarkMode)
        {
            // Gradiente sutil entre Window (#101015) y un punto apenas más claro (#16161D)
            WindowBorder.Background = new LinearGradientBrush(
                Color.FromRgb(0x10, 0x10, 0x15),   // #101015  (SilfBgWindow)
                Color.FromRgb(0x16, 0x16, 0x1D),   // #16161D  (= SilfBgSidebar)
                new Point(0, 0), new Point(1, 1));
        }
        else
        {
            // Modo claro: fondo sólido (lo que ya tenías)
            WindowBorder.Background = B("#F0F2F8");
        }

        // ── 4. Actualizar el icono y texto del botón de tema ──
        var themeIcon = BtnTheme.Template.FindName("ThemeIcon", BtnTheme) as PackIcon;
        var themeText = BtnTheme.Template.FindName("ThemeText", BtnTheme) as TextBlock;

        if (_isDarkMode)
        {
            if (themeIcon != null) { themeIcon.Kind = PackIconKind.WeatherNight; themeIcon.Foreground = B("#FFD54F"); }
            if (themeText != null) themeText.Text = "Oscuro";
        }
        else
        {
            if (themeIcon != null) { themeIcon.Kind = PackIconKind.WhiteBalanceSunny; themeIcon.Foreground = B("#FF9800"); }
            if (themeText != null) themeText.Text = "Claro";
        }
    }

    /// <summary>
    /// Carga/descarga SilfPalette.Dark.xaml en Application.Resources.MergedDictionaries.
    /// Cuando está cargado, sobreescribe las claves de SilfTokens.xaml con los
    /// valores oscuros. Al descargarlo, vuelven a aplicarse los valores claros.
    /// </summary>
    private static void SwapPaletaSilf(bool dark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;

        // Buscar si ya está cargado el dark palette
        ResourceDictionary? darkDict = null;
        foreach (var d in dicts)
        {
            if (d.Source != null && d.Source.OriginalString.EndsWith(
                "SilfPalette.Dark.xaml", StringComparison.OrdinalIgnoreCase))
            {
                darkDict = d;
                break;
            }
        }

        if (dark && darkDict == null)
        {
            // Activar oscuro: agregar el diccionario
            dicts.Add(new ResourceDictionary { Source = DarkPaletteUri });
        }
        else if (!dark && darkDict != null)
        {
            // Volver a claro: quitar el diccionario (los tokens vuelven al default)
            dicts.Remove(darkDict);
        }
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var gc in FindVisualChildren<T>(child)) yield return gc;
        }
    }
}
