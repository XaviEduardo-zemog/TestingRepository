using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 4 -- Validación funcional y regresión final: pruebas de
/// ContribucionViajeProyectada.Venta, confirmando que TotalVenta = 1.00 (y 0.00, y montos
/// mayores) se conserva sin excepción, que Ida/Regreso solo afectan Viajes (nunca Venta), que
/// factorMes se aplica correctamente, y que subtotal_factura nunca sustituye a cis_total_venta.
/// </summary>
public sealed class ProyeccionMensualTests
{
    private static ViajesDto Viaje(string? trayecto, decimal? totalVenta, decimal? subtotalFactura = null, string? fechaIngreso = null) => new()
    {
        cis_trayecto = trayecto,
        cis_total_venta = totalVenta,
        subtotal_factura = subtotalFactura,
        fecha_ingreso = fechaIngreso,
    };

    // ---------------------------------------------------------------------------------------
    // Ida / Regreso -- corte sin proyección (null)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Ida_con_TotalVenta_1_00_y_corte_sin_proyeccion_da_Viajes_1_y_Venta_1_00()
    {
        var viaje = Viaje(trayecto: "Ida", totalVenta: 1.00m);

        Assert.Equal(1m, ContribucionViajeProyectada.Viajes(viaje, corte: null));
        Assert.Equal(1.00m, ContribucionViajeProyectada.Venta(viaje, corte: null));
    }

    [Fact]
    public void Regreso_con_TotalVenta_1_00_y_corte_sin_proyeccion_da_Viajes_0_y_Venta_1_00()
    {
        var viaje = Viaje(trayecto: "Regreso", totalVenta: 1.00m);

        Assert.Equal(0m, ContribucionViajeProyectada.Viajes(viaje, corte: null));
        Assert.Equal(1.00m, ContribucionViajeProyectada.Venta(viaje, corte: null)); // la dirección solo afecta Viajes, nunca Venta
    }

    // ---------------------------------------------------------------------------------------
    // Mes proyectado -- factorMes = 2.00 (DiasEnMes=30 / DiaCorte=15)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TotalVenta_1_00_con_factorMes_2_00_da_Venta_2_00()
    {
        var corte = new CorteMensual(2026, 7, 15, 30); // Factor = 30/15 = 2.00
        Assert.Equal(2.00m, corte.Factor);

        var viaje = Viaje(trayecto: "Ida", totalVenta: 1.00m, fechaIngreso: "10/7/2026 12:00 AM");

        Assert.Equal(2.00m, ContribucionViajeProyectada.Venta(viaje, corte));
    }

    // ---------------------------------------------------------------------------------------
    // Validación de TotalVenta: 0.00 / 1.00 / > 1.00 se conservan por igual, sin caso especial
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.00, 0.00)]
    [InlineData(1.00, 1.00)]
    [InlineData(43280.50, 43280.50)]
    public void Venta_conserva_cis_total_venta_sin_importar_el_monto(double totalVenta, double esperado)
    {
        var viaje = Viaje(trayecto: "Ida", totalVenta: (decimal)totalVenta);

        Assert.Equal((decimal)esperado, ContribucionViajeProyectada.Venta(viaje, corte: null));
    }

    // ---------------------------------------------------------------------------------------
    // Regla negativa: NO debe existir de nuevo "if (venta == 1.00m) venta = 0m;", y
    // subtotal_factura NUNCA sustituye a cis_total_venta.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Venta_NO_convierte_1_00_en_cero_ida_o_regreso()
    {
        // Reproduce exactamente el caso que la regla retirada rompía -- si esa lógica
        // reapareciera, esta prueba fallaría con Venta=0 en vez de 1.00.
        Assert.NotEqual(0m, ContribucionViajeProyectada.Venta(Viaje("Ida", 1.00m), corte: null));
        Assert.NotEqual(0m, ContribucionViajeProyectada.Venta(Viaje("Regreso", 1.00m), corte: null));
    }

    [Fact]
    public void Venta_nunca_usa_subtotal_factura_como_sustituto_de_cis_total_venta()
    {
        // cis_total_venta y subtotal_factura traen valores distintos -- el resultado debe seguir
        // exactamente a cis_total_venta, nunca a subtotal_factura.
        var conAmbos = Viaje(trayecto: "Ida", totalVenta: 1.00m, subtotalFactura: 999.99m);
        Assert.Equal(1.00m, ContribucionViajeProyectada.Venta(conAmbos, corte: null));

        // Incluso cuando cis_total_venta es null (fila sin ese dato), el resultado es 0 -- NUNCA
        // cae a subtotal_factura como respaldo silencioso.
        var sinCis = Viaje(trayecto: "Ida", totalVenta: null, subtotalFactura: 999.99m);
        Assert.Equal(0m, ContribucionViajeProyectada.Venta(sinCis, corte: null));
    }
}
