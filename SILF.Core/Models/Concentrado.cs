using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SILF.Core.Enums;

namespace SILF.Core.Models;

/// <summary>
/// Liquidación de un concentrado (módulo Concentrados). Es un cálculo independiente,
/// sin máquina de estados: se calcula y se emite recibo.
///
/// Para reimprimir el recibo idéntico aunque cambien los parámetros por defecto,
/// se guarda una FOTO (snapshot) del input completo en ParametrosJson, y los
/// resultados clave en columnas para listar/buscar sin recalcular.
/// </summary>
public class Concentrado
{
    public int Id { get; set; }

    /// <summary>Correlativo global del concentrado (1, 2, 3...), fijo de por vida.</summary>
    public int NumeroConcentrado { get; set; }

    public TipoConcentrado Tipo { get; set; }

    // ── Cabecera del recibo ──
    [MaxLength(60)]  public string? NumeroLiquidacion { get; set; }   // texto libre, ej "CZN-TMT-002"
    [MaxLength(200)] public string ClienteNombre { get; set; } = "";
    [MaxLength(50)]  public string? ClienteCi { get; set; }
    [MaxLength(120)] public string? Procedencia { get; set; }
    [MaxLength(120)] public string? Municipio { get; set; }
    [MaxLength(120)] public string? ConcesionMinera { get; set; }
    [MaxLength(60)]  public string? MineralTexto { get; set; }        // "ZN-AG", "AG"

    public DateTime FechaEntrega { get; set; } = DateTime.Today;
    public DateTime FechaLiquidacion { get; set; } = DateTime.Today;

    // ── Entradas clave (para listado/búsqueda; el detalle vive en ParametrosJson) ──
    [Column(TypeName = "decimal(12,2)")] public decimal Tmh { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Tms { get; set; }
    [Column(TypeName = "decimal(8,4)")]  public decimal LeyZn { get; set; }
    [Column(TypeName = "decimal(10,4)")] public decimal LeyAg { get; set; }
    [Column(TypeName = "decimal(8,4)")]  public decimal LeyPb { get; set; }

    // ── Resultados clave ──
    [Column(TypeName = "decimal(16,2)")] public decimal ValorFobBs { get; set; }
    [Column(TypeName = "decimal(16,2)")] public decimal TotalRetenciones { get; set; }
    [Column(TypeName = "decimal(16,2)")] public decimal Anticipo { get; set; }
    [Column(TypeName = "decimal(16,2)")] public decimal LiquidoPagableBs { get; set; }
    [Column(TypeName = "decimal(16,2)")] public decimal SaldoPagarBs { get; set; }
    [Column(TypeName = "decimal(16,2)")] public decimal SaldoPagarUs { get; set; }

    /// <summary>Snapshot JSON del ConcentradoInput completo (parámetros usados).</summary>
    public string ParametrosJson { get; set; } = "";

    // ── Control ──
    public bool Visible { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    [MaxLength(500)] public string? Observaciones { get; set; }
}
