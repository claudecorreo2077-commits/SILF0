// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\LoteFormView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SILF.App.Views;

public partial class LoteFormView : UserControl
{
    public LoteFormView()
    {
        InitializeComponent();

        // Select-all al hacer foco en cualquier TextBox
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnTextBoxGotFocus));

        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnTextBoxPreviewMouseDown));
    }

    private void OnTextBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox tb) tb.SelectAll();
    }

    private void OnTextBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
        {
            tb.Focus();
            e.Handled = true;
        }
    }

    // Mantener por compatibilidad con GotFocus="NumericField_GotFocus" en XAML
    private void NumericField_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb) tb.SelectAll();
    }

    // Proveedor: buscar al escribir
    private void TxtCiNit_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is SILF.App.ViewModels.LoteFormViewModel vm)
            vm.BuscarProveedorCommand.Execute(null);
    }

    // Proveedor: mostrar todos al hacer foco
    private void TxtCiNit_GotFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is SILF.App.ViewModels.LoteFormViewModel vm)
            vm.MostrarTodosProveedoresCommand.Execute(null);
        if (sender is TextBox tb) tb.SelectAll();
    }
}
