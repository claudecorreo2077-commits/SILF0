// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Core\Helpers\SilfCrypto.cs
using System.Security.Cryptography;
using System.Text;

namespace SILF.Core.Helpers;

/// <summary>
/// Encriptación/desencriptación autenticada para archivos .silf-arqueo.
///
/// Esquema: Encrypt-then-MAC (estándar de la industria)
///   AES-256-CBC para confidencialidad
///   HMAC-SHA256 para autenticidad e integridad
///
/// La passphrase es fija (embebida) y la sal también; el propósito NO es
/// proteger el archivo contra un atacante motivado, sino:
///   1. Evitar que un usuario abra el archivo en un editor de texto
///      y lo modifique manualmente.
///   2. Detectar corrupción accidental durante transferencia (USB, email).
///   3. Garantizar que el archivo provenga del propio SILF y no de otra app.
///
/// Si en el futuro se necesita mayor seguridad, basta con cambiar la
/// passphrase por una contraseña que el usuario ingrese al exportar/importar.
/// </summary>
public static class SilfCrypto
{
    // ── Magic + versión del formato ──
    public static readonly byte[] FILE_MAGIC = Encoding.ASCII.GetBytes("SILFARQ\0");  // 8 bytes
    public const byte FILE_VERSION = 0x01;

    // ── Tamaños ──
    public const int IV_SIZE = 16;        // AES block size
    public const int HMAC_SIZE = 32;      // SHA-256 output
    public const int KEY_SIZE = 32;       // AES-256
    public const int HEADER_SIZE = 8 + 1 + IV_SIZE;  // 25 bytes: magic + version + IV

    // ── Passphrase y sal embebidas ──
    // No es un "secret" en sentido criptográfico. Es una marca que diferencia
    // archivos SILF de cualquier otro AES-CBC. Si alguien con tiempo desensambla
    // el ejecutable la puede extraer; eso es esperado para este nivel de uso.
    private const string PASSPHRASE = "SILF-Arqueo-Caja-Chica-2026-Empresa-Minera-Porco-Potosi";
    private static readonly byte[] SALT = Encoding.UTF8.GetBytes("SILF_v1_arqueo_salt_static_v0xABC0");

    private const int PBKDF2_ITERATIONS = 100_000;

