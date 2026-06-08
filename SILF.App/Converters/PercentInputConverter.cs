// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\Converters\PercentInputConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace SILF.App.Converters;

/// <summary>
/// Muestra un valor almacenado como fracción (0.53) en forma de porcentaje (53)
/// y, al escribir, vuelve a guardarlo como fracción (53 -> 0.53).
/// El motor de cálculo y lo que se persiste siguen usando la fracción: solo
/// cambia lo que el usuario ve y teclea.
/// </summary>
public class PercentInputConverter : IValueConverter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return (d * 100m).ToString("0.####", Inv);
        if (value is double db)
            return (db * 100d).ToString("0.####", Inv);
        return "0";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.Trim().Replace(',', '.');
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        if (decimal.TryParse(s, NumberStyles.Any, Inv, out var pct))
            return pct / 100m;
        return 0m;
    }
}
