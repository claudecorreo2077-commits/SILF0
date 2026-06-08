// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Converters\StringMatchConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace SILF.App.Converters;

/// <summary>
/// Compara el valor del binding con ConverterParameter.
/// Retorna true si son iguales. Se usa para IsChecked del sidebar.
/// </summary>
public class StringMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
