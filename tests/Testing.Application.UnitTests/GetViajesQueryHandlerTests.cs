using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

public sealed class GetViajesQueryHandlerTests
{
    private static GetViajesQuery ConsultaDePrueba() => new(
        FechaInicio: new DateTime(2026, 9, 1),
        FechaFin: new DateTime(2026, 9, 7));

    private static DatosCisViajeDirecto FilaCisDirecta(
        string folio = "9000001",
        int noViaje = 1,
        string trayecto = "IDA",
        DateOnly? fechaCalendario = null) => new(
            Identificador: 1,
            Folio: folio,
            FolioComplemento: null,
            NoViaje: noViaje,
            NoGuia: 500,
            Factura: "F-1",
            IdArea: 10,
            IdSucursal: 36,
            Sucursal: "MTY",
            NombreCorto: "Arca",
            Region: "Noreste",
            Nomenclatura: "MTY",
            Trayecto: trayecto,
            FechaCalendario: fechaCalendario ?? new DateOnly(2026, 9, 3),
            Origen: "3001 Guadalupe",
            Destino: "3003 Monterrey",
            EstadoOrigen: "NL",
            EstadoDestino: "NL",
            Ruta: "800052 Origen (CCZ) - Destino (DES) - I",
            CodigoRuta: "COD-1",
            Expedicion: "Paquetería",
            Operador1: "Juan Perez",
            IdOperador1: 55,
            Operador2: null,
            IdOperador2: null,
            Unidad: "U-100",
            Remolque1: "R-1",
            Remolque2: null,
            Dolly: null,
            Kms: 450,
            TotalVenta: 12345.67m,
            EjesEquipos: 5,
            MontoPeajeIave: 100m,
            MontoPeajeEfectivo: 50m,
            EstatusAsignacion: "Entregado");

    [Fact]
    public async Task Handle_usa_ICisViajesDirectosRepository_y_mapea_las_filas_con_ViajesDirectosMapper()
    {
        var filaCis = FilaCisDirecta(folio: "9000001", trayecto: "IDA");
        var repo = new FakeCisViajesDirectosRepository([filaCis]);
        var handler = new GetViajesQueryHandler(repo);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        var viaje = Assert.Single(resultado.Value);
        Assert.Equal(EstadoEnriquecimientoCis.Encontrado, viaje.cis_estado);
        Assert.Equal("CisDirecto", viaje.cis_llave_utilizada);
        Assert.Equal("9000001", viaje.cis_folio);
        Assert.Equal(1, repo.VecesLlamado);
    }

    [Fact]
    public async Task Handle_convierte_FechaInicio_FechaFin_de_GetViajesQuery_a_DateOnly()
    {
        var repo = new FakeCisViajesDirectosRepository([]);
        var handler = new GetViajesQueryHandler(repo);

        await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 9, 1), repo.UltimaFechaInicioRecibida);
        Assert.Equal(new DateOnly(2026, 9, 7), repo.UltimaFechaFinRecibida);
    }

    [Fact]
    public async Task Handle_sin_filas_devuelve_lista_vacia_sin_lanzar()
    {
        var repo = new FakeCisViajesDirectosRepository([]);
        var handler = new GetViajesQueryHandler(repo);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Empty(resultado.Value);
    }

    [Fact]
    public async Task Handle_hace_UNA_sola_llamada_al_repositorio_sin_importar_cuantas_filas_devuelva()
    {
        var filas = new List<DatosCisViajeDirecto>
        {
            FilaCisDirecta(folio: "9000001", noViaje: 1),
            FilaCisDirecta(folio: "9000002", noViaje: 2),
            FilaCisDirecta(folio: "9000003", noViaje: 3),
        };
        var repo = new FakeCisViajesDirectosRepository(filas);
        var handler = new GetViajesQueryHandler(repo);

        var resultado = await handler.Handle(ConsultaDePrueba(), CancellationToken.None);

        Assert.Equal(3, resultado.Value.Count);
        Assert.Equal(1, repo.VecesLlamado);
    }

    private sealed class FakeCisViajesDirectosRepository(IReadOnlyList<DatosCisViajeDirecto> filas) : ICisViajesDirectosRepository
    {
        public int VecesLlamado { get; private set; }
        public DateOnly? UltimaFechaInicioRecibida { get; private set; }
        public DateOnly? UltimaFechaFinRecibida { get; private set; }

        public Task<IReadOnlyList<DatosCisViajeDirecto>> ObtenerPorRangoFechaAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken = default)
        {
            VecesLlamado++;
            UltimaFechaInicioRecibida = fechaInicio;
            UltimaFechaFinRecibida = fechaFin;
            return Task.FromResult(filas);
        }
    }
}