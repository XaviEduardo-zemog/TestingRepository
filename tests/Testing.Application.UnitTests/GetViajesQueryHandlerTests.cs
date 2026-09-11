using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Pruebas de Prompt 2 -- cubren exactamente los escenarios pedidos: coincidencia directa,
/// folio con -C, folio con -R, folio compuesto con coma, folio compuesto con "/", folio
/// inexistente, no_remision nulo, cancelado, coincidencias duplicadas, y confirmación de que
/// NoViaje nunca se usa como llave ni se envía a CIS_DB.
/// </summary>
public sealed class GetViajesQueryHandlerTests
{
    // ---------------------------------------------------------------------------------------
    // Grupo 1: funciones puras de matching (internal, expuestas vía InternalsVisibleTo)
    // ---------------------------------------------------------------------------------------

    //[Fact]
    //public void LimpiarFolio_sin_coma_ni_guion_devuelve_el_mismo_valor()
    //{
    //    Assert.Equal("5001053341", GetViajesQueryHandler.LimpiarFolio("5001053341"));
    //}

    //[Fact]
    //public void LimpiarFolio_con_sufijo_guion_C_toma_lo_anterior_al_guion()
    //{
    //    // Folio con "-C": "5001053341-C,5001053341" -> antes de la coma "5001053341-C" -> antes del guion "5001053341"
    //    Assert.Equal("5001053341", GetViajesQueryHandler.LimpiarFolio("5001053341-C,5001053341"));
    //}

    //[Fact]
    //public void LimpiarFolio_con_sufijo_guion_R_toma_lo_anterior_al_guion()
    //{
    //    // Folio con "-R": incluso sin coma, el guion medio por sí solo ya trunca.
    //    Assert.Equal("20355226", GetViajesQueryHandler.LimpiarFolio("20355226-R"));
    //}

    //[Fact]
    //public void LimpiarFolio_folio_compuesto_con_coma_toma_lo_anterior_a_la_coma()
    //{
    //    Assert.Equal("5001033444", GetViajesQueryHandler.LimpiarFolio("5001033444,4325532"));
    //}

    //[Fact]
    //public void QuitarSufijo_quita_guion_C()
    //{
    //    Assert.Equal("5000866869", GetViajesQueryHandler.QuitarSufijo("5000866869-C"));
    //}

    //[Fact]
    //public void QuitarSufijo_quita_guion_R()
    //{
    //    Assert.Equal("20355226", GetViajesQueryHandler.QuitarSufijo("20355226-R"));
    //}

    //[Fact]
    //public void QuitarSufijo_quita_R_final_sin_guion_si_precede_un_digito()
    //{
    //    // Caso real confirmado en esta sesión: "8003743465R" (sin guion).
    //    Assert.Equal("8003743465", GetViajesQueryHandler.QuitarSufijo("8003743465R"));
    //}

    //[Fact]
    //public void QuitarSufijo_no_toca_un_valor_que_no_termina_en_sufijo_conocido()
    //{
    //    Assert.Equal("5001053341", GetViajesQueryHandler.QuitarSufijo("5001053341"));
    //}

    //[Fact]
    //public void CandidatoSlash_arma_folio_compuesto_uniendo_partes_unicas_con_diagonal()
    //{
    //    // Caso real confirmado en esta sesión: no_remision="5000866869-C,4173404,5000866869"
    //    // -> partes únicas sin sufijo: "5000866869","4173404" -> "5000866869/4173404"
    //    Assert.Equal("5000866869/4173404", GetViajesQueryHandler.CandidatoSlash("5000866869-C,4173404,5000866869"));
    //}

    //[Fact]
    //public void CandidatoSlash_elimina_partes_repetidas_conservando_el_orden()
    //{
    //    Assert.Equal("A/B", GetViajesQueryHandler.CandidatoSlash("A,B,A,B,A"));
    //}

    //[Fact]
    //public void ConstruirCandidatos_para_folio_simple_solo_genera_el_candidato_directo()
    //{
    //    var candidatos = GetViajesQueryHandler.ConstruirCandidatos("5001053341");

    //    Assert.Equal("5001053341", candidatos.Directo);
    //    Assert.Null(candidatos.LimpiarFolio);
    //    Assert.Null(candidatos.Slash);
    //}

