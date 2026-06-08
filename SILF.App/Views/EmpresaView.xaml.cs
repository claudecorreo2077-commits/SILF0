// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\EmpresaView.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace SILF.App.Views;

public partial class EmpresaView : UserControl
{
    public EmpresaView()
    {
        InitializeComponent();
    }

    private void NumericField_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.Dispatcher.BeginInvoke(() => tb.SelectAll(),
                System.Windows.Threading.DispatcherPriority.Input);
    }
}
