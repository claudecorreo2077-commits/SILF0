// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Converters\WizardConverters.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SILF.App.Converters;

/// <summary>Visible si el paso actual coincide con el/los indicados en el parámetro ("1" o "2,3").</summary>
public class StepVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int paso && parameter is string p)
        {
            foreach (var token in p.Split(','))
                if (int.TryParse(token.Trim(), out var n) && n == paso)
                    return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

/// <summary>Opacidad para el indicador de pasos: 1.0 si es el paso activo, 0.4 si no.</summary>
public class StepActiveOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int paso && parameter is string p && int.TryParse(p, out var n))
            return paso >= n ? 1.0 : 0.4;   // pasos ya alcanzados quedan iluminados
        return 0.4;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}
