using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 5 -- cierra un hueco de cobertura detectado en la revisión final: ME/MD → Modelo y el
/// orden Arca-antes-Modelo se habían validado por inspección de código + datos reales (Prompt 4),
/// pero no existía ninguna prueba automatizada dedicada a ellos. Sección final agregada en Etapa D
/// (prototipo paralelo CIS): ObtenerFechaNegocio y ObtenerExpedicion -- ver
/// docs/PROTOTIPO_FUENTE_CIS.md.
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
    // Etapa D (prototipo paralelo CIS) -- ObtenerFechaNegocio y ObtenerExpedicion
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
        // cis_fecha_calendario viene poblado por el enriquecimiento CIS existente de la Fuente A --
        // debe ignorarse ahí también, no solo cuando está ausente. Se usa una fecha deliberadamente
        // distinta a fecha_ingreso para probar que NO se toma cis_fecha_calendario en este camino.
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

    [Fact]
    public void ObtenerExpedicion_devuelve_el_valor_directo_si_existe()
    {
        var viaje = new ViajesDto { expedicion = " Paquetería " };

        Assert.Equal("Paquetería", CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    public void ObtenerExpedicion_con_Ruta_C_devuelve_Comodato_si_Expedicion_no_tiene_valor(string? expedicionCruda)
    {
        var viaje = new ViajesDto { expedicion = expedicionCruda, ruta = "800052 C. RUTA UNO" };

        Assert.Equal("Comodato", CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_con_Ruta_P_devuelve_Propio_si_Expedicion_no_tiene_valor()
    {
        var viaje = new ViajesDto { expedicion = "-", ruta = "800052 P. RUTA DOS" };

        Assert.Equal("Propio", CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_con_Ruta_sin_codigo_C_o_P_devuelve_null()
    {
        var viaje = new ViajesDto { expedicion = "-", ruta = "800052 XX RUTA TRES" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_sin_Expedicion_ni_Ruta_devuelve_null()
    {
        var viaje = new ViajesDto { expedicion = null, ruta = null };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
    }

    [Fact]
    public void ObtenerExpedicion_y_ObtenerTarifa_no_se_mezclan_aunque_compartan_el_fallback_de_Ruta()
    {
        // Misma ruta, dos conceptos distintos: Expedición cae a null en el caso "otro código",
        // Tarifa cae a "Viaje" -- confirma que no son la misma función ni comparten default.
        var viaje = new ViajesDto { expedicion = "-", ruta = "800052 XX RUTA TRES" };

        Assert.Null(CamposDerivadosViajes.ObtenerExpedicion(viaje));
        Assert.Equal("Viaje", CamposDerivadosViajes.ObtenerTarifa(viaje));
    }
}