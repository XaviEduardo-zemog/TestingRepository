using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 2 (2026-09-11, integración trafico_guia) -- pruebas de la 2ª fuente de enriquecimiento
/// (fallback): Sucursales por _base, RutasZam por código de ruta, trafico_guia por Factura+NoViaje.
/// Dos grupos: pruebas de <see cref="ViajesEnriquecidoMapper.EnriquecerConFallback"/> directas
/// (más simples, sin pasar por el handler completo), y pruebas de
/// <see cref="GetViajesQueryHandler"/> de punta a punta con fakes de las 3 fuentes.
/// </summary>
public sealed class SegundaFuenteEnrichmentTests
{
    // ---------------------------------------------------------------------------------------
    // Grupo 1: ViajesEnriquecidoMapper.EnriquecerConFallback directo
    // ---------------------------------------------------------------------------------------

    private static SpViajesDto SpBase(string? direccion = "Ida", decimal? subtotalFactura = null, int noViaje = 1) => new()
    {
        _base = "CUL",
        no_viaje = noViaje,
        estatus_viaje = "Liquidado",
        no_remision = "999999999", // ya falló en CIS -- por eso llegamos al fallback
        ruta = "32203931 P. Santa Rita - Planta Guadiana - R",
        direccion = direccion,
        factura = "FCUL-33155",
        subtotal_factura = subtotalFactura,
    };

    private static DatosSucursalFallback SucursalArca() => new(Cliente: "Arca", Zona: "Pacifico", Matriz: "CUL", IdSucursal: 12, Sucursal: "Culiacan");

    private static DatosRutaFallback RutaEjemplo() => new(Codigo: "32203931", Ruta: "32203931 P. Santa Rita - Planta Guadiana - R", Origen: "Santa Rita", Destino: "Planta Guadiana", Kms: 340m);

    private static DatosTraficoGuiaFallback GuiaConSubtotal(decimal subtotal, int noViaje = 1) => new(
        NumGuia: "FCUL-33155", NoViaje: noViaje, Subtotal: subtotal, KmsGuia: 340,
        NoRemision: "5000852364", FechaGuia: new DateTime(2026, 2, 23), StatusGuia: "C", IdArea: 1, NoGuia: 68752);

