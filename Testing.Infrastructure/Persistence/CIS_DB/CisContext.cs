using Microsoft.EntityFrameworkCore;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class CisContext : DbContext
{
    public CisContext(DbContextOptions<CisContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RutasZam> RutasZams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_CI_AI");

        modelBuilder.Entity<RutasZam>(entity =>
        {
            entity.HasKey(e => e.Ruta);

            entity.ToTable("RutasZam");

            entity.Property(e => e.Ruta)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Destino)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Kms).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Origen)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
