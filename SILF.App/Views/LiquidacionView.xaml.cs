// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\LiquidacionView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SILF.App.Views;

public partial class LiquidacionView : UserControl
{
    public LiquidacionView()
    {
        InitializeComponent();

        // Seleccionar todo al hacer foco en cualquier TextBox
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
}
