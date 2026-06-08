// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\LoteResumen.cs
using System.Windows.Media;
using SILF.Core.Enums;

namespace SILF.App.ViewModels;

/// <summary>
/// DTO para mostrar lotes en el DataGrid del Dashboard.
/// Incluye propiedades calculadas para el badge de estado con color.
/// </summary>
public class LoteResumen
{
    public int Id { get; set; }
    public int NumeroLote { get; set; }
    public string NombreProveedor { get; set; } = "";
    public string NombreMina { get; set; } = "";
    public decimal PesoNeto { get; set; }
    public EstadoLote Estado { get; set; }
    public TipoMineral? TipoMineral { get; set; }
    public DateTime FechaRegistro { get; set; }

    public string TipoMineralTexto => TipoMineral?.ToString() ?? "—";

    public string EstadoTexto => Estado switch
    {
        EstadoLote.Registrado       => "Registrado",
        EstadoLote.AnticipoPagado   => "Anticipo",
        EstadoLote.EnLaboratorio    => "Laboratorio",
        EstadoLote.LeyesRegistradas => "Leyes OK",
        EstadoLote.Liquidado        => "Liquidado",
        EstadoLote.Completado       => "Completado",
        _ => Estado.ToString()
    };

    public SolidColorBrush EstadoColor => new(Estado switch
    {
        EstadoLote.Registrado       => (Color)ColorConverter.ConvertFromString("#78909C"),
        EstadoLote.AnticipoPagado   => (Color)ColorConverter.ConvertFromString("#42A5F5"),
        EstadoLote.EnLaboratorio    => (Color)ColorConverter.ConvertFromString("#FFA726"),
        EstadoLote.LeyesRegistradas => (Color)ColorConverter.ConvertFromString("#AB47BC"),
        EstadoLote.Liquidado        => (Color)ColorConverter.ConvertFromString("#26A69A"),
        EstadoLote.Completado       => (Color)ColorConverter.ConvertFromString("#66BB6A"),
        _ => (Color)ColorConverter.ConvertFromString("#78909C")
    });
}
