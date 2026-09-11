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

    public virtual DbSet<TraficoGuium> TraficoGuia { get; set; }

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

        modelBuilder.Entity<TraficoGuium>(entity =>
        {
            entity.HasKey(e => new { e.IdArea, e.NoGuia }).HasFillFactor(80);

            entity.ToTable("trafico_guia", tb =>
                {
                    tb.HasTrigger("t_iguala_remisiones");
                    tb.HasTrigger("t_libera_remisiones_apis");
                    tb.HasTrigger("t_trafico_guia02");
                    tb.HasTrigger("t_trafico_guia_cancsustguia");
                    tb.HasTrigger("t_trafico_guia_ff01");
                    tb.HasTrigger("t_trafico_guia_updateRetencion");
                    tb.HasTrigger("t_valida_dollies_remolques_guia");
                    tb.HasTrigger("tr_actualizarUnidadCP");
                    tb.HasTrigger("tr_trafico_guia_01");
                    tb.HasTrigger("trafico_guia_idretencion");
                });

            entity.HasIndex(e => new { e.IdArea, e.NoViaje }, "IDX_trafico_guia01").HasFillFactor(80);

            entity.HasIndex(e => e.TipoOrigen, "IDX_trafico_guia_SeguimientoCartasPorte").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdArea, e.NoGuia, e.StatusGuia, e.NoViaje, e.NumGuia, e.IdUnidad, e.NoRemision }, "NonClusteredIndex-20230105-163436").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdArea, e.NoGuia, e.StatusGuia, e.NoViaje, e.NumGuia, e.NoRemision }, "NonClusteredIndex-20230106-165732").HasFillFactor(80);

            entity.HasIndex(e => e.NoRemision, "NonClusteredIndex-20230106-171942").HasFillFactor(80);

            entity.HasIndex(e => e.NumGuia, "XAK1trafguianumguia")
                .IsUnique()
                .HasFillFactor(80);

            entity.HasIndex(e => new { e.FechaCancelacion, e.FechaGuia }, "idx_feccanc_fecguia").HasFillFactor(80);

            entity.HasIndex(e => new { e.FechaGuia, e.FechaCancelacion, e.FechaContabilizado }, "idx_fecguia_feccanc_feccontab").HasFillFactor(80);

            entity.HasIndex(e => e.TipoDoc, "idx_tipodoc").HasFillFactor(80);

            entity.HasIndex(e => new { e.TipoDoc, e.FechaGuia, e.NoPoliza }, "idx_tipodoc_fecguia_nopoliza").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdCliente, e.TipoDoc, e.NumGuiaAsignado, e.StatusGuia, e.NumGuia }, "idx_trafico_guia_id_cliente_tipo_doc_num_guia_asignado_status_guia_num_guia").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdCliente, e.TipoDoc, e.StatusGuia, e.NumGuia }, "idx_trafico_guia_id_cliente_tipo_doc_status_guia_num_guia").HasFillFactor(80);

            entity.HasIndex(e => e.NumGuiaAsignado, "idx_trafico_guia_num_guia_asignado").HasFillFactor(80);

            entity.HasIndex(e => new { e.TipoDoc, e.TipoOrigen, e.StatusGuia, e.FechaGuia }, "idx_trafico_guia_tipo_doc_origen").HasFillFactor(80);

            entity.HasIndex(e => new { e.TipoDoc, e.StatusGuia }, "idx_trafico_guia_tipo_doc_status_guia_include").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdArea, e.TipoOrigen, e.FechaGuia, e.TipoDoc }, "trafico_guia_01").HasFillFactor(80);

            entity.HasIndex(e => new { e.TipoDoc, e.FechaGuia, e.Prestamo }, "trafico_guia_tipodoc_fechaguia_prestamo").HasFillFactor(80);

            entity.HasIndex(e => new { e.IdArea, e.NoViaje }, "xfk_trafviajeguia").HasFillFactor(80);

            entity.Property(e => e.IdArea).HasColumnName("id_area");
            entity.Property(e => e.NoGuia).HasColumnName("no_guia");
            entity.Property(e => e.Autopistas)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("autopistas");
            entity.Property(e => e.BanderaFletefracion)
                .HasDefaultValueSql("('0')", "DF_trafico_guia_bandera_fletefracion")
                .HasColumnName("bandera_fletefracion");
            entity.Property(e => e.Bl)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("bl");
            entity.Property(e => e.Campo1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo1");
            entity.Property(e => e.Campo2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo2");
            entity.Property(e => e.Campo3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo3");
            entity.Property(e => e.Campo4)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo4");
            entity.Property(e => e.Campo5)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo5");
            entity.Property(e => e.Campo6)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo6");
            entity.Property(e => e.Campo7)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("campo7");
            entity.Property(e => e.Campo8)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("campo8");
            entity.Property(e => e.Campo9)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("campo9");
            entity.Property(e => e.CantMovguia).HasColumnName("cant_movguia");
            entity.Property(e => e.Castigo)
                .HasDefaultValue(0, "DF_trafico_guia_castigo")
                .HasColumnName("castigo");
            entity.Property(e => e.CedulaPago)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("cedula_pago");
            entity.Property(e => e.ClasificacionDoc).HasColumnName("clasificacion_doc");
            entity.Property(e => e.ClavePorteador)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("clave_porteador");
            entity.Property(e => e.CobroViajeKms)
                .HasColumnType("decimal(18, 10)")
                .HasColumnName("cobro_viaje_kms");
            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("codigo");
            entity.Property(e => e.ConducirA).HasColumnName("conducir_a");
            entity.Property(e => e.ConducirDe).HasColumnName("conducir_de");
            entity.Property(e => e.ContratoEstimacion)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("contrato_estimacion");
            entity.Property(e => e.ContratoFechaFin)
                .HasColumnType("datetime")
                .HasColumnName("contrato_fecha_fin");
            entity.Property(e => e.ContratoFechaIni)
                .HasColumnType("datetime")
                .HasColumnName("contrato_fecha_ini");
            entity.Property(e => e.ContratoNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("contrato_no");
            entity.Property(e => e.ControlPago)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("control_pago");
            entity.Property(e => e.ConvOperador)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("conv_operador");
            entity.Property(e => e.ConvPermisionario)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("conv_permisionario");
            entity.Property(e => e.ConvPermisionarioorigen)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("conv_permisionarioorigen");
            entity.Property(e => e.ConvenidoTonelada)
                .HasColumnType("decimal(18, 10)")
                .HasColumnName("convenido_tonelada");
            entity.Property(e => e.Cpac)
                .HasDefaultValue(0m, "DF_trafico_guia_cpac")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("cpac");
            entity.Property(e => e.Cruces)
                .HasDefaultValue(0m, "DF_trafico_guia_cruces")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("cruces");
            entity.Property(e => e.DescFlete)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("desc_flete");
            entity.Property(e => e.EntregarEn)
                .HasMaxLength(240)
                .IsUnicode(false)
                .HasColumnName("entregar_en");
            entity.Property(e => e.FCarfinProg)
                .HasColumnType("datetime")
                .HasColumnName("f_carfin_prog");
            entity.Property(e => e.FactorCpac)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("factor_cpac");
            entity.Property(e => e.FactorIva)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("factor_iva");
            entity.Property(e => e.Facturado).HasColumnName("facturado");
            entity.Property(e => e.FechaCancelacion)
                .HasColumnType("datetime")
                .HasColumnName("fecha_cancelacion");
            entity.Property(e => e.FechaConfirmacion)
                .HasColumnType("datetime")
                .HasColumnName("fecha_confirmacion");
            entity.Property(e => e.FechaContabilizado)
                .HasColumnType("datetime")
                .HasColumnName("fecha_contabilizado");
            entity.Property(e => e.FechaDocumentador)
                .HasColumnType("datetime")
                .HasColumnName("fecha_documentador");
            entity.Property(e => e.FechaFacturacion)
                .HasColumnType("datetime")
                .HasColumnName("fecha_facturacion");
            entity.Property(e => e.FechaGuia)
                .HasColumnType("datetime")
                .HasColumnName("fecha_guia");
            entity.Property(e => e.FechaIngreso)
                .HasColumnType("datetime")
                .HasColumnName("fecha_ingreso");
            entity.Property(e => e.FechaModifico)
                .HasColumnType("datetime")
                .HasColumnName("fecha_modifico");
            entity.Property(e => e.FechaPago)
                .HasColumnType("datetime")
                .HasColumnName("fecha_pago");
            entity.Property(e => e.FechaVencimiento)
                .HasColumnType("datetime")
                .HasColumnName("fecha_vencimiento");
            entity.Property(e => e.Flete)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("flete");
            entity.Property(e => e.FleteBruto)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("flete_bruto");
            entity.Property(e => e.IdAduanal).HasColumnName("id_aduanal");
            entity.Property(e => e.IdAreaFacturacion).HasColumnName("id_area_facturacion");
            entity.Property(e => e.IdAreaViaje)
                .HasDefaultValue(0, "DF_trafico_guia_id_area_viaje")
                .HasColumnName("id_area_viaje");
            entity.Property(e => e.IdAreaconvenio).HasColumnName("id_areaconvenio");
            entity.Property(e => e.IdAreapagoperm).HasColumnName("id_areapagoperm");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdCompania).HasColumnName("id_compania");
            entity.Property(e => e.IdCondujo).HasColumnName("id_condujo");
            entity.Property(e => e.IdContrato).HasColumnName("id_contrato");
            entity.Property(e => e.IdConvenio).HasColumnName("id_convenio");
            entity.Property(e => e.IdDepositoFactoraje).HasColumnName("id_deposito_factoraje");
            entity.Property(e => e.IdDestinatario).HasColumnName("id_destinatario");
            entity.Property(e => e.IdDestino).HasColumnName("id_destino");
            entity.Property(e => e.IdFactura).HasColumnName("id_factura");
            entity.Property(e => e.IdFraccion).HasColumnName("id_fraccion");
            entity.Property(e => e.IdIngreso)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("id_ingreso");
            entity.Property(e => e.IdIva).HasColumnName("id_iva");
            entity.Property(e => e.IdLinearem1)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("id_linearem1");
            entity.Property(e => e.IdLinearem2)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("id_linearem2");
            entity.Property(e => e.IdModifico)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id_modifico");
            entity.Property(e => e.IdMonedaPermisionario).HasColumnName("id_moneda_permisionario");
            entity.Property(e => e.IdMultiEmpresa).HasColumnName("id_multi_empresa");
            entity.Property(e => e.IdOperador2).HasColumnName("id_operador2");
            entity.Property(e => e.IdOrigen).HasColumnName("id_origen");
            entity.Property(e => e.IdPago).HasColumnName("id_pago");
            entity.Property(e => e.IdPersonal).HasColumnName("id_personal");
            entity.Property(e => e.IdRemitente).HasColumnName("id_remitente");
            entity.Property(e => e.IdRemolque1)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("id_remolque1");
            entity.Property(e => e.IdRemolque2)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("id_remolque2");
            entity.Property(e => e.IdRetencion).HasColumnName("id_retencion");
            entity.Property(e => e.IdSector).HasColumnName("id_sector");
            entity.Property(e => e.IdSeguro).HasColumnName("id_seguro");
            entity.Property(e => e.IdSerieguia).HasColumnName("id_serieguia");
            entity.Property(e => e.IdSerieguia2).HasColumnName("id_serieguia2");
            entity.Property(e => e.IdSubsector).HasColumnName("id_subsector");
            entity.Property(e => e.IdTercero).HasColumnName("id_tercero");
            entity.Property(e => e.IdTipoFactura)
                .HasDefaultValue(0, "DF_trafico_guia_id_tipo_factura")
                .HasColumnName("id_tipo_factura");
            entity.Property(e => e.IdTipoMoneda).HasColumnName("id_tipo_moneda");
            entity.Property(e => e.IdTipoOperacion).HasColumnName("id_tipo_operacion");
            entity.Property(e => e.IdUnidad)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("id_unidad");
            entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");
            entity.Property(e => e.Ieps)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("ieps");
            entity.Property(e => e.Incentivo).HasColumnName("incentivo");
            entity.Property(e => e.IvaGuia)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("iva_guia");
            entity.Property(e => e.KmsConvenio).HasColumnName("kms_convenio");
            entity.Property(e => e.KmsCpac).HasColumnName("kms_cpac");
            entity.Property(e => e.KmsGuia).HasColumnName("kms_guia");
            entity.Property(e => e.LocalForaneo).HasColumnName("local_foraneo");
            entity.Property(e => e.Maniobras)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("maniobras");
            entity.Property(e => e.Medida)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("medida");
            entity.Property(e => e.MontoAplicadoCxp)
                .HasDefaultValue(0m, "DF_trafico_guia_monto_aplicado_cxp")
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_aplicado_cxp");
            entity.Property(e => e.MontoComisiontercero)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_comisiontercero");
            entity.Property(e => e.MontoDescto)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_descto");
            entity.Property(e => e.MontoFaltante)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_faltante");
            entity.Property(e => e.MontoIvadescto)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ivadescto");
            entity.Property(e => e.MontoIvaflete)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ivaflete");
            entity.Property(e => e.MontoIvancargo)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ivancargo");
            entity.Property(e => e.MontoIvancredito)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ivancredito");
            entity.Property(e => e.MontoNcargo)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ncargo");
            entity.Property(e => e.MontoNcredito)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_ncredito");
            entity.Property(e => e.MontoPago)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_pago");
            entity.Property(e => e.MontoRetdescto)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_retdescto");
            entity.Property(e => e.MontoRetencion)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_retencion");
            entity.Property(e => e.MontoRetenciontercero)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_retenciontercero");
            entity.Property(e => e.MontoTipoCambio)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_tipo_cambio");
            entity.Property(e => e.MontoTipoCambioDlls)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("monto_tipo_cambio_dlls");
            entity.Property(e => e.MotivoCancelacion)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("motivo_cancelacion");
            entity.Property(e => e.NoCarta)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("no_carta");
            entity.Property(e => e.NoDeposito).HasColumnName("no_deposito");
            entity.Property(e => e.NoKit)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("no_kit");
            entity.Property(e => e.NoPoliza)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("no_poliza");
            entity.Property(e => e.NoRemision)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("no_remision");
            entity.Property(e => e.NoTransferencia).HasColumnName("no_transferencia");
            entity.Property(e => e.NoTransferenciaCobranza).HasColumnName("no_transferencia_cobranza");
            entity.Property(e => e.NoViaje).HasColumnName("no_viaje");
            entity.Property(e => e.NumGuia)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("num_guia");
            entity.Property(e => e.NumGuiaAsignado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("num_guia_asignado");
            entity.Property(e => e.NumGuiaIncentivo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("num_guia_incentivo");
            entity.Property(e => e.NumGuiacancel)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("num_guiacancel");
            entity.Property(e => e.NumOrden)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("num_orden");
            entity.Property(e => e.NumVapor)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("num_vapor");
            entity.Property(e => e.NumeroContrato)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("numero_contrato");
            entity.Property(e => e.NumeroRelacion)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("numero_relacion");
            entity.Property(e => e.ObsCobranza)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("obs_cobranza");
            entity.Property(e => e.ObservacionesGuia)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("observaciones_guia");
            entity.Property(e => e.OrdenFact).HasColumnName("orden_fact");
            entity.Property(e => e.Otros)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("otros");
            entity.Property(e => e.PagoParcialidades).HasColumnName("pago_parcialidades");
            entity.Property(e => e.Pedimento)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("pedimento");
            entity.Property(e => e.PeriodoFacturacion).HasColumnName("periodo_facturacion");
            entity.Property(e => e.Personalnombre)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("personalnombre");
            entity.Property(e => e.PlazaEmision).HasColumnName("plaza_emision");
            entity.Property(e => e.Prestamo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("prestamo");
            entity.Property(e => e.RecogerEn)
                .HasMaxLength(240)
                .IsUnicode(false)
                .HasColumnName("recoger_en");
            entity.Property(e => e.RegistroEstatal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("registro_estatal");
            entity.Property(e => e.Rem1placa)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("rem1placa");
            entity.Property(e => e.Rem1tipo)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("rem1tipo");
            entity.Property(e => e.Rem2placa)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("rem2placa");
            entity.Property(e => e.Rem2tipo)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("rem2tipo");
            entity.Property(e => e.Seguro)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("seguro");
            entity.Property(e => e.StatusGuia)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("status_guia");
            entity.Property(e => e.StatusPago)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("status_pago");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("subtotal");
            entity.Property(e => e.SubtotalIeps)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("Subtotal_ieps");
            entity.Property(e => e.SucursalPosicion).HasColumnName("sucursal_posicion");
            entity.Property(e => e.SustituidoPor)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sustituido_por");
            entity.Property(e => e.SustituyeDocumento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sustituye_documento");
            entity.Property(e => e.TipoCambioOperador)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("tipo_cambio_operador");
            entity.Property(e => e.TipoCambioPermisionario)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("tipo_cambio_permisionario");
            entity.Property(e => e.TipoCobro)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tipo_cobro");
            entity.Property(e => e.TipoCpac).HasColumnName("tipo_cpac");
            entity.Property(e => e.TipoDetalle).HasColumnName("tipo_detalle");
            entity.Property(e => e.TipoDoc).HasColumnName("tipo_doc");
            entity.Property(e => e.TipoFacturacion)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("tipo_facturacion");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tipo_movimiento");
            entity.Property(e => e.TipoOrigen).HasColumnName("tipo_origen");
            entity.Property(e => e.TipoPago)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tipo_pago");
            entity.Property(e => e.TipoPago02).HasColumnName("tipo_pago02");
            entity.Property(e => e.TipoProducto)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("tipo_producto");
            entity.Property(e => e.TipoServ)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("tipo_serv");
            entity.Property(e => e.TipoViaje).HasColumnName("tipo_viaje");
            entity.Property(e => e.Tipocambioconvenio).HasColumnName("tipocambioconvenio");
            entity.Property(e => e.Unidadplaca)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("unidadplaca");
            entity.Property(e => e.Unidadtipo)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("unidadtipo");
            entity.Property(e => e.ValeCarga)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("vale_carga");
            entity.Property(e => e.ValorDeclarado)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("valor_declarado");
            entity.Property(e => e.ValorUnitario)
                .HasColumnType("decimal(18, 10)")
                .HasColumnName("valor_unitario");
            entity.Property(e => e.Zona)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("zona");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
