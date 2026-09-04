using Microsoft.EntityFrameworkCore;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class CisContext : DbContext
{
    public CisContext()
    {
    }

    public CisContext(DbContextOptions<CisContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RutasZam> RutasZams { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=10.128.0.5;DataBase=CIS_DB;User Id=sqlalejandro;Password=Q7FozxB}qULd&Xy(;TrustServerCertificate=True");

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
