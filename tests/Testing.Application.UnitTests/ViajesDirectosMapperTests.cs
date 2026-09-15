using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Etapa D (prototipo paralelo CIS) -- cubre el mapping campo a campo de
/// ViajesDirectosMapper.Mapear, que construye ViajesDto directamente desde
/// DatosCisViajeDirecto sin pasar por el SP ni por el matching de Folio de la Fuente A. Ver
/// docs/PROTOTIPO_FUENTE_CIS.md y docs/ETAPA_D_PROTOTIPO_CIS_CAMBIOS.md.
/// </summary>
public sealed class ViajesDirectosMapperTests
{
    private static DatosCisViajeDirecto FilaCisDirecta(
        string folio = "9000001",
        string? folioComplemento = null,
        int noViaje = 1,
        int noGuia = 500,
        string? factura = "F-1",
        int idArea = 10,
        int idSucursal = 36,
        string sucursal = "MTY",
        string? nombreCorto = "Arca",
        string? region = "Noreste",
        string? nomenclatura = "MTY",
        string trayecto = "IDA",
        DateOnly? fechaCalendario = null,
        string origen = "3001 Guadalupe",
        string destino = "3003 Monterrey",
        string estadoOrigen = "NL",
        string estadoDestino = "NL",
        string ruta = "800052 Origen (CCZ) - Destino (DES) - I",
        string codigoRuta = "COD-1",
        string expedicion = "Paquetería",
        string operador1 = "Juan Perez",
        int idOperador1 = 55,
        string? operador2 = null,
        int? idOperador2 = null,
        string unidad = "U-100",
        string remolque1 = "R-1",
        string? remolque2 = null,
        string? dolly = null,
        int kms = 450,
        decimal totalVenta = 12345.67m,
        int? ejesEquipos = 5,
        decimal montoPeajeIave = 100m,
        decimal montoPeajeEfectivo = 50m,
        string estatusAsignacion = "Entregado") => new(
            Identificador: 1,
            Folio: folio,
            FolioComplemento: folioComplemento,
            NoViaje: noViaje,
            NoGuia: noGuia,
            Factura: factura,
            IdArea: idArea,
            IdSucursal: idSucursal,
            Sucursal: sucursal,
            NombreCorto: nombreCorto,
            Region: region,
            Nomenclatura: nomenclatura,
            Trayecto: trayecto,
            FechaCalendario: fechaCalendario ?? new DateOnly(2026, 9, 3),
            Origen: origen,
            Destino: destino,
            EstadoOrigen: estadoOrigen,
            EstadoDestino: estadoDestino,
            Ruta: ruta,
            CodigoRuta: codigoRuta,
            Expedicion: expedicion,
            Operador1: operador1,
            IdOperador1: idOperador1,
            Operador2: operador2,
            IdOperador2: idOperador2,
            Unidad: unidad,
            Remolque1: remolque1,
            Remolque2: remolque2,
            Dolly: dolly,
            Kms: kms,
            TotalVenta: totalVenta,
            EjesEquipos: ejesEquipos,
            MontoPeajeIave: montoPeajeIave,
            MontoPeajeEfectivo: montoPeajeEfectivo,
            EstatusAsignacion: estatusAsignacion);