    /// <summary>
    /// Deriva dos claves (AES y HMAC) de la passphrase usando PBKDF2-HMAC-SHA256.
    /// Devuelve un tuple: (keyAes [32], keyHmac [32]).
    /// </summary>
    private static (byte[] KeyAes, byte[] KeyHmac) DerivarClaves()
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            PASSPHRASE, SALT, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);

        var keyAes = pbkdf2.GetBytes(KEY_SIZE);
        var keyHmac = pbkdf2.GetBytes(KEY_SIZE);
        return (keyAes, keyHmac);
    }

    /// <summary>
    /// Encripta el texto plano y lo envuelve en el formato .silf-arqueo:
    ///
    ///   [0..7]   Magic 'SILFARQ\0'
    ///   [8]      Version 0x01
    ///   [9..24]  IV (16 bytes aleatorios)
    ///   [25..N]  Ciphertext (AES-256-CBC, PKCS7)
    ///   [N..N+32] HMAC-SHA256 sobre todo lo anterior (incluyendo header e IV)
    ///
    /// </summary>
    public static byte[] Encriptar(string textoPlano)
    {
        if (textoPlano == null) throw new ArgumentNullException(nameof(textoPlano));

        var (keyAes, keyHmac) = DerivarClaves();
        var iv = RandomNumberGenerator.GetBytes(IV_SIZE);

        // ── Encriptar ──
        byte[] cipherText;
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyAes;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(textoPlano);
            cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        // ── Construir buffer: magic + version + IV + ciphertext ──
        using var ms = new MemoryStream();
        ms.Write(FILE_MAGIC, 0, FILE_MAGIC.Length);
        ms.WriteByte(FILE_VERSION);
        ms.Write(iv, 0, iv.Length);
        ms.Write(cipherText, 0, cipherText.Length);

        var encryptedPayload = ms.ToArray();

        // ── HMAC sobre TODO el payload (encrypt-then-MAC) ──
        byte[] hmacTag;
        using (var hmac = new HMACSHA256(keyHmac))
        {
            hmacTag = hmac.ComputeHash(encryptedPayload);
        }

        // ── Concatenar payload + HMAC ──
        var resultado = new byte[encryptedPayload.Length + hmacTag.Length];
        Buffer.BlockCopy(encryptedPayload, 0, resultado, 0, encryptedPayload.Length);
        Buffer.BlockCopy(hmacTag, 0, resultado, encryptedPayload.Length, hmacTag.Length);

        return resultado;
    }

    /// <summary>
    /// Desencripta y verifica un archivo .silf-arqueo. Lanza
    /// <see cref="SilfArqueoFormatException"/> si el archivo no es válido,
    /// está corrupto, o fue modificado externamente.
    /// </summary>
    public static string Desencriptar(byte[] archivo)
    {
        if (archivo == null) throw new ArgumentNullException(nameof(archivo));
        if (archivo.Length < HEADER_SIZE + HMAC_SIZE + 16)  // mínimo: header + hmac + 1 bloque AES
            throw new SilfArqueoFormatException("Archivo demasiado pequeño o vacío.");

        // ── Verificar magic ──
        for (int i = 0; i < FILE_MAGIC.Length; i++)
        {
            if (archivo[i] != FILE_MAGIC[i])
                throw new SilfArqueoFormatException(
                    "El archivo no es un .silf-arqueo válido (firma incorrecta).");
        }

        // ── Verificar versión ──
        var version = archivo[FILE_MAGIC.Length];
        if (version != FILE_VERSION)
            throw new SilfArqueoFormatException(
                $"Versión de archivo no soportada: 0x{version:X2}. Esperaba 0x{FILE_VERSION:X2}.");

        var (keyAes, keyHmac) = DerivarClaves();

        // ── Verificar HMAC ──
        var payloadLength = archivo.Length - HMAC_SIZE;
        var payload = new byte[payloadLength];
        Buffer.BlockCopy(archivo, 0, payload, 0, payloadLength);

        var hmacEsperado = new byte[HMAC_SIZE];
        Buffer.BlockCopy(archivo, payloadLength, hmacEsperado, 0, HMAC_SIZE);

        byte[] hmacCalculado;
        using (var hmac = new HMACSHA256(keyHmac))
        {
            hmacCalculado = hmac.ComputeHash(payload);
        }

        if (!CryptographicOperations.FixedTimeEquals(hmacEsperado, hmacCalculado))
            throw new SilfArqueoFormatException(
                "El archivo está corrupto o fue modificado. Verificación de integridad falló.");

        // ── Extraer IV y ciphertext ──
        var iv = new byte[IV_SIZE];
        Buffer.BlockCopy(archivo, FILE_MAGIC.Length + 1, iv, 0, IV_SIZE);

        var cipherStart = HEADER_SIZE;
        var cipherLength = payloadLength - HEADER_SIZE;
        var cipherText = new byte[cipherLength];
        Buffer.BlockCopy(archivo, cipherStart, cipherText, 0, cipherLength);

        // ── Desencriptar ──
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyAes;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            throw new SilfArqueoFormatException(
                "No se pudo desencriptar el archivo. " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Devuelve true si los bytes parecen un archivo .silf-arqueo válido
    /// (solo verifica el magic, no hace verificación completa).
    /// </summary>
    public static bool TieneFormatoSilfArqueo(byte[] archivo)
    {
        if (archivo == null || archivo.Length < FILE_MAGIC.Length) return false;
        for (int i = 0; i < FILE_MAGIC.Length; i++)
            if (archivo[i] != FILE_MAGIC[i]) return false;
        return true;
    }
}

/// <summary>
/// Excepción lanzada cuando un archivo .silf-arqueo tiene formato inválido,
/// está corrupto o fue manipulado externamente.
/// </summary>
public class SilfArqueoFormatException : Exception
{
    public SilfArqueoFormatException(string message) : base(message) { }
    public SilfArqueoFormatException(string message, Exception inner) : base(message, inner) { }
}
