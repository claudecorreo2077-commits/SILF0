// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\ProveedoresView.xaml.cs
using System.Windows.Controls;
using System.Windows.Input;

namespace SILF.App.Views;

public partial class ProveedoresView : UserControl
{
    public ProveedoresView()
    {
        InitializeComponent();
    }

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.ProveedoresViewModel vm)
            vm.CancelarDialogoCommand.Execute(null);
    }
}
