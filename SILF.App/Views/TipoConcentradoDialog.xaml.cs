// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\TipoConcentradoDialog.xaml.cs
using System.Windows;
using SILF.Core.Enums;

namespace SILF.App.Views;

public partial class TipoConcentradoDialog : Window
{
    public TipoConcentrado? TipoElegido { get; private set; }

    public TipoConcentradoDialog()
    {
        InitializeComponent();
    }

    private void BtnZnAg_Click(object sender, RoutedEventArgs e)
    {
        TipoElegido = TipoConcentrado.ZnAg;
        DialogResult = true;
        Close();
    }

    private void BtnAg_Click(object sender, RoutedEventArgs e)
    {
        TipoElegido = TipoConcentrado.Ag;
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
