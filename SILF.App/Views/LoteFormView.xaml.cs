// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\LoteFormView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using SILF.App.ViewModels;

namespace SILF.App.Views;

public partial class LoteFormView : UserControl
{
    public LoteFormView()
    {
        InitializeComponent();
    }

    private void TxtCiNit_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is LoteFormViewModel vm)
            vm.BuscarProveedorCommand.Execute(null);
    }

    private void TxtCiNit_GotFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoteFormViewModel vm)
            vm.MostrarTodosProveedoresCommand.Execute(null);
    }

    private void NumericField_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.Dispatcher.BeginInvoke(() => tb.SelectAll(),
                System.Windows.Threading.DispatcherPriority.Input);
    }
}
