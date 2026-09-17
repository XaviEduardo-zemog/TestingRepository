using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

public sealed class ResumenEjecutivoCalculatorTests
{
    [Fact]
    public void ArbolComparativo_no_supera_3_niveles_ClienteZonaMatriz()
    {
        var viajes = new List<ViajesDto>
        {
            new() { cis_cliente = "ME", cis_zona = "Occidente", cis_matriz = "CCZ CTO", cis_trayecto = "Ida", cis_total_venta = 100m, fecha_ingreso = "1/7/2026 12:00 AM" },
            new() { cis_cliente = "Arca", cis_zona = "Noreste", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 200m, fecha_ingreso = "1/8/2026 12:00 AM" },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);

        Assert.NotNull(resumen.ArbolComparativo);
        Assert.Equal(2, MaxNivel(resumen.ArbolComparativo!));
    }

    [Fact]
    public void CalcularAsignacion_clasifica_desde_EjesEquipos_y_PctComodato_no_es_null_con_datos()
    {
        var viajes = new List<ViajesDto>
        {
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 100m, cis_ejes_equipos = 5, fecha_ingreso = "1/7/2026 12:00 AM" },
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 100m, cis_ejes_equipos = 5, fecha_ingreso = "2/8/2026 12:00 AM" },
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 200m, cis_ejes_equipos = 6, fecha_ingreso = "3/8/2026 12:00 AM" },
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 300m, cis_ejes_equipos = 9, fecha_ingreso = "4/8/2026 12:00 AM" },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);
        var asignacion = ResumenEjecutivoCalculator.CalcularAsignacion(resumen.ArbolComparativo!);

        Assert.Equal(1m, asignacion.Sencillo);
        Assert.Equal(1m, asignacion.Comodato);
        Assert.Equal(1m, asignacion.Full);
        Assert.Equal(3m, asignacion.Total);
        Assert.NotNull(asignacion.PctComodato);
        Assert.NotNull(asignacion.DeltaPuntosPorcentuales);
    }

    private static int MaxNivel(NodoComparativo nodo) =>
        nodo.Hijos.Count == 0 ? nodo.Nivel : nodo.Hijos.Values.Max(MaxNivel);
}