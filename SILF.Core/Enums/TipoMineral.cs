namespace SILF.Core.Enums;

/// <summary>
/// Clasificación del lote según la cantidad de minerales presentes.
/// Se determina automáticamente al registrar las leyes del laboratorio.
/// </summary>
public enum TipoMineral
{
    /// <summary>Lote con un solo mineral (solo ZN, solo AG, o solo PB).</summary>
    Brosa = 0,

    /// <summary>Lote con 2 o 3 minerales (combinación de ZN, AG, PB).</summary>
    Complejo = 1
}
