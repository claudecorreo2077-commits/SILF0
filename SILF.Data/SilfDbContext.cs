// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Data\SilfDbContext.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SILF.Core.Models;
using SILF.Core.Enums;

namespace SILF.Data;

public class SilfDbContext : DbContext
{
    // ── Configuración ──
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // ── Catálogos ──
    public DbSet<Cooperativa> Cooperativas => Set<Cooperativa>();
    public DbSet<Mina> Minas => Set<Mina>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    // ── Núcleo del negocio ──
    public DbSet<ProcesoFlotacion> ProcesosFlotacion => Set<ProcesoFlotacion>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<Flotacion> Flotaciones => Set<Flotacion>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<BonoTransporte> BonosTransporte => Set<BonoTransporte>();

    // ── Caja chica ──
    public DbSet<ReciboCaja> RecibosCaja => Set<ReciboCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<ArqueoCaja> ArqueosCaja => Set<ArqueoCaja>();

    private readonly string _dbPath = string.Empty;

    // ── Auto-migración: se ejecuta una sola vez por proceso ──
    private static bool _esquemaVerificado = false;
    private static readonly object _esquemaLock = new();

    public SilfDbContext()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = Path.Combine(appDir, "silf.db");
        AsegurarEsquemaActualizado();
    }

    public SilfDbContext(DbContextOptions<SilfDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }
    }

    /// <summary>
    /// Aplica migraciones manuales de esquema cuando la BD existe pero
    /// es de una versión anterior. Idempotente: si ya está al día, no hace nada.
    /// </summary>
    private void AsegurarEsquemaActualizado()
    {
        lock (_esquemaLock)
        {
            if (_esquemaVerificado) return;
            if (!File.Exists(_dbPath)) { _esquemaVerificado = true; return; }

            try
            {
                using var conn = new SqliteConnection($"Data Source={_dbPath}");
                conn.Open();

                // ════════════════════════════════════════
                // Empresas: dos tipos de cambio
                // ════════════════════════════════════════
                var columnasEmpresas = ListarColumnas(conn, "Empresas");

                if (columnasEmpresas.Contains("TipoCambio") && !columnasEmpresas.Contains("TipoCambioGeneral"))
                {
                    EjecutarSql(conn, "ALTER TABLE Empresas RENAME COLUMN TipoCambio TO TipoCambioGeneral;");
                    columnasEmpresas.Remove("TipoCambio");
                    columnasEmpresas.Add("TipoCambioGeneral");
                }

                if (!columnasEmpresas.Contains("TipoCambioGeneral"))
                {
                    EjecutarSql(conn, "ALTER TABLE Empresas ADD COLUMN TipoCambioGeneral DECIMAL(8,4) NOT NULL DEFAULT 6.90;");
                }

                if (!columnasEmpresas.Contains("TipoCambioRegalias"))
                {
                    EjecutarSql(conn, "ALTER TABLE Empresas ADD COLUMN TipoCambioRegalias DECIMAL(8,4) NOT NULL DEFAULT 6.96;");
                }

                // ════════════════════════════════════════
                // ProcesosFlotacion: tabla nueva
                // ════════════════════════════════════════
                if (!ExisteTabla(conn, "ProcesosFlotacion"))
                {
                    EjecutarSql(conn, @"
                        CREATE TABLE ProcesosFlotacion (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            NumeroProceso INTEGER NOT NULL,
                            FechaApertura TEXT NOT NULL,
                            FechaCierre TEXT NULL,
                            Estado INTEGER NOT NULL DEFAULT 0,
                            Observaciones TEXT NULL
                        );");
                    EjecutarSql(conn, "CREATE UNIQUE INDEX IX_ProcesosFlotacion_NumeroProceso ON ProcesosFlotacion (NumeroProceso);");
                }

                // ════════════════════════════════════════
                // Lotes: FK al proceso de flotación
                // ════════════════════════════════════════
                var columnasLotes = ListarColumnas(conn, "Lotes");

                if (!columnasLotes.Contains("ProcesoFlotacionId"))
                {
                    var cantProcesos = EscalarLong(conn, "SELECT COUNT(*) FROM ProcesosFlotacion;");
                    if (cantProcesos == 0)
                    {
                        var ahora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        EjecutarSql(conn,
                            $"INSERT INTO ProcesosFlotacion (NumeroProceso, FechaApertura, FechaCierre, Estado, Observaciones) " +
                            $"VALUES (1, '{ahora}', NULL, 0, 'Proceso inicial creado por migración automática');");
                    }
                    var procesoInicialId = EscalarLong(conn,
                        "SELECT Id FROM ProcesosFlotacion ORDER BY NumeroProceso ASC LIMIT 1;");

                    EjecutarSql(conn,
                        $"ALTER TABLE Lotes ADD COLUMN ProcesoFlotacionId INTEGER NOT NULL DEFAULT {procesoInicialId};");

                    EjecutarSql(conn, "CREATE INDEX IF NOT EXISTS IX_Lotes_ProcesoFlotacionId ON Lotes (ProcesoFlotacionId);");
                }

                // Asegurar siempre un proceso abierto
                var hayAbierto = EscalarLong(conn,
                    "SELECT COUNT(*) FROM ProcesosFlotacion WHERE Estado = 0;");
                if (hayAbierto == 0)
                {
                    var maxNum = EscalarLong(conn,
                        "SELECT COALESCE(MAX(NumeroProceso), 0) FROM ProcesosFlotacion;");
                    var ahora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    EjecutarSql(conn,
                        $"INSERT INTO ProcesosFlotacion (NumeroProceso, FechaApertura, FechaCierre, Estado, Observaciones) " +
                        $"VALUES ({maxNum + 1}, '{ahora}', NULL, 0, NULL);");
                }

                // ════════════════════════════════════════
                // RecibosCaja: dos talonarios independientes
                // (TipoMovimiento, NumeroRecibo) único en vez de NumeroRecibo único.
                // Si encontramos el índice viejo, lo borramos, renumeramos los recibos
                // existentes dentro de cada tipo y creamos el índice nuevo.
                // ════════════════════════════════════════
                if (ExisteTabla(conn, "RecibosCaja"))
                {
                    var tieneIndiceViejo = ExisteIndice(conn, "IX_RecibosCaja_NumeroRecibo");
                    var tieneIndiceNuevo = ExisteIndice(conn, "IX_RecibosCaja_TipoMovimiento_NumeroRecibo");

                    if (!tieneIndiceNuevo)
                    {
                        // Borrar índice viejo si está
                        if (tieneIndiceViejo)
                            EjecutarSql(conn, "DROP INDEX IX_RecibosCaja_NumeroRecibo;");

                        // Renumerar recibos existentes: ordenados por (Fecha, Id) dentro de
                        // cada TipoMovimiento, asignar correlativo desde 1.
                        // Si no hay recibos, este bloque no hace nada.
                        RenumerarRecibosPorTipo(conn);

                        // Crear índice nuevo compuesto único
                        EjecutarSql(conn,
                            "CREATE UNIQUE INDEX IX_RecibosCaja_TipoMovimiento_NumeroRecibo " +
                            "ON RecibosCaja (TipoMovimiento, NumeroRecibo);");
                    }
                }

                _esquemaVerificado = true;
            }
            catch (Exception)
            {
                _esquemaVerificado = true;
                throw;
            }
        }
    }

    /// <summary>
    /// Renumera los recibos existentes: ordenados por (Fecha asc, Id asc) DENTRO
    /// de cada TipoMovimiento, asigna NumeroRecibo = 1, 2, 3... empezando desde 1
    /// para cada tipo. Conserva el orden cronológico.
    /// </summary>
    private static void RenumerarRecibosPorTipo(SqliteConnection conn)
    {
        // Recoger los ids agrupados por tipo, en orden cronológico
        var porTipo = new Dictionary<string, List<long>>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, TipoMovimiento FROM RecibosCaja ORDER BY Fecha ASC, Id ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var tipo = reader.IsDBNull(1) ? "Salida" : reader.GetString(1);
                if (!porTipo.ContainsKey(tipo)) porTipo[tipo] = new List<long>();
                porTipo[tipo].Add(id);
            }
        }

        if (porTipo.Count == 0) return; // no hay recibos, nada que renumerar

        // Fase 1: poner números temporales NEGATIVOS para evitar colisiones con el índice
        // (los temporales son únicos globalmente porque usan el Id).
        using (var tx = conn.BeginTransaction())
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE RecibosCaja SET NumeroRecibo = -Id;";
                cmd.ExecuteNonQuery();
            }

            // Fase 2: asignar los correlativos definitivos por tipo
            foreach (var kvp in porTipo)
            {
                int correlativo = 1;
                foreach (var id in kvp.Value)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE RecibosCaja SET NumeroRecibo = $n WHERE Id = $id;";
                    cmd.Parameters.AddWithValue("$n", correlativo);
                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.ExecuteNonQuery();
                    correlativo++;
                }
            }

            tx.Commit();
        }
    }

    private static HashSet<string> ListarColumnas(SqliteConnection conn, string tabla)
    {
        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tabla});";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            columnas.Add(reader.GetString(1));
        }
        return columnas;
    }

    private static bool ExisteTabla(SqliteConnection conn, string tabla)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$nombre;";
        cmd.Parameters.AddWithValue("$nombre", tabla);
        var res = cmd.ExecuteScalar();
        return res != null && Convert.ToInt32(res) > 0;
    }

    private static bool ExisteIndice(SqliteConnection conn, string indice)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name=$nombre;";
        cmd.Parameters.AddWithValue("$nombre", indice);
        var res = cmd.ExecuteScalar();
        return res != null && Convert.ToInt32(res) > 0;
    }

    private static long EscalarLong(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var res = cmd.ExecuteScalar();
        return res == null || res == DBNull.Value ? 0L : Convert.ToInt64(res);
    }

    private static void EjecutarSql(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ══════════════════════════════════════════
        // RELACIONES 1:1 con Lote
        // ══════════════════════════════════════════

        modelBuilder.Entity<Liquidacion>()
            .HasOne(l => l.Lote)
            .WithOne(lot => lot.Liquidacion)
            .HasForeignKey<Liquidacion>(l => l.LoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Flotacion>()
            .HasOne(f => f.Lote)
            .WithOne(lot => lot.Flotacion)
            .HasForeignKey<Flotacion>(f => f.LoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Lote)
            .WithOne(lot => lot.Pago)
            .HasForeignKey<Pago>(p => p.LoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BonoTransporte>()
            .HasOne(b => b.Lote)
            .WithOne(lot => lot.BonoTransporte)
            .HasForeignKey<BonoTransporte>(b => b.LoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // ══════════════════════════════════════════
        // RELACIONES N:1
        // ══════════════════════════════════════════

        modelBuilder.Entity<Lote>()
            .HasOne(l => l.Proveedor)
            .WithMany(p => p.Lotes)
            .HasForeignKey(l => l.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lote>()
            .HasOne(l => l.Mina)
            .WithMany(m => m.Lotes)
            .HasForeignKey(l => l.MinaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lote>()
            .HasOne(l => l.ProcesoFlotacion)
            .WithMany(pf => pf.Lotes)
            .HasForeignKey(l => l.ProcesoFlotacionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Proveedor>()
            .HasOne(p => p.Cooperativa)
            .WithMany(c => c.Proveedores)
            .HasForeignKey(p => p.CooperativaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MovimientoCaja>()
            .HasOne(m => m.ReciboCaja)
            .WithMany()
            .HasForeignKey(m => m.ReciboCajaId)
            .OnDelete(DeleteBehavior.SetNull);

        // ══════════════════════════════════════════
        // ÍNDICES
        // ══════════════════════════════════════════

        modelBuilder.Entity<Proveedor>()
            .HasIndex(p => p.CiNit);

        modelBuilder.Entity<Lote>()
            .HasIndex(l => l.Estado);

        modelBuilder.Entity<Lote>()
            .HasIndex(l => l.FechaRegistro);

        modelBuilder.Entity<Lote>()
            .HasIndex(l => new { l.ProcesoFlotacionId, l.NumeroLote });

        modelBuilder.Entity<ProcesoFlotacion>()
            .HasIndex(p => p.NumeroProceso)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        // ── ReciboCaja: índice único COMPUESTO (TipoMovimiento + NumeroRecibo).
        // Permite tener INGRESO #1, INGRESO #2 y SALIDA #1, SALIDA #2 sin colisión.
        modelBuilder.Entity<ReciboCaja>()
            .HasIndex(r => new { r.TipoMovimiento, r.NumeroRecibo })
            .IsUnique();

        // ══════════════════════════════════════════
        // DATOS SEMILLA
        // ══════════════════════════════════════════

        modelBuilder.Entity<Empresa>().HasData(new Empresa
        {
            Id = 1,
            RazonSocial = "Empresa Minera",
            Municipio = "Porco",
            Ingenio = "Villa Imperial",
            TipoCambioGeneral = 6.90m,
            TipoCambioRegalias = 6.96m
        });

        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1,
            NombreCompleto = "Administrador",
            NombreUsuario = "admin",
            PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9",
            Rol = RolUsuario.Administrador,
            Activo = true,
            FechaCreacion = new DateTime(2026, 1, 1)
        });

        modelBuilder.Entity<Mina>().HasData(
            new Mina { Id = 1, Nombre = "CERRO" },
            new Mina { Id = 2, Nombre = "PORCO R.L." },
            new Mina { Id = 3, Nombre = "HUAYNA PORCO" }
        );

        modelBuilder.Entity<ProcesoFlotacion>().HasData(new ProcesoFlotacion
        {
            Id = 1,
            NumeroProceso = 1,
            FechaApertura = new DateTime(2026, 1, 1),
            FechaCierre = null,
            Estado = EstadoProcesoFlotacion.Abierto,
            Observaciones = "Proceso inicial"
        });
    }
}