    [Fact]
    public void Mapear_KM_viene_directo_de_Kms_sin_transformacion()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(kms: 777));

        Assert.Equal(777m, viaje.kms_viaje);
    }

    [Fact]
    public void Mapear_Venta_viene_de_TotalVenta_no_de_subtotal_factura()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(totalVenta: 999.50m));

        Assert.Equal(999.50m, viaje.cis_total_venta);
        Assert.Null(viaje.subtotal_factura);
    }

    [Fact]
    public void Mapear_Ruta_viene_directo_de_Ruta_y_alimenta_ObtenerTarifa()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(ruta: "800052 C. RUTA X"));

        Assert.Equal("800052 C. RUTA X", viaje.ruta);
        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void Mapear_Unidad_viene_de_Unidad()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(unidad: "U-500"));

        Assert.Equal("U-500", viaje.id_unidad);
    }

    [Fact]
    public void Mapear_Operador_viene_de_Operador1_e_IdOperador1()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(operador1: "Ana Lopez", idOperador1: 42));

        Assert.Equal("Ana Lopez", viaje.operador1);
        Assert.Equal(42, viaje.id_operador1);
    }

    [Theory]
    [InlineData("IDA", "Ida")]
    [InlineData("REGRESO", "Regreso")]
    public void Mapear_Movimiento_se_deriva_de_Trayecto_via_ObtenerMovimiento(string trayectoCrudo, string movimientoEsperado)
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(trayecto: trayectoCrudo));

        Assert.Equal(trayectoCrudo, viaje.cis_trayecto);
        Assert.Equal(movimientoEsperado, CamposDerivadosViajes.ObtenerMovimiento(viaje));
    }

    [Fact]
    public void Mapear_jerarquia_Cliente_Zona_Matriz_viene_del_LEFT_JOIN_a_Sucursales()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(nombreCorto: "ME", region: "Bajio", nomenclatura: "GDL"));

        Assert.Equal("Modelo", CamposDerivadosViajes.ObtenerCliente(viaje)); // ME -> Modelo, misma normalización que la Fuente A
        Assert.Equal("Bajio", CamposDerivadosViajes.ObtenerZona(viaje));
        Assert.Equal("GDL", CamposDerivadosViajes.ObtenerMatriz(viaje));
    }

    [Fact]
    public void Mapear_base_shim_usa_Nomenclatura_igual_que_Matriz()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(nomenclatura: "CCZ"));

        Assert.Equal("CCZ", viaje._base);
        Assert.Equal(viaje._base, CamposDerivadosViajes.ObtenerMatriz(viaje));
    }

    [Fact]
    public void Mapear_Armado_siempre_null()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta());

        Assert.Null(viaje.armado);
        Assert.Null(CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void Mapear_EstatusAsignacion_no_se_propaga_a_estatus_viaje()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(estatusAsignacion: "Entregado"));

        Assert.Null(viaje.estatus_viaje);
    }

    [Fact]
    public void Mapear_Folio_llena_no_remision_y_cis_folio_por_compatibilidad()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(folio: "5001234567"));

        Assert.Equal("5001234567", viaje.no_remision);
        Assert.Equal("5001234567", viaje.cis_folio);
    }

    [Fact]
    public void Mapear_marca_cis_estado_Encontrado_y_llave_CisDirecto()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta());

        Assert.Equal(EstadoEnriquecimientoCis.Encontrado, viaje.cis_estado);
        Assert.Equal(CamposDerivadosViajes.LlaveCisDirecto, viaje.cis_llave_utilizada);
        Assert.Null(viaje.cis_motivo_no_coincidencia);
    }

    [Fact]
    public void Mapear_Peajes_quedan_poblados_como_candidatos_diagnosticos_no_validados()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(montoPeajeIave: 120m, montoPeajeEfectivo: 30m));

        // Documentado como NO reconciliado contra el SP (docs/VALIDACION_SP_VS_CIS.md §14) -- esta
        // prueba solo confirma el mapeo mecánico, no una equivalencia de negocio.
        Assert.Equal(120m, viaje.peaje_electronico);
        Assert.Equal(30m, viaje.peaje_efectivo);
    }

    [Fact]
    public void Mapear_Expedicion_es_passthrough_crudo_el_fallback_se_resuelve_on_demand()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(expedicion: "-"));

        Assert.Equal("-", viaje.expedicion); // el mapper NO resuelve el fallback -- eso lo hace ObtenerExpedicion(v) on-demand
    }

    [Fact]
    public void Mapear_fecha_ingreso_queda_null_y_ObtenerFechaNegocio_usa_cis_fecha_calendario()
    {
        var viaje = ViajesDirectosMapper.Mapear(FilaCisDirecta(fechaCalendario: new DateOnly(2026, 9, 3)));

        Assert.Null(viaje.fecha_ingreso);
        Assert.Equal(new DateOnly(2026, 9, 3), viaje.cis_fecha_calendario);
        Assert.Equal(new DateTime(2026, 9, 3), CamposDerivadosViajes.ObtenerFechaNegocio(viaje));
    }
}