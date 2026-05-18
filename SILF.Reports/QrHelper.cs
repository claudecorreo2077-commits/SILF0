// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\QrHelper.cs
using QRCoder;

namespace SILF.Reports;

/// <summary>
/// Helper para generar códigos QR como imagen PNG en bytes.
/// Usado por el generador PDF y por la vista previa WPF.
/// </summary>
public static class QrHelper
{
    /// <summary>
    /// Genera un código QR como PNG en bytes.
    /// </summary>
    public static byte[] GenerarPng(string data, int pixelsPerModule = 5)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Genera la cadena de datos para el QR de un recibo.
    /// </summary>
    public static string DatosRecibo(int numero, decimal monto, DateTime fecha,
        string beneficiario, string tipo, string entrego, string recibio, string? concepto = null)
    {
        var data = $"SILF|R{numero}|{fecha:yyyyMMdd}|Bs{monto:F2}|{tipo}|Entrego:{entrego}|Recibio:{recibio}";
        if (!string.IsNullOrWhiteSpace(concepto))
            data += $"|Concepto:{concepto}";
        return data;
    }
}
