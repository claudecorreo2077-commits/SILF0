// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\App.xaml.cs
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MaterialDesignThemes.Wpf;
using MaterialDesignColors;
using SILF.Data;
using SILF.App.ViewModels;

namespace SILF.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            // ── MaterialDesign ──
            var bundledTheme = new BundledTheme
            {
                BaseTheme = BaseTheme.Light,
                PrimaryColor = PrimaryColor.Indigo,
                SecondaryColor = SecondaryColor.Amber
            };
            Resources.MergedDictionaries.Add(bundledTheme);
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml")
            });

            // ══════════════════════════════════════════
            // COLORES DE ESTADO DE LOTES (globales)
            // Usar: {DynamicResource EstadoRegistrado}
            // ══════════════════════════════════════════
            Resources["EstadoRegistrado"]       = B("#78909C");  // Gris
            Resources["EstadoAnticipoPagado"]    = B("#42A5F5");  // Azul
            Resources["EstadoEnLaboratorio"]     = B("#FFA726");  // Ámbar
            Resources["EstadoLeyesRegistradas"]  = B("#AB47BC");  // Púrpura
            Resources["EstadoLiquidado"]         = B("#26A69A");  // Teal
            Resources["EstadoCompletado"]        = B("#66BB6A");  // Verde

            // ── DI ──
            var services = new ServiceCollection();
            services.AddDbContext<SilfDbContext>(options =>
            {
                var dbPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "silf.db");
                options.UseSqlite($"Data Source={dbPath}");
            });
            services.AddSingleton<SesionService>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            Services = services.BuildServiceProvider();

            // ── BD ──
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            db.Database.EnsureCreated();

            // ── Login ──
            var login = new Views.LoginView();
            MainWindow = login;
            login.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al iniciar SILF:\n\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                "SILF - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Error:\n\n{e.Exception.Message}", "SILF - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
