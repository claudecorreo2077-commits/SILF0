using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SILF.Core.Enums;

namespace SILF.Core.Models;

/// <summary>
/// Lote de mineral. Entidad central del sistema.
/// Cada lote pasa por 6 estados: Registrado → AnticipoPagado → EnLaboratorio → 
/// LeyesRegistradas → Liquidado → Completado.
/// Cada lote genera 1 Liquidación y 1 Flotación (simultáneas).
/// </summary>
public class Lote
{
    public int Id { get; set; }

    /// <summary>Número de ticket de ingreso.</summary>
    [MaxLength(50)]
    public string? Ticket { get; set; }

    /// <summary>Número de lote correlativo. Se resetea cada 70 toneladas.</summary>
    public int NumeroLote { get; set; }

    /// <summary>Proceso: COMPLEJO o BROSA. Se auto-clasifica al registrar leyes.</summary>
    public TipoMineral? TipoMineral { get; set; }

    /// <summary>Estado actual del lote en la máquina de estados.</summary>
    public EstadoLote Estado { get; set; } = EstadoLote.Registrado;

    // ── Pesos ──
    [Column(TypeName = "decimal(12,2)")]
    public decimal PesoBruto { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Tara { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal PesoNeto { get; set; }

    // ── Transporte ──
    [MaxLength(200)]
    public string? NombreChofer { get; set; }

    [MaxLength(50)]
    public string? CiChofer { get; set; }

    [MaxLength(20)]
    public string? Placa { get; set; }

    // ── Leyes del laboratorio (se llenan después) ──
    /// <summary>Ley de Zinc en porcentaje.</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal? LeyZn { get; set; }

    /// <summary>Ley de Plata en onzas troy/tonelada corta.</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal? LeyAg { get; set; }

    /// <summary>Ley de Plomo en porcentaje.</summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal? LeyPb { get; set; }

    // ── Control ──
    /// <summary>Si es false, no se considera en reportes ni listados.</summary>
    public bool Visible { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public DateTime? FechaLaboratorio { get; set; }

    public DateTime? FechaLiquidacion { get; set; }

    public DateTime? FechaCompletado { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    // ── FKs ──
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int MinaId { get; set; }
    public Mina Mina { get; set; } = null!;

    // ── Navegación 1:1 ──
    public Liquidacion? Liquidacion { get; set; }
    public Flotacion? Flotacion { get; set; }
    public Pago? Pago { get; set; }
    public BonoTransporte? BonoTransporte { get; set; }
}