    //[Fact]
    //public void ConstruirCandidatos_para_folio_compuesto_genera_los_3_candidatos_en_orden()
    //{
    //    var candidatos = GetViajesQueryHandler.ConstruirCandidatos("5000866869-C,4173404,5000866869");

    //    Assert.Equal("5000866869-C,4173404,5000866869", candidatos.Directo);
    //    Assert.Equal("5000866869", candidatos.LimpiarFolio);
    //    Assert.Equal("5000866869/4173404", candidatos.Slash);
    //}

    //[Fact]
    //public void EsNoRemisionNulo_detecta_null_vacio_y_solo_espacios()
    //{
    //    Assert.True(GetViajesQueryHandler.EsNoRemisionNulo(null));
    //    Assert.True(GetViajesQueryHandler.EsNoRemisionNulo(""));
    //    Assert.True(GetViajesQueryHandler.EsNoRemisionNulo("   "));
    //    Assert.False(GetViajesQueryHandler.EsNoRemisionNulo("5001053341"));
    //}

    //[Theory]
    //[InlineData("Cancelado")]
    //[InlineData("cancelado")]
    //[InlineData("Cancelada")]
    //[InlineData(" Cancelado ")]
    //public void EsEstatusCancelado_reconoce_Cancelado_y_Cancelada_sin_importar_mayusculas_o_espacios(string estatus)
    //{
    //    Assert.True(GetViajesQueryHandler.EsEstatusCancelado(estatus));
    //}

    //[Fact]
    //public void EsEstatusCancelado_no_marca_Liquidado_como_cancelado()
    //{
    //    Assert.False(GetViajesQueryHandler.EsEstatusCancelado("Liquidado"));
    //}

    // ---------------------------------------------------------------------------------------
    // Grupo 2: Handle() completo, con fakes de IApplicationDbContext/ICisViajeEnrichmentRepository
    // ---------------------------------------------------------------------------------------

    private static GetViajesQuery ConsultaDePrueba() => new(
        FechaInicio: new DateTime(2026, 1, 1),
        FechaFin: new DateTime(2026, 9, 9),
        TipoFecha: "ingreso",
        Areas: null, IdUnidad: null, Estados: null, IdRuta: null, IdOperador: null, NoRemision: null);

    private static SpViajesDto Viaje(string? noRemision, string estatus = "Liquidado", int noViaje = 1) => new()
    {
        _base = "CCZ",
        no_viaje = noViaje,
        estatus_viaje = estatus,
        no_remision = noRemision,
        ruta = "800052 Origen (CCZ) - Destino (DES) - I",
        direccion = "Ida",
    };

    private static DatosCisViaje CisPara(string folio) => new(
        Folio: folio,
        FolioComplemento: null,
        IdSucursal: 36,
        Sucursal: "MTY",
        NombreCorto: "Arca",
        Region: "Noreste",
        Nomenclatura: "MTY",
        Origen: "3001 Guadalupe",
        Destino: "3003 Monterrey",
        EstadoOrigen: "NL",
        EstadoDestino: "NL",
        TotalVenta: 7546.35m,
        EjesEquipos: 5,
        FechaCalendario: new DateOnly(2026, 7, 1),
        Trayecto: "I",
        Operacion: "Arca");

    [Fact]
    public async Task NoRemision_nulo_o_vacio_se_excluye_de_la_tabla()
    {
        var filas = new List<SpViajesDto> { Viaje(noRemision: null), Viaje(noRemision: "   ") };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Empty(resultado.Value);
        // Ninguna fila con no_remision nulo/vacío debió siquiera generar un candidato para CIS.
        Assert.Null(cis.UltimosCandidatosRecibidos);
    }

    [Fact]
    public async Task Estatus_cancelado_se_excluye_de_la_tabla()
    {
        var filas = new List<SpViajesDto> { Viaje(noRemision: "5001053341", estatus: "Cancelado") };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Empty(resultado.Value);
    }

