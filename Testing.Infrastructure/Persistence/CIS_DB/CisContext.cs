using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class CisContext : DbContext
{
    public CisContext(DbContextOptions<CisContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RutasZam> RutasZams { get; set; }

    public virtual DbSet<Sucursale> Sucursales { get; set; }

    public virtual DbSet<ZemogViajesEnZamAnual> ZemogViajesEnZamAnuals { get; set; }

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

        modelBuilder.Entity<Sucursale>(entity =>
        {
            entity.HasKey(e => e.IdSucursal).HasName("PK_Sucursal");

            entity.HasIndex(e => e.Nomenclatura, "NonClusteredIndex-20221214-173759").IsUnique();

            entity.Property(e => e.IdSucursal)
                .ValueGeneratedNever()
                .HasColumnName("Id_sucursal");
            entity.Property(e => e.IdAreaZam)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdOperacion).HasColumnName("Id_operacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreCorto)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Nomenclatura)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ZemogViajesEnZamAnual>(entity =>
        {
            entity.HasKey(e => e.Identificador);

            entity.ToTable("ZemogViajesEnZamAnual");

            entity.HasIndex(e => e.CitaCarga, "NonClusteredIndex-20241114-135104");

            entity.Property(e => e.CartaPorte)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Casetas).IsUnicode(false);
            entity.Property(e => e.CitaCarga).HasColumnType("datetime");
            entity.Property(e => e.CodigoRuta)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ComisionOp1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ComisionOp2).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ComisionRuta).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CompensacionOp1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CompensacionOp2).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ConceptosDispension).IsUnicode(false);
            entity.Property(e => e.CostoLitroDiesel).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Destino)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Dolly)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.EstadoDestino)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.EstadoOrigen)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.EstatusAsignacion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Expedicion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Factura)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.FechaConfirmacionViaticos).HasColumnType("datetime");
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaLiquidacion).HasColumnType("datetime");
            entity.Property(e => e.FechaTimbrado).HasColumnType("datetime");
            entity.Property(e => e.Folio)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.FolioComplemento)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ListosDieselViaje).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.MontoPeajeEfectivo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoPeajeIave).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Operacion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Operador1)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Operador2)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Origen)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Remolque1)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Remolque2)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Rendimiento).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Ruta)
                .HasMaxLength(350)
                .IsUnicode(false);
            entity.Property(e => e.Sucursal)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Tipo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalViaticos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Trayecto)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Unidad)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.ZemogViajesEnZamAnuals)
                .HasForeignKey(d => d.IdSucursal)
                .HasConstraintName("FK_ZemogViajesEnZamAnual_Sucursales");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
