// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\ArqueoExportService.cs
using System.Text.Json;
using SILF.Core.Helpers;
using SILF.Core.Models;

namespace SILF.Reports;

/// <summary>
/// Datos contenidos en un archivo .silf-arqueo (versión 1).
/// </summary>
public class SilfArqueoPayload
{
    public int Version { get; set; } = 1;
    public DateTime FechaExportacion { get; set; }
    public string ExportadoPor { get; set; } = "";
    public string MaquinaOrigen { get; set; } = "";
    public string Empresa { get; set; } = "";
    public int Cantidad { get; set; }
    public List<ArqueoExportado> Arqueos { get; set; } = new();
}

public class ArqueoExportado
{
    public string IdentificadorUnico { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal SaldoContable { get; set; }
    public decimal SaldoFisico { get; set; }
    public decimal Diferencia { get; set; }
    public string? Observaciones { get; set; }
    public string? RealizadoPor { get; set; }
}

/// <summary>
/// Servicio que exporta una lista de arqueos a un archivo .silf-arqueo
/// encriptado y autenticado.
/// </summary>
public static class ArqueoExportService
{
    /// <summary>
    /// Serializa los arqueos a JSON, los encripta con AES-256-CBC + HMAC,
    /// y devuelve el contenido del archivo listo para guardar en disco.
    /// </summary>
    /// <param name="arqueos">Arqueos a exportar.</param>
    /// <param name="exportadoPor">Nombre del usuario que exporta.</param>
    /// <param name="empresa">Nombre de la empresa (informativo).</param>
    /// <returns>Bytes del archivo .silf-arqueo.</returns>
    public static byte[] Generar(
        IEnumerable<ArqueoCaja> arqueos,
        string exportadoPor,
        string empresa)
    {
        if (arqueos == null) throw new ArgumentNullException(nameof(arqueos));

        var lista = arqueos.Select(a => new ArqueoExportado
        {
            IdentificadorUnico = a.IdentificadorUnico,
            Fecha = a.Fecha,
            SaldoContable = a.SaldoContable,
            SaldoFisico = a.SaldoFisico,
            Diferencia = a.Diferencia,
            Observaciones = a.Observaciones,
            RealizadoPor = a.RealizadoPor
        }).ToList();

        var payload = new SilfArqueoPayload
        {
            Version = 1,
            FechaExportacion = DateTime.Now,
            ExportadoPor = exportadoPor ?? "Usuario",
            MaquinaOrigen = Environment.MachineName,
            Empresa = empresa ?? "",
            Cantidad = lista.Count,
            Arqueos = lista
        };

        var opciones = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(payload, opciones);

        return SilfCrypto.Encriptar(json);
    }

    /// <summary>
    /// Sugiere un nombre de archivo descriptivo para el exporte.
    /// Ejemplo: "arqueos_2026-05-24_15-30_juan_perez.silf-arqueo"
    /// </summary>
    public static string SugerirNombreArchivo(string exportadoPor)
    {
        var nombreLimpio = (exportadoPor ?? "usuario")
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('.', '_');

        // Quitar caracteres no aptos para nombre de archivo
        var safe = new string(nombreLimpio
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .ToArray());

        if (string.IsNullOrWhiteSpace(safe)) safe = "usuario";

        var fecha = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        return $"arqueos_{fecha}_{safe}.silf-arqueo";
    }
}
