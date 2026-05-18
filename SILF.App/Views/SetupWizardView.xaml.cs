// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\SetupWizardView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SILF.App.ViewModels;

namespace SILF.App.Views;

public partial class SetupWizardView : Window
{
    private bool _sincronizando;

    public SetupWizardView()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── PasswordBox → ViewModel (modo oculto) ──
    private void PwdBox1_Changed(object sender, RoutedEventArgs e)
    {
        if (_sincronizando) return;
        if (DataContext is SetupWizardViewModel vm)
        {
            _sincronizando = true;
            vm.AdminPassword = PwdBox1.Password;
            vm.PasswordTextoVisible = PwdBox1.Password;
            _sincronizando = false;
        }
    }

    private void PwdBox2_Changed(object sender, RoutedEventArgs e)
    {
        if (_sincronizando) return;
        if (DataContext is SetupWizardViewModel vm)
        {
            _sincronizando = true;
            vm.AdminConfirmarPassword = PwdBox2.Password;
            vm.ConfirmarPasswordTextoVisible = PwdBox2.Password;
            _sincronizando = false;
        }
    }

    // ── TextBox → ViewModel (modo visible) ──
    private void PwdVisible1_Changed(object sender, TextChangedEventArgs e)
    {
        if (_sincronizando) return;
        if (DataContext is SetupWizardViewModel vm && sender is TextBox tb)
        {
            _sincronizando = true;
            vm.AdminPassword = tb.Text;
            PwdBox1.Password = tb.Text;
            _sincronizando = false;
        }
    }

    private void PwdVisible2_Changed(object sender, TextChangedEventArgs e)
    {
        if (_sincronizando) return;
        if (DataContext is SetupWizardViewModel vm && sender is TextBox tb)
        {
            _sincronizando = true;
            vm.AdminConfirmarPassword = tb.Text;
            PwdBox2.Password = tb.Text;
            _sincronizando = false;
        }
    }
}
