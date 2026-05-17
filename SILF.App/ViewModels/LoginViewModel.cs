// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LoginViewModel.cs
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly SesionService _sesion;

    public LoginViewModel(SesionService sesion)
    {
        _sesion = sesion;
        Titulo = "Iniciar Sesión";
    }

    [ObservableProperty] private string _nombreUsuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _tieneError;

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        TieneError = false;
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreUsuario) || string.IsNullOrWhiteSpace(Password))
        {
            MensajeError = "Ingrese usuario y contraseña.";
            TieneError = true;
            return;
        }

        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();
            var hash = ComputeSha256(Password);
            var usuario = await db.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == NombreUsuario && u.PasswordHash == hash && u.Activo);

            if (usuario == null)
            {
                MensajeError = "Usuario o contraseña incorrectos.";
                TieneError = true;
                return;
            }

            _sesion.IniciarSesion(usuario);

            // ── Transición con fade-out del login ──
            var loginWindow = Application.Current.MainWindow;
            var mainWindow = new Views.MainWindow();
            mainWindow.Opacity = 0;
            mainWindow.Show();

            // Fade-in del main
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            mainWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Fade-out del login
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            fadeOut.Completed += (_, _) =>
            {
                Application.Current.MainWindow = mainWindow;
                loginWindow?.Close();
            };
            loginWindow?.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error: {ex.Message}";
            TieneError = true;
        }
        finally
        {
            Cargando = false;
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
