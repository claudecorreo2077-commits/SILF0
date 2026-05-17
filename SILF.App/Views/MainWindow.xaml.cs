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
    private bool _isDarkMode = true;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
        MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
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

    private void ApplyTheme()
    {
        var ph = new PaletteHelper();
        var t = ph.GetTheme();
        t.SetBaseTheme(_isDarkMode ? BaseTheme.Dark : BaseTheme.Light);
        ph.SetTheme(t);

        var themeIcon = BtnTheme.Template.FindName("ThemeIcon", BtnTheme) as PackIcon;
        var themeText = BtnTheme.Template.FindName("ThemeText", BtnTheme) as TextBlock;

        if (_isDarkMode)
        {
            WindowBorder.Background = new LinearGradientBrush(
                Color.FromRgb(6, 5, 49), Color.FromRgb(27, 20, 72),
                new Point(0, 1), new Point(1, 0));
            WindowBorder.BorderBrush = B("#2A2060");
            NavPanel.Background = B("#0C0A28");
            ContentArea.Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
            TxtTitleSilf.Foreground = Brushes.White;
            TxtModulo.Foreground = Brushes.White;

            Resources["MenuHoverBrush"]  = new SolidColorBrush(Color.FromArgb(0x18, 255, 255, 255));
            Resources["MenuSelectedBg"]  = B("#F0F0F0");
            Resources["MenuSelectedFg"]  = B("#1B1448");
            Resources["MenuForeground"]  = B("#90A4AE");
            Resources["MenuSeparator"]   = new SolidColorBrush(Color.FromArgb(0x15, 255, 255, 255));
            Resources["ExtensionBg"]     = B("#161240");
            Resources["ExtensionFg"]     = Brushes.White;
            Resources["SidebarTitle"]    = Brushes.White;
            Resources["InputBg"]         = new SolidColorBrush(Color.FromArgb(0x10, 255, 255, 255));
            Resources["InputBorder"]     = new SolidColorBrush(Color.FromArgb(0x30, 255, 255, 255));
            Resources["InputFg"]         = Brushes.White;
            Resources["PanelBorder"]     = new SolidColorBrush(Color.FromArgb(0x15, 255, 255, 255));
            Resources["CardBg"]          = new SolidColorBrush(Color.FromArgb(0x0A, 255, 255, 255));

            if (themeIcon != null) { themeIcon.Kind = PackIconKind.WeatherNight; themeIcon.Foreground = B("#FFD54F"); }
            if (themeText != null) themeText.Text = "Oscuro";
        }
        else
        {
            WindowBorder.Background = B("#F0F2F8");
            WindowBorder.BorderBrush = B("#C8CDD7");
            NavPanel.Background = B("#FFFFFF");
            ContentArea.Background = B("#F8F9FC");
            TxtTitleSilf.Foreground = B("#1E1E3E");
            TxtModulo.Foreground = B("#1E1E3E");

            Resources["MenuHoverBrush"]  = B("#E8EAF0");
            Resources["MenuSelectedBg"]  = B("#462AD8");
            Resources["MenuSelectedFg"]  = Brushes.White;
            Resources["MenuForeground"]  = B("#5A5E72");
            Resources["MenuSeparator"]   = B("#E0E2E8");
            Resources["ExtensionBg"]     = B("#FFFFFF");
            Resources["ExtensionFg"]     = B("#1E1E3E");
            Resources["SidebarTitle"]    = B("#1E1E3E");
            Resources["InputBg"]         = B("#F5F6FA");
            Resources["InputBorder"]     = B("#C8CDD7");
            Resources["InputFg"]         = B("#1E1E3E");
            Resources["PanelBorder"]     = B("#D8DCE6");
            Resources["CardBg"]          = Brushes.White;

            if (themeIcon != null) { themeIcon.Kind = PackIconKind.WhiteBalanceSunny; themeIcon.Foreground = B("#FF9800"); }
            if (themeText != null) themeText.Text = "Claro";
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


