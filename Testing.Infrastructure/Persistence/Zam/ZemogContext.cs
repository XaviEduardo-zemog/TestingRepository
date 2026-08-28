using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Testing.Infrastructure.Persistence.Zam;

public partial class ZemogContext : DbContext
{
    public ZemogContext(DbContextOptions<ZemogContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TraficoCombustible> TraficoCombustibles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AS");

        modelBuilder.Entity<TraficoCombustible>(entity =>
        {
            entity.HasKey(e => e.IdCombustible).HasFillFactor(80);

            entity.ToTable("trafico_combustible", tb =>
                {
                    tb.HasTrigger("t_trafico_actualiza_liga_energex");
                    tb.HasTrigger("t_trafico_combustible");
                    tb.HasTrigger("t_trafico_combustible01");
                    tb.HasTrigger("tr_Cancelarvales");
                });

            entity.HasIndex(e => new { e.IdArea, e.NoViaje }, "IDX_trafico_combustible_01").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdArea, e.IdCombustible, e.NoViaje }, "NonClusteredIndex-20230105-164317").HasFillFactor(80);

            entity.HasIndex(e => new { e.NoConsecutivo, e.IdProveedor, e.StatusDocto }, "xak1_trafico_combustible")
                .IsUnique()
                .HasFilter("([status_docto]<>'C')")
                .HasFillFactor(80);

            entity.Property(e => e.IdCombustible)
                .ValueGeneratedNever()
                .HasColumnName("id_combustible");
            entity.Property(e => e.Almacen).HasColumnName("almacen");
            entity.Property(e => e.AreaLiq).HasColumnName("area_liq");
            entity.Property(e => e.Bonificacion)
                .HasDefaultValue(0m, "DF_trafico_combustible_bonificacion")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("bonificacion");
            entity.Property(e => e.BonificacionLts)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("bonificacion_lts");
            entity.Property(e => e.CantCargado)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("Cant_Cargado");
            entity.Property(e => e.CantSaldocomb)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("cant_saldocomb");
            entity.Property(e => e.CantidadComb)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("cantidad_comb");
            entity.Property(e => e.CantidadComb2)
                .HasDefaultValue(0m, "DF_trafico_combustible_cantidad_comb2")
                .HasColumnType("decimal(18, 9)")
                .HasColumnName("cantidad_comb2");
            entity.Property(e => e.CantidadCombHist)
                .HasDefaultValue(0m, "DF_trafico_combustible_cantidad_comb_hist")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("cantidad_comb_hist");
            entity.Property(e => e.CntrlCruceroKm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("cntrl_crucero_km");
            entity.Property(e => e.Coast)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("coast");
            entity.Property(e => e.ConteoFrenos)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("conteo_frenos");
            entity.Property(e => e.Descto)
                .HasDefaultValue(0m, "DF_trafico_combustible_descto")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("descto");
            entity.Property(e => e.DescuentoLts)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("descuento_lts");
            entity.Property(e => e.DieselReseteo)
                .HasDefaultValue(0m, "DF_trafico_combustible_diesel_reseteo")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("diesel_reseteo");
            entity.Property(e => e.DifLts)
                .HasDefaultValue(0m, "DF_trafico_combustible_Dif_Lts")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("Dif_Lts");
            entity.Property(e => e.DifLtsvale)
                .HasDefaultValue(0m, "DF_trafico_combustible_dif_ltsvale")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("dif_ltsvale");
            entity.Property(e => e.EcmPath)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("ecm_path");
            entity.Property(e => e.FactorCarga)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("factor_carga");
            entity.Property(e => e.FactorIva)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("factor_iva");
            entity.Property(e => e.FechaAutorizo)
                .HasColumnType("datetime")
                .HasColumnName("fecha_autorizo");
            entity.Property(e => e.FechaCancelo)
                .HasColumnType("datetime")
                .HasColumnName("fecha_cancelo");
            entity.Property(e => e.FechaCarga).HasColumnType("datetime");
            entity.Property(e => e.FechaContabilizado)
                .HasColumnType("datetime")
                .HasColumnName("fecha_contabilizado");
            entity.Property(e => e.FechaDoc)
                .HasColumnType("datetime")
                .HasColumnName("fecha_doc");
            entity.Property(e => e.FechaIngreso)
                .HasColumnType("datetime")
                .HasColumnName("fecha_ingreso");
            entity.Property(e => e.FechaModifico)
                .HasColumnType("datetime")
                .HasColumnName("fecha_modifico");
            entity.Property(e => e.FechaRegistro)
                .HasColumnType("datetime")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FolioEnergex)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("folio_energex");
            entity.Property(e => e.Foraneo).HasColumnName("foraneo");
            entity.Property(e => e.GearDownKm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("gear_down_km");
            entity.Property(e => e.HrsBajaH)
                .HasDefaultValue(0, "DF_trafico_combustible_hrs_baja_h")
                .HasColumnName("hrs_baja_h");
            entity.Property(e => e.HrsBajaM)
                .HasDefaultValue(0, "DF_trafico_combustible_hrs_baja_m")
                .HasColumnName("hrs_baja_m");
            entity.Property(e => e.IdArea).HasColumnName("id_area");
            entity.Property(e => e.IdAreaCaptura).HasColumnName("id_area_captura");
            entity.Property(e => e.IdAreaPreliq)
                .HasDefaultValue(0, "DF_trafico_combustible_id_area_preliq")
                .HasColumnName("id_area_preliq");
            entity.Property(e => e.IdAreaviaje).HasColumnName("id_areaviaje");
            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.IdAutorizo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("id_autorizo");
            entity.Property(e => e.IdCancelo)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("id_cancelo");
            entity.Property(e => e.IdCombfact).HasColumnName("id_combfact");
            entity.Property(e => e.IdCompania).HasColumnName("id_compania");
            entity.Property(e => e.IdConciliacion).HasColumnName("id_conciliacion");
            entity.Property(e => e.IdGasolinera).HasColumnName("id_gasolinera");
            entity.Property(e => e.IdIngreso)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("id_ingreso");
            entity.Property(e => e.IdIngresoValext)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("", "DF_trafico_combustible_id_ingreso_valext")
                .HasColumnName("id_ingreso_valext");
            entity.Property(e => e.IdIva).HasColumnName("id_iva");
            entity.Property(e => e.IdModifico)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("id_modifico");
            entity.Property(e => e.IdMultiEmpresa)
                .HasDefaultValue(0, "DF_trafico_combustible_id_multi_empresa")
                .HasColumnName("id_multi_empresa");
            entity.Property(e => e.IdOperador).HasColumnName("id_operador");
            entity.Property(e => e.IdPago).HasColumnName("id_pago");
            entity.Property(e => e.IdPlazaorigen).HasColumnName("id_plazaorigen");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdUnidad)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("id_unidad");
            entity.Property(e => e.Impreso).HasColumnName("impreso");
            entity.Property(e => e.Iva)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("iva");
            entity.Property(e => e.IvaFactura)
                .HasColumnType("decimal(16, 2)")
                .HasColumnName("iva_factura");
            entity.Property(e => e.KmRecEcm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("km_rec_ecm");
            entity.Property(e => e.KmsActuales).HasColumnName("kms_actuales");
            entity.Property(e => e.KmsActualesvale).HasColumnName("kms_actualesvale");
            entity.Property(e => e.KmsFin).HasColumnName("kms_fin");
            entity.Property(e => e.KmsFinales).HasColumnName("kms_finales");
            entity.Property(e => e.KmsHorometro).HasColumnName("kms_horometro");
            entity.Property(e => e.KmsIni).HasColumnName("kms_ini");
            entity.Property(e => e.KmsRecorridosvale).HasColumnName("kms_recorridosvale");
            entity.Property(e => e.KmsUnidad)
                .HasDefaultValue(0, "DF_kms_unidad")
                .HasColumnName("kms_unidad");
            entity.Property(e => e.LitrosAutorizados)
                .HasColumnType("numeric(18, 6)")
                .HasColumnName("litros_autorizados");
            entity.Property(e => e.LitrosManejando)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("litros_manejando");
            entity.Property(e => e.LocalForaneo).HasColumnName("local_foraneo");
            entity.Property(e => e.LtConsEcm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("lt_cons_ecm");
            entity.Property(e => e.LtRelanti)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("lt_relanti");
            entity.Property(e => e.LtsBaja)
                .HasDefaultValue(0m, "DF_trafico_combustible_lts_baja")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("lts_baja");
            entity.Property(e => e.LtsCarga)
                .HasDefaultValue(0m, "DF_trafico_combustible_lts_carga")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("lts_carga");
            entity.Property(e => e.LtsForaneos)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("lts_foraneos");
            entity.Property(e => e.LtsReales)
                .HasDefaultValue(0m, "DF_trafico_combustible_lts_reales")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("lts_reales");
            entity.Property(e => e.LtsTolerancia)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("lts_tolerancia");
            entity.Property(e => e.MontoComb)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_comb");
            entity.Property(e => e.MontoComb2)
                .HasDefaultValue(0m, "DF_trafico_combustible_monto_comb2")
                .HasColumnType("decimal(18, 9)")
                .HasColumnName("monto_comb2");
            entity.Property(e => e.MontoIeps)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ieps");
            entity.Property(e => e.MontoIepsFactura)
                .HasColumnType("decimal(16, 2)")
                .HasColumnName("monto_ieps_factura");
            entity.Property(e => e.MontoPrecio)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_precio");
            entity.Property(e => e.MontoPrecio2)
                .HasDefaultValue(0m, "DF_trafico_combustible_monto_precio2")
                .HasColumnType("decimal(18, 9)")
                .HasColumnName("monto_precio2");
            entity.Property(e => e.NoConsecutivo)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("no_consecutivo");
            entity.Property(e => e.NoFactura)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("", "DF_trafico_combustible_no_factura")
                .HasColumnName("no_factura");
            entity.Property(e => e.NoLiquidacion).HasColumnName("no_liquidacion");
            entity.Property(e => e.NoPoliza)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("no_poliza");
            entity.Property(e => e.NoPreliq)
                .HasDefaultValue(0, "DF_trafico_combustible_no_preliq")
                .HasColumnName("no_preliq");
            entity.Property(e => e.NoViaje).HasColumnName("no_viaje");
            entity.Property(e => e.NumFact)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("num_fact");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(70)
                .IsUnicode(false)
                .HasColumnName("observaciones");
            entity.Property(e => e.Odometro)
                .HasDefaultValue(0, "DF_trafico_combustible_odometro")
                .HasColumnName("odometro");
            entity.Property(e => e.OdometroCarga)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PorcManejoBaja)
                .HasDefaultValue(0m, "DF_trafico_combustible_porc_manejo_baja")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("porc_manejo_baja");
            entity.Property(e => e.PrecioFactura)
                .HasColumnType("decimal(16, 2)")
                .HasColumnName("precio_factura");
            entity.Property(e => e.PrecioXLto)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("precio_x_lto");
            entity.Property(e => e.PresionPedal)
                .HasDefaultValue(0m, "DF_trafico_combustible_presion_pedal")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("presion_pedal");
            entity.Property(e => e.Pto)
                .HasColumnType("decimal(16, 4)")
                .HasColumnName("pto");
            entity.Property(e => e.RebasaPresupuesto).HasColumnName("rebasa_presupuesto");
            entity.Property(e => e.Recarga).HasColumnName("recarga");
            entity.Property(e => e.ReinicioOdometro)
                .HasDefaultValue(0, "DF_trafico_combustible_reinicio_odometro")
                .HasColumnName("reinicio_odometro");
            entity.Property(e => e.RendBase)
                .HasDefaultValue(0m, "DF_trafico_combustible_rend_base")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("rend_base");
            entity.Property(e => e.RendComputadora)
                .HasDefaultValue(0m, "DF_trafico_combustible_rend_computadora")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("rend_computadora");
            entity.Property(e => e.RendManejo)
                .HasDefaultValue(0m, "DF_trafico_combustible_rend_manejo")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("rend_manejo");
            entity.Property(e => e.RendReal)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("rend_real");
            entity.Property(e => e.RendRealvale)
                .HasDefaultValue(0m, "DF_trafico_combustible_rend_realvale")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("rend_realvale");
            entity.Property(e => e.RpmMax)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("rpm_max");
            entity.Property(e => e.StatusDocto)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("status_docto");
            entity.Property(e => e.SubtotalCombustibleFactura)
                .HasColumnType("decimal(16, 2)")
                .HasColumnName("subtotal_combustible_factura");
            entity.Property(e => e.TiempoManejando)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("tiempo_manejando");
            entity.Property(e => e.TiempoViaje)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("tiempo_viaje");
            entity.Property(e => e.TieneComision)
                .HasDefaultValue(0)
                .HasColumnName("tiene_comision");
            entity.Property(e => e.TipoAutorizacion).HasColumnName("tipo_autorizacion");
            entity.Property(e => e.TipoCarga).HasColumnName("tipo_carga");
            entity.Property(e => e.TipoComb)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tipo_comb");
            entity.Property(e => e.TipoValeConvenio)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("E", "DF_trafico_combustible_tipo_vale_convenio")
                .HasColumnName("tipo_vale_convenio");
            entity.Property(e => e.Tlleno).HasColumnName("tlleno");
            entity.Property(e => e.TopGearKm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("top_gear_km");
            entity.Property(e => e.TotalCombustibleFactura)
                .HasColumnType("decimal(16, 2)")
                .HasColumnName("total_combustible_factura");
            entity.Property(e => e.VelMaxima)
                .HasDefaultValue(0m, "DF_trafico_combustible_vel_maxima")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("vel_maxima");
            entity.Property(e => e.VelProm)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("vel_prom");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
