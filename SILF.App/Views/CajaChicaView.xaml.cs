// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Views\CajaChicaView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SILF.App.Views;

public partial class CajaChicaView : UserControl
{
    public CajaChicaView()
    {
        InitializeComponent();
    }

    // Select-all on focus para todos los TextBox
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (e.NewFocus is TextBox tb && !tb.IsReadOnly)
        {
            tb.SelectAll();
            e.Handled = true;
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (e.OriginalSource is FrameworkElement fe)
        {
            var tb = FindParent<TextBox>(fe);
            if (tb != null && !tb.IsKeyboardFocusWithin && !tb.IsReadOnly)
            {
                tb.Focus();
                tb.SelectAll();
                e.Handled = true;
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T t) return t;
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
