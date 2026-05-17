namespace SILF.Core.Enums;

/// <summary>
/// Estados del ciclo de vida de un lote de mineral.
/// El flujo es secuencial: cada estado habilita el siguiente.
/// </summary>
public enum EstadoLote
{
    /// <summary>Lote registrado con datos de peso, proveedor, chofer. Esperando anticipo o laboratorio.</summary>
    Registrado = 0,

    /// <summary>Se pagó el anticipo al proveedor. Esperando envío a laboratorio.</summary>
    AnticipoPagado = 1,

    /// <summary>Muestra enviada al laboratorio. Esperando resultados (días).</summary>
    EnLaboratorio = 2,

    /// <summary>Leyes de mineral recibidas del laboratorio (ZN, AG, PB).</summary>
    LeyesRegistradas = 3,

    /// <summary>Liquidación calculada. Saldo determinado, listo para pago final.</summary>
    Liquidado = 4,

    /// <summary>Saldo pagado y lote cerrado. Registra fecha/hora de cierre.</summary>
    Completado = 5
}
