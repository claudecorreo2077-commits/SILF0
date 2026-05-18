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

    private const string DefaultPasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Evitar que WPF cierre la app al cerrar el wizard (antes de abrir el login)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

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

            Resources["EstadoRegistrado"]       = B("#78909C");
            Resources["EstadoAnticipoPagado"]    = B("#42A5F5");
            Resources["EstadoEnLaboratorio"]     = B("#FFA726");
            Resources["EstadoLeyesRegistradas"]  = B("#AB47BC");
            Resources["EstadoLiquidado"]         = B("#26A69A");
            Resources["EstadoCompletado"]        = B("#66BB6A");

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

            // ══════════════════════════════════════════
            // WIZARD DE PRIMERA VEZ
            // ══════════════════════════════════════════
            var admin = db.Usuarios.FirstOrDefault(u => u.Id == 1);
            if (admin != null && admin.PasswordHash == DefaultPasswordHash)
            {
                var vm = new SetupWizardViewModel();
                var wizard = new Views.SetupWizardView { DataContext = vm };
                vm.CerrarVentana = () =>
                {
                    wizard.DialogResult = true;
                    wizard.Close();
                };

                var resultado = wizard.ShowDialog();
                if (resultado != true || !vm.Completado)
                {
                    Shutdown();
                    return;
                }
            }

            // ── Login ──
            // Volver al modo normal: cerrar app cuando se cierre la última ventana
            ShutdownMode = ShutdownMode.OnLastWindowClose;
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
