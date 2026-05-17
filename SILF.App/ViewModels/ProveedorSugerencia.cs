// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ProveedorSugerencia.cs
namespace SILF.App.ViewModels;

/// <summary>
/// DTO para el popup de autocompletado de proveedores.
/// </summary>
public class ProveedorSugerencia
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string CiNit { get; set; } = "";
    public string CooperativaNombre { get; set; } = "";
    public int? CooperativaId { get; set; }
}
