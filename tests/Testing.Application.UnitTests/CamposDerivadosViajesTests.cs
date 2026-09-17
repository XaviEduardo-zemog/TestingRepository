using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 5 -- cierra un hueco de cobertura detectado en la revisión final: ME/MD → Modelo y el
/// orden Arca-antes-Modelo se habían validado por inspección de código + datos reales (Prompt 4),
/// pero no existía ninguna prueba automatizada dedicada a ellos. Sección final agregada en Etapa D
/// (prototipo paralelo CIS): ObtenerFechaNegocio y ObtenerExpedicion -- ver
/// docs/PROTOTIPO_FUENTE_CIS.md. Sección de Expedición/Tarifa reescrita en
/// docs/CAMBIO_EXPEDICION_TARIFA.md / docs/AJUSTE_TIPO_TARIFA.md. Sección de Ejes/Asignación
/// agregada en docs/FILTRO_EJES_ASIGNACION.md: ClasificarArmado ahora se clasifica desde
/// cis_ejes_equipos (5=Sencillo, 6=Comodato, 9=Full), con fallback a armado por compatibilidad.
/// </summary>
public sealed class CamposDerivadosViajesTests
{
    [Theory]
    [InlineData("ME", "Modelo")]
    [InlineData("MD", "Modelo")]
    [InlineData("me", "Modelo")]
    [InlineData("md", "Modelo")]
    [InlineData("Arca", "Arca")]
    public void NormalizarCliente_agrupa_ME_y_MD_bajo_Modelo_y_conserva_Arca(string crudo, string esperado)
    {
        Assert.Equal(esperado, CamposDerivadosViajes.NormalizarCliente(crudo));
    }

    [Theory]
    [InlineData("ME")]
    [InlineData("MD")]
    public void ObtenerCliente_normaliza_ME_y_MD_a_Modelo(string clienteCrudo)
    {
        var viaje = new ViajesDto { cis_cliente = clienteCrudo };

        Assert.Equal("Modelo", CamposDerivadosViajes.ObtenerCliente(viaje));
    }

    [Fact]
    public void ObtenerCliente_no_produce_ME_MD_y_Modelo_como_clientes_separados()
    {
        // Reproduce el requisito explícito del Prompt 5: no deben coexistir ME, MD y Modelo como
        // 3 valores distintos -- todo pasa por la misma normalización.
        var clientes = new[] { "ME", "MD", "Modelo" }
            .Select(c => CamposDerivadosViajes.ObtenerCliente(new ViajesDto { cis_cliente = c }))
            .Distinct()
            .ToList();

        Assert.Single(clientes);
        Assert.Equal("Modelo", clientes[0]);
    }

    [Theory]
    [InlineData("Arca", 0)]
    [InlineData("Modelo", 1)]
    [InlineData("Otro", 2)]
    [InlineData(null, 2)]
    public void PrioridadCliente_asigna_Arca_0_Modelo_1_otros_2(string? cliente, int prioridadEsperada)
    {
        Assert.Equal(prioridadEsperada, CamposDerivadosViajes.PrioridadCliente(cliente));
    }

    [Fact]
    public void Ordenar_por_PrioridadCliente_pone_Arca_antes_que_Modelo_sin_importar_alfabeto()
    {
        var clientes = new[] { "Zeta", "Modelo", "Arca" };

        var ordenado = clientes.OrderBy(CamposDerivadosViajes.PrioridadCliente).ThenBy(c => c).ToList();

        Assert.Equal(new[] { "Arca", "Modelo", "Zeta" }, ordenado);
    }

    // ---------------------------------------------------------------------------------------
    // Etapa D (prototipo paralelo CIS) -- ObtenerFechaNegocio
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ObtenerFechaNegocio_para_CisDirecto_usa_cis_fecha_calendario()
    {
        var viaje = new ViajesDto
        {
            cis_llave_utilizada = CamposDerivadosViajes.LlaveCisDirecto,
            cis_fecha_calendario = new DateOnly(2026, 9, 3),
            fecha_ingreso = null,
        };

        Assert.Equal(new DateTime(2026, 9, 3), CamposDerivadosViajes.ObtenerFechaNegocio(viaje));
    }

    [Fact]
    public void ObtenerFechaNegocio_para_StoredProcedure_ignora_cis_fecha_calendario_y_usa_fecha_ingreso()
    {
        var viaje = new ViajesDto
        {
            cis_llave_utilizada = "Directo",
            cis_fecha_calendario = new DateOnly(2026, 9, 9),
            fecha_ingreso = "3/9/2026 8:00 AM",
        };

        Assert.Equal(new DateTime(2026, 9, 3, 8, 0, 0), CamposDerivadosViajes.ObtenerFechaNegocio(viaje));
    }

    [Fact]
    public void ObtenerFechaNegocio_sin_fecha_ingreso_ni_llave_CisDirecto_devuelve_null()
    {
        var viaje = new ViajesDto { cis_llave_utilizada = "LimpiarFolio", fecha_ingreso = null };

        Assert.Null(CamposDerivadosViajes.ObtenerFechaNegocio(viaje));
    }

