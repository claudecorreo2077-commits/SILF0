// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Recovery\Program.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

// ══════════════════════════════════════════
// SILF Recovery Tool
// Acceso de emergencia para resetear contraseñas
// ══════════════════════════════════════════

var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "silf.db");

// Buscar silf.db en el directorio actual o en el padre
if (!File.Exists(dbPath))
{
    var parentPath = Path.Combine(Directory.GetCurrentDirectory(), "silf.db");
    if (File.Exists(parentPath))
        dbPath = parentPath;
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No se encontró silf.db en el directorio actual.");
        Console.WriteLine("Coloque este archivo en la misma carpeta que silf.db");
        Console.ResetColor();
        Console.ReadKey();
        return;
    }
}

// Autenticación con clave maestra
Console.Title = "Mantenimiento del Sistema";
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("════════════════════════════════════════");
Console.WriteLine("  Herramienta de Mantenimiento");
Console.WriteLine("════════════════════════════════════════");
Console.ResetColor();
Console.Write("\nClave de acceso: ");
Console.ForegroundColor = ConsoleColor.Black; // ocultar texto
var clave = Console.ReadLine()?.Trim() ?? "";
Console.ResetColor();

// Verificar clave maestra (hash SHA256 de la clave)
var claveHash = Sha256(clave);
var masterHash = Sha256("krlosdelfin:1983MVPemq100v$");

if (claveHash != masterHash)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\nAcceso denegado.");
    Console.ResetColor();
    Console.ReadKey();
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n✓ Acceso autorizado.\n");
Console.ResetColor();

// Conectar a la BD
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("┌─────────────────────────────────┐");
    Console.WriteLine("│  1. Listar usuarios             │");
    Console.WriteLine("│  2. Resetear contraseña         │");
    Console.WriteLine("│  3. Activar/Desactivar usuario  │");
    Console.WriteLine("│  4. Salir                       │");
    Console.WriteLine("└─────────────────────────────────┘");
    Console.ResetColor();
    Console.Write("\nOpción: ");
    var opcion = Console.ReadLine()?.Trim();

    switch (opcion)
    {
        case "1":
            ListarUsuarios(conn);
            break;
        case "2":
            ResetearPassword(conn);
            break;
        case "3":
            ToggleActivo(conn);
            break;
        case "4":
            return;
        default:
            Console.WriteLine("Opción no válida.");
            break;
    }
    Console.WriteLine();
}

// ══════════════════════════════════════════
// FUNCIONES
// ══════════════════════════════════════════

static void ListarUsuarios(SqliteConnection conn)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Id, NombreCompleto, NombreUsuario, Rol, Activo FROM Usuarios ORDER BY Id";
    using var reader = cmd.ExecuteReader();

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"{"ID",-5} {"Nombre",-25} {"Usuario",-15} {"Rol",-15} {"Activo"}");
    Console.WriteLine(new string('─', 70));
    Console.ResetColor();

    while (reader.Read())
    {
        var activo = reader.GetInt32(4) == 1;
        Console.ForegroundColor = activo ? ConsoleColor.White : ConsoleColor.DarkGray;
        Console.WriteLine($"{reader.GetInt32(0),-5} {reader.GetString(1),-25} {reader.GetString(2),-15} {reader.GetInt32(3),-15} {(activo ? "Sí" : "No")}");
    }
    Console.ResetColor();
}

static void ResetearPassword(SqliteConnection conn)
{
    Console.Write("ID del usuario: ");
    if (!int.TryParse(Console.ReadLine(), out int userId))
    {
        Console.WriteLine("ID inválido.");
        return;
    }

    // Verificar que existe
    using var check = conn.CreateCommand();
    check.CommandText = "SELECT NombreCompleto, NombreUsuario FROM Usuarios WHERE Id = @id";
    check.Parameters.AddWithValue("@id", userId);
    using var reader = check.ExecuteReader();

    if (!reader.Read())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Usuario no encontrado.");
        Console.ResetColor();
        return;
    }

    var nombre = reader.GetString(0);
    var usuario = reader.GetString(1);
    reader.Close();

    Console.WriteLine($"Usuario: {nombre} ({usuario})");
    Console.Write("Nueva contraseña: ");
    var nuevaPass = Console.ReadLine()?.Trim() ?? "";

    if (nuevaPass.Length < 4)
    {
        Console.WriteLine("La contraseña debe tener al menos 4 caracteres.");
        return;
    }

    Console.Write($"¿Confirmar reset para '{usuario}'? (s/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "s") return;

    using var update = conn.CreateCommand();
    update.CommandText = "UPDATE Usuarios SET PasswordHash = @hash WHERE Id = @id";
    update.Parameters.AddWithValue("@hash", Sha256(nuevaPass));
    update.Parameters.AddWithValue("@id", userId);
    update.ExecuteNonQuery();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✓ Contraseña de '{usuario}' actualizada.");
    Console.ResetColor();
}

static void ToggleActivo(SqliteConnection conn)
{
    Console.Write("ID del usuario: ");
    if (!int.TryParse(Console.ReadLine(), out int userId))
    {
        Console.WriteLine("ID inválido.");
        return;
    }

    using var check = conn.CreateCommand();
    check.CommandText = "SELECT NombreUsuario, Activo FROM Usuarios WHERE Id = @id";
    check.Parameters.AddWithValue("@id", userId);
    using var reader = check.ExecuteReader();

    if (!reader.Read())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Usuario no encontrado.");
        Console.ResetColor();
        return;
    }

    var usuario = reader.GetString(0);
    var activo = reader.GetInt32(1) == 1;
    reader.Close();

    var nuevoEstado = activo ? 0 : 1;
    using var update = conn.CreateCommand();
    update.CommandText = "UPDATE Usuarios SET Activo = @activo WHERE Id = @id";
    update.Parameters.AddWithValue("@activo", nuevoEstado);
    update.Parameters.AddWithValue("@id", userId);
    update.ExecuteNonQuery();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✓ '{usuario}' ahora está {(nuevoEstado == 1 ? "ACTIVO" : "DESACTIVADO")}.");
    Console.ResetColor();
}

static string Sha256(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexStringLower(bytes);
}
