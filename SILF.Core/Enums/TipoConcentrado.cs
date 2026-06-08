namespace SILF.Core.Enums;

/// <summary>
/// Tipo de concentrado a liquidar. Cada tipo usa una fórmula de compra-venta
/// de concentrados distinta (metales pagables, maquila, refinación, penalidades
/// y fletes propios). Se elige al entrar al módulo Concentrados.
/// </summary>
public enum TipoConcentrado
{
    /// <summary>Concentrado de Zinc-Plata. Paga Zn y Ag. Sin refinación.</summary>
    ZnAg = 0,

    /// <summary>Concentrado de Plata. Paga Ag y Pb. Con refinación de Ag.</summary>
    Ag = 1
}
