// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Helpers\NumeroALetras.cs
namespace SILF.Core.Helpers;

/// <summary>
/// Convierte un número decimal a su representación en letras en español.
/// Equivalente al macro NumLetras del Excel de Caja Chica.
/// Ejemplo: 1100.00 → "UN MIL CIEN 00/100 BOLIVIANOS"
/// </summary>
public static class NumeroALetras
{
    private static readonly string[] Unidades =
    {
        "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
        "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE",
        "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE",
        "VEINTE", "VEINTIUN", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO",
        "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
    };

    private static readonly string[] Decenas =
    {
        "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA",
        "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
    };

    private static readonly string[] Centenas =
    {
        "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
        "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
    };

    /// <summary>
    /// Convierte un valor numérico a texto en español con el formato del recibo.
    /// </summary>
    /// <param name="valor">Monto a convertir.</param>
    /// <param name="moneda">Nombre de la moneda en plural (ej: "BOLIVIANOS").</param>
    /// <returns>Texto como "UN MIL CIEN 00/100 BOLIVIANOS".</returns>
    public static string Convertir(decimal valor, string moneda = "BOLIVIANOS")
    {
        valor = Math.Round(valor, 2);
        long parteEntera = (long)Math.Abs(valor);
        int centavos = (int)Math.Round((Math.Abs(valor) - parteEntera) * 100);

        string letras;

        if (parteEntera == 0)
            letras = "CERO";
        else
            letras = ConvertirGrupos(parteEntera);

        string resultado = $"{letras.Trim()} {centavos:00}/100 {moneda}";
        return resultado;
    }

    private static string ConvertirGrupos(long numero)
    {
        if (numero == 0) return "";

        // Millones
        if (numero >= 1_000_000)
        {
            long millones = numero / 1_000_000;
            long resto = numero % 1_000_000;
            string textoMillones = millones == 1
                ? "UN MILLON"
                : $"{ConvertirGrupos(millones).Trim()} MILLONES";
            string textoResto = resto > 0 ? $" {ConvertirGrupos(resto).Trim()}" : "";
            return $"{textoMillones}{textoResto}";
        }

        // Miles
        if (numero >= 1_000)
        {
            long miles = numero / 1_000;
            long resto = numero % 1_000;
            string textoMiles = miles == 1
                ? "UN MIL"
                : $"{ConvertirGrupos(miles).Trim()} MIL";
            string textoResto = resto > 0 ? $" {ConvertirGrupos(resto).Trim()}" : "";
            return $"{textoMiles}{textoResto}";
        }

        // Centenas
        if (numero >= 100)
        {
            if (numero == 100) return "CIEN";

            int centena = (int)(numero / 100);
            long resto = numero % 100;
            string textoCentena = Centenas[centena - 1];
            string textoResto = resto > 0 ? $" {ConvertirGrupos(resto).Trim()}" : "";
            return $"{textoCentena}{textoResto}";
        }

        // 1-29 (casos directos)
        if (numero <= 29)
        {
            return Unidades[(int)numero - 1];
        }

        // 30-99
        int decena = (int)(numero / 10);
        int unidad = (int)(numero % 10);
        string textoDecena = Decenas[decena - 1];
        if (unidad == 0) return textoDecena;
        return $"{textoDecena} Y {Unidades[unidad - 1]}";
    }
}