    [Fact]
    public void Cliente_Arca_se_resuelve_desde_Sucursales_por_base()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), null, GuiaConSubtotal(1000m), null);

        Assert.Equal(EstadoEnriquecimientoCis.EncontradoFallback, viaje.cis_estado);
        Assert.Equal("Arca", CamposDerivadosViajes.ObtenerCliente(viaje));
    }

    [Theory]
    [InlineData("ME")]
    [InlineData("MD")]
    public void Cliente_ME_MD_se_normaliza_a_Modelo_igual_que_por_CIS(string clienteCrudo)
    {
        // Sucursales.NombreCorto trae "ME"/"MD" crudo, igual que en la ruta CIS -- la
        // normalización vive en CamposDerivadosViajes.ObtenerCliente y es transparente al origen
        // del enriquecimiento (CIS o fallback).
        var sucursal = new DatosSucursalFallback(Cliente: clienteCrudo, Zona: "Occidente", Matriz: "CCZ", IdSucursal: 5, Sucursal: "Zacatecas");
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), sucursal, null, GuiaConSubtotal(1000m), null);

        Assert.Equal("Modelo", CamposDerivadosViajes.ObtenerCliente(viaje));
        Assert.Equal(clienteCrudo, CamposDerivadosViajes.ObtenerClienteOriginal(viaje));
    }

    [Fact]
    public void Matriz_CUL_se_resuelve_desde_Sucursales_por_base()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), null, GuiaConSubtotal(1000m), null);

        Assert.Equal("CUL", CamposDerivadosViajes.ObtenerMatriz(viaje));
    }

    [Fact]
    public void Origen_y_Destino_se_resuelven_desde_RutasZam()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(1000m), null);

        Assert.Equal("Santa Rita", CamposDerivadosViajes.ObtenerOrigen(viaje));
        Assert.Equal("Planta Guadiana", CamposDerivadosViajes.ObtenerDestino(viaje));
    }

    [Fact]
    public void Venta_se_resuelve_desde_trafico_guia_Subtotal()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(43280.50m), null);

        Assert.Equal(43280.50m, viaje.cis_total_venta);
        Assert.Equal(43280.50m, ContribucionViajeProyectada.Venta(viaje, corte: null));
    }

    [Fact]
    public void Venta_de_1_00_se_conserva_sin_convertirse_a_cero()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(1.00m), null);

        Assert.Equal(1.00m, viaje.cis_total_venta);
        Assert.Equal(1.00m, ContribucionViajeProyectada.Venta(viaje, corte: null));
    }

    [Fact]
    public void Movimiento_Ida_se_usa_para_contabilizar_Viajes()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(direccion: "Ida"), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(1000m), null);

        Assert.Equal("Ida", viaje.cis_trayecto);
        Assert.Equal(1m, ContribucionViajeProyectada.Viajes(viaje, corte: null));
    }

    [Fact]
    public void Direccion_Tramo_no_se_fuerza_a_Ida_ni_Regreso()
    {
        // Confirmado con datos reales antes de implementar: "Tramo" existe en el SP y NUNCA debe
        // interpretarse como Ida/Regreso -- ver ArbolJerarquiaViajes/CCZ CTO en fases anteriores.
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(direccion: "Tramo"), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(1000m), null);

        Assert.Null(viaje.cis_trayecto);
        Assert.Equal(0m, ContribucionViajeProyectada.Viajes(viaje, corte: null));
    }

    [Fact]
    public void Subtotal_factura_nunca_sustituye_la_venta_de_trafico_guia_cuando_no_hay_guia()
    {
        // guia=null (no se encontró en trafico_guia) -- aunque subtotal_factura tenga un valor,
        // la fila queda NoEncontrado y cis_total_venta en null. Nunca se usa subtotal_factura
        // como sustituto.
        var sp = SpBase(subtotalFactura: 999999m);
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(sp, SucursalArca(), RutaEjemplo(), guia: null, motivoNoEncontradoOriginal: "motivo original");

        Assert.Equal(EstadoEnriquecimientoCis.NoEncontrado, viaje.cis_estado);
        Assert.Null(viaje.cis_total_venta);
        Assert.Equal("motivo original", viaje.cis_motivo_no_coincidencia);
    }

    [Fact]
    public void Subtotal_factura_nunca_sustituye_la_venta_de_trafico_guia_cuando_difieren()
    {
        // guia SÍ se encontró, con un Subtotal distinto de subtotal_factura -- debe prevalecer
        // SIEMPRE el de trafico_guia.
        var sp = SpBase(subtotalFactura: 999999m);
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(sp, SucursalArca(), RutaEjemplo(), GuiaConSubtotal(4321m), null);

        Assert.Equal(4321m, viaje.cis_total_venta);
        Assert.NotEqual(999999m, viaje.cis_total_venta);
    }

    [Fact]
    public void Fuente_de_enriquecimiento_distingue_fallback_de_CIS()
    {
        var viaje = ViajesEnriquecidoMapper.EnriquecerConFallback(SpBase(), SucursalArca(), RutaEjemplo(), GuiaConSubtotal(1000m), null);

        Assert.Equal(EstadoEnriquecimientoCis.EncontradoFallback, viaje.cis_estado);
        Assert.NotEqual(EstadoEnriquecimientoCis.Encontrado, viaje.cis_estado);
        Assert.Equal("Fallback:Sucursales+RutasZam+TraficoGuia", viaje.cis_fuente_enriquecimiento);
    }

    // ---------------------------------------------------------------------------------------
    // Grupo 2: GetViajesQueryHandler de punta a punta, con las 3 fuentes fakeadas
    // ---------------------------------------------------------------------------------------

    private static GetViajesQuery ConsultaDePrueba() => new(
        FechaInicio: new DateTime(2026, 1, 1),
        FechaFin: new DateTime(2026, 9, 11),
        TipoFecha: "ingreso",
        Areas: null, IdUnidad: null, Estados: null, IdRuta: null, IdOperador: null, NoRemision: null);

    private static SpViajesDto ViajePendiente(string? factura = "FCUL-33155", int noViaje = 1, string? ruta = "32203931 P. Santa Rita - Planta Guadiana - R", string? direccion = "Ida") => new()
    {
        _base = "CUL",
        no_viaje = noViaje,
        estatus_viaje = "Liquidado",
        no_remision = "999999999", // no existe en CIS -- NoEncontrado, candidato a fallback
        ruta = ruta,
        direccion = direccion,
        factura = factura,
    };

    [Fact]
    public async Task Registro_CIS_encontrado_no_se_toca_y_no_consulta_la_2da_fuente()
    {
        var filas = new List<SpViajesDto> { new() { _base = "MTY", no_viaje = 1, estatus_viaje = "Liquidado", no_remision = "4292946", ruta = "x", direccion = "Ida" } };
        var porFolio = new Dictionary<string, DatosCisViaje>
        {
            ["4292946"] = new("4292946", null, 36, "MTY", "Arca", "Noreste", "MTY", "3001 Guadalupe", "3003 Monterrey", "NL", "NL", 7546.35m, 5, new DateOnly(2026, 7, 1), "I", "Arca"),
        };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(new CisEnrichmentBatchResult(porFolio, new HashSet<string>()));
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.Encontrado, viaje.cis_estado);
        Assert.Equal("CIS_DB", viaje.cis_fuente_enriquecimiento);
        Assert.Null(segundaFuente.UltimasFacturasRecibidas); // nunca se llamó -- nada quedó pendiente
    }

    [Fact]
    public async Task Registro_pendiente_se_recupera_por_base_y_trafico_guia()
    {
        var filas = new List<SpViajesDto> { ViajePendiente() };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);

        var resultadoSegunda = new SegundaFuenteBatchResult(
            PorBase: new Dictionary<string, DatosSucursalFallback> { ["CUL"] = SucursalArca() },
            PorCodigoRuta: new Dictionary<string, DatosRutaFallback> { ["32203931"] = RutaEjemplo() },
            RutasCodigosAmbiguos: new HashSet<string>(),
            PorFactura: new Dictionary<string, DatosTraficoGuiaFallback> { ["FCUL-33155"] = GuiaConSubtotal(4321m) });
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(resultadoSegunda);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.EncontradoFallback, viaje.cis_estado);
        Assert.Equal("Arca", viaje.cis_cliente);
        Assert.Equal(4321m, viaje.cis_total_venta);
    }

    [Fact]
    public async Task Busqueda_exacta_por_Factura_mas_NoViaje_confirma_el_match()
    {
        var filas = new List<SpViajesDto> { ViajePendiente(factura: "FCUL-33155", noViaje: 29871) };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var resultadoSegunda = new SegundaFuenteBatchResult(
            new Dictionary<string, DatosSucursalFallback>(),
            new Dictionary<string, DatosRutaFallback>(),
            new HashSet<string>(),
            new Dictionary<string, DatosTraficoGuiaFallback> { ["FCUL-33155"] = GuiaConSubtotal(4321m, noViaje: 29871) });
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(resultadoSegunda);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.EncontradoFallback, viaje.cis_estado);
    }

    [Fact]
    public async Task NoViaje_solo_nunca_es_suficiente_si_la_Factura_no_coincide_con_ese_NoViaje()
    {
        // trafico_guia SÍ tiene la Factura, pero con un NoViaje DISTINTO al de esta fila del SP
        // -- el match debe rechazarse (nunca se acepta por NoViaje solo).
        var filas = new List<SpViajesDto> { ViajePendiente(factura: "FCUL-33155", noViaje: 29871) };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var resultadoSegunda = new SegundaFuenteBatchResult(
            new Dictionary<string, DatosSucursalFallback>(),
            new Dictionary<string, DatosRutaFallback>(),
            new HashSet<string>(),
            new Dictionary<string, DatosTraficoGuiaFallback> { ["FCUL-33155"] = GuiaConSubtotal(4321m, noViaje: 99999) }); // NoViaje distinto
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(resultadoSegunda);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.Empty(resultado.Value); // no se resolvió -- queda excluido, no entra silenciosamente
    }

    [Fact]
    public async Task Factura_nula_no_consulta_trafico_guia()
    {
        var filas = new List<SpViajesDto> { ViajePendiente(factura: null) };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.NotNull(segundaFuente.UltimasFacturasRecibidas);
        Assert.Empty(segundaFuente.UltimasFacturasRecibidas!);
    }

    [Fact]
    public async Task Multiples_coincidencias_de_RutasZam_no_usan_First_silenciosamente()
    {
        // El código de ruta viene marcado como ambiguo (2+ filas en RutasZam) -- el handler NUNCA
        // debe tomar una al azar; Origen/Destino deben quedar null, nunca inventados.
        var filas = new List<SpViajesDto> { ViajePendiente() };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var resultadoSegunda = new SegundaFuenteBatchResult(
            new Dictionary<string, DatosSucursalFallback> { ["CUL"] = SucursalArca() },
            PorCodigoRuta: new Dictionary<string, DatosRutaFallback>(), // "32203931" NO está aquí -- es ambiguo
            RutasCodigosAmbiguos: new HashSet<string> { "32203931" },
            PorFactura: new Dictionary<string, DatosTraficoGuiaFallback> { ["FCUL-33155"] = GuiaConSubtotal(4321m) });
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(resultadoSegunda);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.EncontradoFallback, viaje.cis_estado); // Venta sí se resolvió
        Assert.Null(viaje.cis_origen); // pero la ruta ambigua nunca se adivina
        Assert.Null(viaje.cis_destino);
    }

    // ---------------------------------------------------------------------------------------
    // Fakes -- sin base de datos real, sin mocks de terceros.
    // ---------------------------------------------------------------------------------------

    private sealed class FakeApplicationDbContext(IReadOnlyList<SpViajesDto> filas) : IApplicationDbContext
    {
        public Task<IReadOnlyList<TResult>> QueryAsync<TResult>(string sql, IReadOnlyCollection<QueryParameter> parameters, CancellationToken cancellationToken = default) where TResult : class
        {
            return Task.FromResult((IReadOnlyList<TResult>)(object)filas);
        }
    }

    private sealed class FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult resultado) : ICisViajeEnrichmentRepository
    {
        public Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> candidatosFolio, CancellationToken cancellationToken = default)
            => Task.FromResult(resultado);
    }

    // Fake privado a este archivo (no compartido con GetViajesQueryHandlerTests.cs) -- cada
    // archivo de pruebas tiene su propia copia para poder compilar y aplicarse de forma
    // independiente, sin importar el orden en que se copien los archivos del Prompt 2.
    private sealed class FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult resultado) : ISegundaFuenteEnrichmentRepository
    {
        public IReadOnlyCollection<string>? UltimasBasesRecibidas { get; private set; }
        public IReadOnlyCollection<string>? UltimosCodigosRecibidos { get; private set; }
        public IReadOnlyCollection<string>? UltimasFacturasRecibidas { get; private set; }

        public Task<SegundaFuenteBatchResult> ObtenerAsync(
            IReadOnlyCollection<string> basesDistintas,
            IReadOnlyCollection<string> codigosRutaDistintos,
            IReadOnlyCollection<string> facturasDistintas,
            CancellationToken cancellationToken = default)
        {
            UltimasBasesRecibidas = basesDistintas;
            UltimosCodigosRecibidos = codigosRutaDistintos;
            UltimasFacturasRecibidas = facturasDistintas;
            return Task.FromResult(resultado);
        }
    }
}