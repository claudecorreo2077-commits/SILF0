// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Converters\RequiredFieldConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SILF.App.Converters;

/// <summary>
/// Devuelve un borde naranja cuando el campo está vacío (texto vacío o número en 0),
/// y DependencyProperty.UnsetValue cuando tiene dato (deja el borde por defecto del control).
/// Se usa enlazando BorderBrush al mismo valor del campo.
/// </summary>
public class RequiredFieldConverter : IValueConverter
{
    private static readonly Brush Faltante = CrearBrush();

    private static Brush CrearBrush()
    {
        var b = new SolidColorBrush(Color.FromRgb(0xFB, 0x8C, 0x00)); // naranja
        b.Freeze();
        return b;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool vacio = value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            decimal d => d == 0m,
            double db => db == 0d,
            int i => i == 0,
            _ => false
        };
        return vacio ? Faltante : DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
