// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.Data\SilfDbContext.cs
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

    public SilfDbContext()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _dbPath = Path.Combine(appDir, "silf.db");
    }

    public SilfDbContext(DbContextOptions<SilfDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }
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
            .HasIndex(l => l.NumeroLote);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<ReciboCaja>()
            .HasIndex(r => r.NumeroRecibo)
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
            TipoCambio = 6.97m
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
    }
}
