// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\AgregarItemDialog.xaml.cs
using System.Windows;

namespace SILF.App.Views;

public partial class AgregarItemDialog : Window
{
    /// <summary>El nombre ingresado por el usuario.</summary>
    public string NombreIngresado { get; private set; } = "";

    public AgregarItemDialog(string titulo)
    {
        InitializeComponent();
        TxtTitulo.Text = titulo;
        Title = titulo;
        TxtNombre.Focus();
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var nombre = TxtNombre.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            TxtNombre.BorderBrush = System.Windows.Media.Brushes.Red;
            return;
        }

        NombreIngresado = nombre.ToUpper();
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
