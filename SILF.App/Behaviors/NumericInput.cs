// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Behaviors\NumericInput.cs
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SILF.App.Behaviors;

/// <summary>
/// Attached property para inputs numéricos del sistema SILF.
///
/// Aplica tres comportamientos a un TextBox:
/// 1. Al recibir foco (click, tab o programático), selecciona TODO el contenido,
///    permitiendo reescribir sin tener que borrar nada.
///    La selección se difiere con Dispatcher.BeginInvoke para evitar que el
///    posicionamiento del caret del click la sobrescriba (bug clásico de WPF).
/// 2. Permite hasta 4 decimales como punto decimal (acepta tanto . como ,).
/// 3. Bloquea letras y símbolos no numéricos.
///
/// Uso en XAML:
///
///     xmlns:b="clr-namespace:SILF.App.Behaviors"
///
///     &lt;TextBox b:NumericInput.IsEnabled="True"
///              Text="{Binding TipoCambioGeneral, StringFormat=\{0:0.####\}, ConverterCulture=en-US, UpdateSourceTrigger=LostFocus}"/&gt;
/// </summary>
public static class NumericInput
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(NumericInput),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;

        if ((bool)e.NewValue)
        {
            tb.GotFocus += OnGotFocus;
            tb.GotKeyboardFocus += OnGotKeyboardFocus;
            tb.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            tb.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(tb, OnPaste);
        }
        else
        {
            tb.GotFocus -= OnGotFocus;
            tb.GotKeyboardFocus -= OnGotKeyboardFocus;
            tb.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            tb.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(tb, OnPaste);
        }
    }

    // ── Selección automática al tomar foco ──
    //
    // El SelectAll se difiere con BeginInvoke en prioridad Input para que se
    // ejecute DESPUÉS de que MaterialDesign / WPF posicione el caret en el
    // punto donde el usuario hizo click. Sin esto, el click "pisa" la selección
    // y solo quedaría seleccionada la parte donde clickeó el usuario, que es
    // exactamente el bug "solo borra los enteros y los decimales se quedan".

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb) DeferirSelectAll(tb);
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox tb) DeferirSelectAll(tb);
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            tb.Focus();
            DeferirSelectAll(tb);
        }
    }

    /// <summary>
    /// Encola SelectAll en el dispatcher para que se ejecute después de cualquier
    /// otro código que esté procesando el focus o el click. Esto es lo que arregla
    /// que la selección "se pierda" inmediatamente después de disparar GotFocus.
    /// </summary>
    private static void DeferirSelectAll(TextBox tb)
    {
        tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()),
            DispatcherPriority.Input);
    }

    // ── Validación de entrada: solo dígitos, punto/coma decimal y signo opcional ──

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var propuesto = ConstruirTextoPropuesto(tb, e.Text);
        if (!EsNumeroValido(propuesto)) e.Handled = true;
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox tb) { e.CancelCommand(); return; }

        if (!e.DataObject.GetDataPresent(typeof(string))) { e.CancelCommand(); return; }

        var pegado = (string)e.DataObject.GetData(typeof(string))!;
        var propuesto = ConstruirTextoPropuesto(tb, pegado);
        if (!EsNumeroValido(propuesto)) e.CancelCommand();
    }

    private static string ConstruirTextoPropuesto(TextBox tb, string entrada)
    {
        var actual = tb.Text ?? string.Empty;
        var start = tb.SelectionStart;
        var len = tb.SelectionLength;
        return actual.Substring(0, start) + entrada + actual.Substring(start + len);
    }

    private static bool EsNumeroValido(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return true; // permitir vacío para borrar
        // Aceptar tanto coma como punto como separador decimal
        var normalizado = texto.Replace(',', '.');
        // Reconocer: opcional signo, dígitos, opcional un solo punto, máx 4 decimales
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                normalizado, @"^-?\d*\.?\d{0,4}$")) return false;

        // Si solo tiene "-" o "." aún, lo aceptamos como tránsito
        if (normalizado is "-" or "." or "-.") return true;

        // Parseo final para descartar cosas como "."
        return decimal.TryParse(normalizado, NumberStyles.Number,
            CultureInfo.InvariantCulture, out _);
    }
}