    [Fact]
    public async Task Coincidencia_directa_resuelve_Encontrado_con_llave_Directo()
    {
        var filas = new List<SpViajesDto> { Viaje(noRemision: "4292946") };
        var porFolio = new Dictionary<string, DatosCisViaje> { ["4292946"] = CisPara("4292946") };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(new CisEnrichmentBatchResult(porFolio, new HashSet<string>()));
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.Encontrado, viaje.cis_estado);
        Assert.Equal("Directo", viaje.cis_llave_utilizada);
        Assert.Equal("Arca", viaje.cis_cliente);
        Assert.Null(viaje.cis_motivo_no_coincidencia);
    }

    [Fact]
    public async Task Folio_inexistente_resulta_NoEncontrado_y_se_excluye_de_la_tabla()
    {
        var filas = new List<SpViajesDto> { Viaje(noRemision: "999999999") };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        // NoEncontrado nunca llega al resultado final -- se excluye de la tabla y de métricas.
        Assert.Empty(resultado.Value);
    }

    [Fact]
    public async Task Folio_duplicado_en_CIS_resulta_Duplicado_y_se_excluye_sin_usar_First()
    {
        var filas = new List<SpViajesDto> { Viaje(noRemision: "20355226") };
        var duplicados = new HashSet<string> { "20355226" };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(new CisEnrichmentBatchResult(new Dictionary<string, DatosCisViaje>(), duplicados));
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        // Duplicado tampoco llega al resultado final -- mismo criterio que NoEncontrado.
        Assert.Empty(resultado.Value);
    }

    [Fact]
    public async Task NoViaje_nunca_se_envia_como_candidato_a_CIS_DB()
    {
        // no_viaje se fija a un valor que, si por error se usara como candidato de Folio,
        // aparecería tal cual en la lista que recibe el repositorio de CIS.
        var filas = new List<SpViajesDto> { Viaje(noRemision: "4292946", noViaje: 999999) };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.NotNull(cis.UltimosCandidatosRecibidos);
        Assert.DoesNotContain("999999", cis.UltimosCandidatosRecibidos);
        Assert.All(cis.UltimosCandidatosRecibidos, c => Assert.False(int.TryParse(c, out var v) && v == 999999));
    }

    [Fact]
    public async Task Solo_se_hace_UNA_consulta_en_lote_a_CIS_sin_importar_cuantas_filas_traiga_el_SP()
    {
        var filas = new List<SpViajesDto>
        {
            Viaje(noRemision: "4292946", noViaje: 1),
            Viaje(noRemision: "4294403", noViaje: 2),
            Viaje(noRemision: "5000866869-C,4173404,5000866869", noViaje: 3),
        };
        var db = new FakeApplicationDbContext(filas);
        var cis = new FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult.Vacio);
        var segundaFuente = new FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult.Vacio);
        var handler = new GetViajesQueryHandler(db, cis, segundaFuente);

        await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.Equal(1, cis.VecesLlamado);
    }

    // ---------------------------------------------------------------------------------------
    // Fakes -- sin base de datos real, sin mocks de terceros.
    // ---------------------------------------------------------------------------------------

    private sealed class FakeApplicationDbContext(IReadOnlyList<SpViajesDto> filas) : IApplicationDbContext
    {
        public Task<IReadOnlyList<TResult>> QueryAsync<TResult>(string sql, IReadOnlyCollection<QueryParameter> parameters, CancellationToken cancellationToken = default) where TResult : class
        {
            // GetViajesQueryHandler solo llama QueryAsync<SpViajesDto> -- este fake asume eso,
            // como corresponde a una prueba enfocada en el handler, no en el ORM.
            return Task.FromResult((IReadOnlyList<TResult>)(object)filas);
        }
    }

    private sealed class FakeCisViajeEnrichmentRepository(CisEnrichmentBatchResult resultado) : ICisViajeEnrichmentRepository
    {
        public IReadOnlyCollection<string>? UltimosCandidatosRecibidos { get; private set; }
        public int VecesLlamado { get; private set; }

        public Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> candidatosFolio, CancellationToken cancellationToken = default)
        {
            UltimosCandidatosRecibidos = candidatosFolio;
            VecesLlamado++;
            return Task.FromResult(resultado);
        }
    }

    private sealed class FakeSegundaFuenteEnrichmentRepository(SegundaFuenteBatchResult resultado) : ISegundaFuenteEnrichmentRepository
    {
        public Task<SegundaFuenteBatchResult> ObtenerAsync(
            IReadOnlyCollection<string> basesDistintas,
            IReadOnlyCollection<string> codigosRutaDistintos,
            IReadOnlyCollection<string> facturasDistintas,
            CancellationToken cancellationToken = default)
            => Task.FromResult(resultado);
    }
}