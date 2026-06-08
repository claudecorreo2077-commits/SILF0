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
    public DbSet<Concentrado> Concentrados => Set<Concentrado>();
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
                // ProcesosFlotacion: tabla (las "flotaciones")
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
                // Lotes: FK al proceso de flotación AHORA ANULABLE.
                // NULL = lote disponible (liquidado, sin flotación).
                // Las flotaciones se arman manualmente seleccionando liquidaciones.
                // ════════════════════════════════════════
                var columnasLotes = ListarColumnas(conn, "Lotes");

                if (!columnasLotes.Contains("ProcesoFlotacionId"))
                {
                    EjecutarSql(conn, "ALTER TABLE Lotes ADD COLUMN ProcesoFlotacionId INTEGER NULL;");
                    EjecutarSql(conn, "CREATE INDEX IF NOT EXISTS IX_Lotes_ProcesoFlotacionId ON Lotes (ProcesoFlotacionId);");
                }

                // ════════════════════════════════════════
                // RecibosCaja: dos talonarios independientes
                // ════════════════════════════════════════
                if (ExisteTabla(conn, "RecibosCaja"))
                {
                    var tieneIndiceViejo = ExisteIndice(conn, "IX_RecibosCaja_NumeroRecibo");
                    var tieneIndiceNuevo = ExisteIndice(conn, "IX_RecibosCaja_TipoMovimiento_NumeroRecibo");

                    if (!tieneIndiceNuevo)
                    {
                        if (tieneIndiceViejo)
                            EjecutarSql(conn, "DROP INDEX IX_RecibosCaja_NumeroRecibo;");

                        RenumerarRecibosPorTipo(conn);

                        EjecutarSql(conn,
                            "CREATE UNIQUE INDEX IX_RecibosCaja_TipoMovimiento_NumeroRecibo " +
                            "ON RecibosCaja (TipoMovimiento, NumeroRecibo);");
                    }
                }

                // ════════════════════════════════════════
                // ArqueosCaja: columnas de exportación/importación
                // ════════════════════════════════════════
                if (ExisteTabla(conn, "ArqueosCaja"))
                {
                    var columnasArqueos = ListarColumnas(conn, "ArqueosCaja");

                    if (!columnasArqueos.Contains("IdentificadorUnico"))
                    {
                        EjecutarSql(conn,
                            "ALTER TABLE ArqueosCaja ADD COLUMN IdentificadorUnico TEXT NULL;");
                        PoblarIdentificadoresArqueos(conn);
                        EjecutarSql(conn,
                            "CREATE UNIQUE INDEX IX_ArqueosCaja_IdentificadorUnico " +
                            "ON ArqueosCaja (IdentificadorUnico);");
                    }

                    if (!columnasArqueos.Contains("Exportado"))
                    {
                        EjecutarSql(conn,
                            "ALTER TABLE ArqueosCaja ADD COLUMN Exportado INTEGER NOT NULL DEFAULT 0;");
                    }

                    if (!columnasArqueos.Contains("FechaExportacion"))
                    {
                        EjecutarSql(conn,
                            "ALTER TABLE ArqueosCaja ADD COLUMN FechaExportacion TEXT NULL;");
                    }

                    if (!columnasArqueos.Contains("OrigenImportacion"))
                    {
                        EjecutarSql(conn,
                            "ALTER TABLE ArqueosCaja ADD COLUMN OrigenImportacion TEXT NULL;");
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
    /// Asigna un Guid único a cada arqueo que aún no lo tenga.
    /// </summary>
    private static void PoblarIdentificadoresArqueos(SqliteConnection conn)
    {
        var ids = new List<long>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM ArqueosCaja WHERE IdentificadorUnico IS NULL ORDER BY Id ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) ids.Add(reader.GetInt64(0));
        }

        if (ids.Count == 0) return;

        using var tx = conn.BeginTransaction();
        foreach (var id in ids)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE ArqueosCaja SET IdentificadorUnico = $g WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$g", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>
    /// Renumera los recibos existentes por TipoMovimiento, desde 1.
    /// </summary>
    private static void RenumerarRecibosPorTipo(SqliteConnection conn)
    {
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

        if (porTipo.Count == 0) return;

        using (var tx = conn.BeginTransaction())
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE RecibosCaja SET NumeroRecibo = -Id;";
                cmd.ExecuteNonQuery();
            }

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

        // Lote → ProcesoFlotacion: OPCIONAL.
        // Al eliminar una flotación, los lotes quedan con FK null (disponibles).
        // En la práctica desvinculamos manualmente antes de borrar, pero SetNull
        // es la red de seguridad declarada.
        modelBuilder.Entity<Lote>()
            .HasOne(l => l.ProcesoFlotacion)
            .WithMany(pf => pf.Lotes)
            .HasForeignKey(l => l.ProcesoFlotacionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

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
            .HasIndex(l => l.ProcesoFlotacionId);

        modelBuilder.Entity<ProcesoFlotacion>()
            .HasIndex(p => p.NumeroProceso)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<ReciboCaja>()
            .HasIndex(r => new { r.TipoMovimiento, r.NumeroRecibo })
            .IsUnique();

        modelBuilder.Entity<ArqueoCaja>()
            .HasIndex(a => a.IdentificadorUnico)
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

        // NOTA: ya NO se siembra ningún ProcesoFlotacion. Las flotaciones se crean
        // a demanda agrupando liquidaciones disponibles desde el módulo Inv. Flotación.
    }
}