    // ---------------------------------------------------------------------------------------
    // docs/CAMBIO_EXPEDICION_TARIFA.md / docs/AJUSTE_TIPO_TARIFA.md
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("E1", "E1")]
    [InlineData("X1", "X1")]
    [InlineData("X2", "X2")]
    [InlineData("XF", "XF")]
    [InlineData("XG", "XG")]
    public void ObtenerExpedicion_conserva_los_valores_reales_de_CIS_tal_cual(string crudo, string esperado)
    {
        var viaje = new ViajesDto { expedicion = crudo };

        Assert.Equal(esperado, CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_aplica_Trim_a_valores_con_espacios()
    {
        var viaje = new ViajesDto { expedicion = "  XF  " };

        Assert.Equal("XF", CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_null_devuelve_null()
    {
        var viaje = new ViajesDto { expedicion = null };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_vacia_devuelve_null()
    {
        var viaje = new ViajesDto { expedicion = "" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_guion_devuelve_null()
    {
        var viaje = new ViajesDto { expedicion = "-" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_vacia_con_Ruta_C_NO_debe_devolver_Comodato_debe_devolver_null()
    {
        var viaje = new ViajesDto { expedicion = "", ruta = "33083312 C. Chihuahua - Delicias - I" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_vacia_con_Ruta_P_NO_debe_devolver_Propio_debe_devolver_null()
    {
        var viaje = new ViajesDto { expedicion = "", ruta = "33083301 P. Chihuahua - Juarez Chh. - I" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_nunca_consulta_Ruta_incluso_si_Ruta_tiene_codigo_C_o_P()
    {
        var viaje = new ViajesDto { expedicion = "XF", ruta = "33083312 C. Chihuahua - Delicias - I" };

        Assert.Equal("XF", CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_C_devuelve_Comodato()
    {
        var viaje = new ViajesDto { ruta = "33083312 C. Chihuahua - Delicias - I" };

        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_P_devuelve_Propio()
    {
        var viaje = new ViajesDto { ruta = "33083301 P. Chihuahua - Juarez Chh. - I" };

        Assert.Equal("Propio", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_sin_codigo_C_ni_P_devuelve_Viaje()
    {
        var viaje = new ViajesDto { ruta = "800052 XX RUTA TRES" };

        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_null_devuelve_Viaje()
    {
        var viaje = new ViajesDto { ruta = null };

        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_vacia_devuelve_Viaje()
    {
        var viaje = new ViajesDto { ruta = "" };

        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_solo_espacios_devuelve_Viaje()
    {
        var viaje = new ViajesDto { ruta = "   " };

        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_con_Ruta_sin_ningun_espacio_devuelve_Viaje()
    {
        var viaje = new ViajesDto { ruta = "800052CRUTAX" };

        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    [Fact]
    public void ObtenerTarifa_no_usa_Expedicion_para_calcular_el_resultado()
    {
        var conExpedicionXF = new ViajesDto { expedicion = "XF", ruta = "33083312 C. Chihuahua - Delicias - I" };
        var sinExpedicion = new ViajesDto { expedicion = null, ruta = "33083312 C. Chihuahua - Delicias - I" };

        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerTarifa(conExpedicionXF));
        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerTarifa(sinExpedicion));
    }

    [Fact]
    public void ObtenerExpedicion_y_ObtenerTarifa_son_conceptos_independientes_no_se_mezclan()
    {
        var viaje = new ViajesDto { expedicion = "-", ruta = "33083312 C. Chihuahua - Delicias - I" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }

    // ---------------------------------------------------------------------------------------
    // docs/FILTRO_EJES_ASIGNACION.md -- ObtenerEjes (filtro UI) y ClasificarArmado/
    // NormalizarArmadoCrudo (Asignación del Reporte Ejecutivo) desde cis_ejes_equipos.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ObtenerEjes_con_valor_devuelve_texto()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = 5 };

        Assert.Equal("5", CamposDerivadosViajes.ObtenerEjes(viaje));
    }

    [Fact]
    public void ObtenerEjes_sin_valor_devuelve_null()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = null };

        Assert.Null(CamposDerivadosViajes.ObtenerEjes(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_5_devuelve_Sencillo()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = 5 };

        Assert.Equal("Sencillo", CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_6_devuelve_Comodato()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = 6 };

        Assert.Equal("Comodato", CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_9_devuelve_Full()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = 9 };

        Assert.Equal("Full", CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_desconocido_devuelve_null()
    {
        // No se inventa clasificación para otros ejes (p.ej. 7).
        var viaje = new ViajesDto { cis_ejes_equipos = 7 };

        Assert.Null(CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_null_y_armado_FULL_usa_fallback_de_compatibilidad()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = null, armado = "FULL" };

        Assert.Equal("Full", CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void ClasificarArmado_con_ejes_null_y_armado_null_devuelve_null()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = null, armado = null };

        Assert.Null(CamposDerivadosViajes.ClasificarArmado(viaje));
    }

    [Fact]
    public void NormalizarArmadoCrudo_con_ejes_desconocido_devuelve_el_numero_para_diagnostico()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = 7 };

        Assert.Equal("7", CamposDerivadosViajes.NormalizarArmadoCrudo(viaje));
    }

    [Fact]
    public void NormalizarArmadoCrudo_con_ejes_null_cae_a_armado_crudo()
    {
        var viaje = new ViajesDto { cis_ejes_equipos = null, armado = "raro" };

        Assert.Equal("RARO", CamposDerivadosViajes.NormalizarArmadoCrudo(viaje));
    }
}