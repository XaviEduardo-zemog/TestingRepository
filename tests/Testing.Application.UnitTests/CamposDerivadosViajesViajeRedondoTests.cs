using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Tests de CamposDerivadosViajes.AplicarViajeRedondo -- la corrección de
/// docs/BUG_DESTINO_VIAJE_REDONDO.md. Congelan el comportamiento esperado (replicado de
/// viajes_v14.html, bloque "VIAJE REDONDO"): un Regreso hereda Destino/Estado Destino del Ida
/// más reciente de la MISMA Unidad+Sucursal, ordenado por CitaCarga.
/// </summary>
public sealed class CamposDerivadosViajesViajeRedondoTests
{
    [Fact]
    public void AplicarViajeRedondo_el_Regreso_hereda_destino_del_Ida_previo_de_la_misma_unidad()
    {
        var ida = new ViajesDto
        {
            id_unidad = "U-1",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 8, 0, 0),
            cis_destino = "Monterrey",
            cis_estado_destino = "NL",
        };
        var regreso = new ViajesDto
        {
            id_unidad = "U-1",
            cis_sucursal = "MTY",
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 14, 0, 0),
            cis_destino = "CrudoIncorrecto", // valor propio del tramo de vuelta -- debe sobreescribirse
            cis_estado_destino = "XX",
        };

        var viajes = new List<ViajesDto> { ida, regreso };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        Assert.Equal("Monterrey", regreso.cis_destino);
        Assert.Equal("NL", regreso.cis_estado_destino);

        // El Ida nunca se toca.
        Assert.Equal("Monterrey", ida.cis_destino);
        Assert.Equal("NL", ida.cis_estado_destino);
    }

    [Fact]
    public void AplicarViajeRedondo_un_Regreso_sin_Ida_previa_conserva_su_propio_destino()
    {
        var regresoSinIdaPrevia = new ViajesDto
        {
            id_unidad = "U-2",
            cis_sucursal = "MTY",
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 6, 0, 0),
            cis_destino = "DestinoPropio",
            cis_estado_destino = "NL",
        };

        var viajes = new List<ViajesDto> { regresoSinIdaPrevia };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        Assert.Equal("DestinoPropio", regresoSinIdaPrevia.cis_destino);
        Assert.Equal("NL", regresoSinIdaPrevia.cis_estado_destino);
    }

    [Fact]
    public void AplicarViajeRedondo_no_cruza_unidades_distintas()
    {
        var idaUnidadA = new ViajesDto
        {
            id_unidad = "U-A",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 8, 0, 0),
            cis_destino = "Monterrey",
        };
        var regresoUnidadB = new ViajesDto
        {
            id_unidad = "U-B", // unidad distinta -- NO debe heredar de idaUnidadA
            cis_sucursal = "MTY",
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 9, 0, 0),
            cis_destino = "DestinoPropioB",
        };

        var viajes = new List<ViajesDto> { idaUnidadA, regresoUnidadB };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        Assert.Equal("DestinoPropioB", regresoUnidadB.cis_destino);
    }

    [Fact]
    public void AplicarViajeRedondo_no_cruza_sucursales_distintas()
    {
        var idaSucursalMty = new ViajesDto
        {
            id_unidad = "U-9",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 8, 0, 0),
            cis_destino = "Monterrey",
        };
        var regresoMismaUnidadOtraSucursal = new ViajesDto
        {
            id_unidad = "U-9", // misma unidad...
            cis_sucursal = "SLP", // ...pero sucursal distinta -- no debe heredar
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 9, 0, 0),
            cis_destino = "DestinoPropioSlp",
        };

        var viajes = new List<ViajesDto> { idaSucursalMty, regresoMismaUnidadOtraSucursal };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        Assert.Equal("DestinoPropioSlp", regresoMismaUnidadOtraSucursal.cis_destino);
    }

    [Fact]
    public void AplicarViajeRedondo_el_Regreso_hereda_del_Ida_mas_reciente_no_del_primero()
    {
        var idaAntigua = new ViajesDto
        {
            id_unidad = "U-3",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 6, 0, 0),
            cis_destino = "DestinoViejo",
        };
        var idaReciente = new ViajesDto
        {
            id_unidad = "U-3",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 10, 0, 0),
            cis_destino = "DestinoNuevo",
        };
        var regreso = new ViajesDto
        {
            id_unidad = "U-3",
            cis_sucursal = "MTY",
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 12, 0, 0),
            cis_destino = "CrudoIncorrecto",
        };

        // Orden de inserción deliberadamente distinto al orden cronológico -- AplicarViajeRedondo
        // debe ordenar internamente por CitaCarga, no depender del orden de la lista de entrada.
        var viajes = new List<ViajesDto> { regreso, idaAntigua, idaReciente };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        Assert.Equal("DestinoNuevo", regreso.cis_destino);
    }

    [Fact]
    public void AplicarViajeRedondo_no_modifica_kms_venta_peaje_ni_movimiento()
    {
        var ida = new ViajesDto
        {
            id_unidad = "U-4",
            cis_sucursal = "MTY",
            cis_trayecto = "Ida",
            cis_cita_carga = new DateTime(2026, 7, 1, 8, 0, 0),
            cis_destino = "Monterrey",
            cis_estado_destino = "NL",
            kms_viaje = 450m,
            cis_total_venta = 12345.67m,
            peaje_efectivo = 50m,
            peaje_electronico = 100m,
        };
        var regreso = new ViajesDto
        {
            id_unidad = "U-4",
            cis_sucursal = "MTY",
            cis_trayecto = "Regreso",
            cis_cita_carga = new DateTime(2026, 7, 1, 14, 0, 0),
            cis_destino = "CrudoIncorrecto",
            cis_estado_destino = "XX",
            kms_viaje = 450m,
            cis_total_venta = 9999.99m,
            peaje_efectivo = 30m,
            peaje_electronico = 20m,
        };

        var viajes = new List<ViajesDto> { ida, regreso };
        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        // Destino/EstadoDestino sí cambiaron (es la corrección); todo lo demás queda intacto.
        Assert.Equal(450m, regreso.kms_viaje);
        Assert.Equal(9999.99m, regreso.cis_total_venta);
        Assert.Equal(30m, regreso.peaje_efectivo);
        Assert.Equal(20m, regreso.peaje_electronico);
        Assert.Equal("Regreso", CamposDerivadosViajes.ObtenerMovimiento(regreso));

        Assert.Equal(450m, ida.kms_viaje);
        Assert.Equal(12345.67m, ida.cis_total_venta);
        Assert.Equal("Ida", CamposDerivadosViajes.ObtenerMovimiento(ida));
    }
}
