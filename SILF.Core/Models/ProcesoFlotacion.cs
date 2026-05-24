// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Models\ProcesoFlotacion.cs
using System.ComponentModel.DataAnnotations;
using SILF.Core.Enums;

namespace SILF.Core.Models;

/// <summary>
/// Proceso de Flotación. Agrupa lotes que se procesan en un mismo batch.
/// Solo puede haber UN proceso Abierto a la vez. Los lotes nuevos se asignan
/// al proceso abierto, y su NumeroLote se reinicia desde 1 con cada nuevo proceso.
///
/// Al presionar el botón FLOTAR, el proceso actual se cierra (Estado=Cerrado,
/// FechaCierre=Now) y se crea uno nuevo con NumeroProceso = anterior + 1.
/// </summary>
public class ProcesoFlotacion
{
    public int Id { get; set; }

    /// <summary>Número correlativo del proceso. 1, 2, 3...</summary>
    public int NumeroProceso { get; set; }

    /// <summary>Cuándo se abrió el proceso.</summary>
    public DateTime FechaApertura { get; set; } = DateTime.Now;

    /// <summary>Cuándo se cerró (botón FLOTAR). null si está abierto.</summary>
    public DateTime? FechaCierre { get; set; }

    public EstadoProcesoFlotacion Estado { get; set; } = EstadoProcesoFlotacion.Abierto;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Lotes que pertenecen a este proceso.</summary>
    public ICollection<Lote> Lotes { get; set; } = new List<Lote>();
}
