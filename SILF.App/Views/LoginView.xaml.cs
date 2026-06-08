// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\LoginView.xaml.cs
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SILF.App.ViewModels;

namespace SILF.App.Views;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<LoginViewModel>();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm
            && vm.IniciarSesionCommand.CanExecute(null))
        {
            vm.IniciarSesionCommand.Execute(null);
        }
    }
}
