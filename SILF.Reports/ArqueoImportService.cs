// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Reports\ArqueoImportService.cs
using System.Text.Json;
using SILF.Core.Helpers;

namespace SILF.Reports;

/// <summary>
/// Resultado del proceso de importación de un archivo .silf-arqueo.
/// </summary>
public class ResultadoImportacion
{
    public bool Exito { get; set; }
    public int ArqueosImportados { get; set; }
    public int ArqueosOmitidosDuplicados { get; set; }
    public string ExportadoPor { get; set; } = "";
    public string MaquinaOrigen { get; set; } = "";
    public DateTime FechaExportacion { get; set; }
    public string? MensajeError { get; set; }
    public List<ArqueoExportado> ArqueosNuevos { get; set; } = new();
}

/// <summary>
/// Servicio que lee y desencripta un archivo .silf-arqueo.
/// La deduplicación e inserción en BD la maneja el llamador.
/// </summary>
public static class ArqueoImportService
{
    /// <summary>
    /// Lee, verifica y desencripta un archivo .silf-arqueo desde disco.
    /// Devuelve los datos parseados o lanza una excepción.
    /// </summary>
    public static SilfArqueoPayload LeerArchivo(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
            throw new FileNotFoundException("El archivo no existe.", rutaArchivo);

        var bytes = File.ReadAllBytes(rutaArchivo);

        if (!SilfCrypto.TieneFormatoSilfArqueo(bytes))
            throw new SilfArqueoFormatException(
                "El archivo seleccionado no es un .silf-arqueo válido.");

        var json = SilfCrypto.Desencriptar(bytes);

        var opciones = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var payload = JsonSerializer.Deserialize<SilfArqueoPayload>(json, opciones)
            ?? throw new SilfArqueoFormatException("El archivo no contiene datos válidos.");

        if (payload.Version != 1)
            throw new SilfArqueoFormatException(
                $"Versión del archivo no soportada: {payload.Version}. Esperaba 1.");

        return payload;
    }

    /// <summary>
    /// Dado un payload importado y un conjunto de IdentificadoresUnicos ya
    /// existentes en la BD, separa los arqueos en (a importar) vs (duplicados).
    /// </summary>
    public static (List<ArqueoExportado> AImportar, List<ArqueoExportado> Duplicados)
        Deduplicar(SilfArqueoPayload payload, HashSet<string> identificadoresExistentes)
    {
        var aImportar = new List<ArqueoExportado>();
        var duplicados = new List<ArqueoExportado>();

        foreach (var a in payload.Arqueos)
        {
            if (string.IsNullOrWhiteSpace(a.IdentificadorUnico))
                continue;  // arqueo sin identificador → corrupto, omitir

            if (identificadoresExistentes.Contains(a.IdentificadorUnico))
                duplicados.Add(a);
            else
                aImportar.Add(a);
        }

        return (aImportar, duplicados);
    }

    /// <summary>
    /// Genera la cadena descriptiva del origen para guardar en el campo
    /// <c>OrigenImportacion</c> del arqueo.
    /// Formato: "PC-CONTADOR · Juan Pérez · 2026-05-24 15:30"
    /// </summary>
    public static string GenerarTextoOrigen(SilfArqueoPayload payload)
    {
        var maquina = string.IsNullOrWhiteSpace(payload.MaquinaOrigen) ? "Desconocida" : payload.MaquinaOrigen;
        var usuario = string.IsNullOrWhiteSpace(payload.ExportadoPor) ? "Usuario" : payload.ExportadoPor;
        var fecha = payload.FechaExportacion.ToString("yyyy-MM-dd HH:mm");
        return $"{maquina} · {usuario} · {fecha}";
    }
}
