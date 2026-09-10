using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 5 -- cierra un hueco de cobertura: la jerarquía Cliente → Zona → Matriz (3 niveles, sin
/// Sucursal como 4º nivel) se había validado por inspección de código, no con una prueba
/// automatizada que construya el árbol real vía la API pública de ResumenEjecutivoCalculator.
/// </summary>
public sealed class ResumenEjecutivoCalculatorTests
{
    [Fact]
    public void ArbolComparativo_no_supera_3_niveles_ClienteZonaMatriz()
    {
        // 2 meses distintos para que hayComparativos sea true y se construya el árbol.
        var viajes = new List<ViajesDto>
        {
            new() { cis_cliente = "ME", cis_zona = "Occidente", cis_matriz = "CCZ CTO", cis_trayecto = "Ida", cis_total_venta = 100m, fecha_ingreso = "1/7/2026 12:00 AM" },
            new() { cis_cliente = "Arca", cis_zona = "Noreste", cis_matriz = "MTY", cis_trayecto = "Ida", cis_total_venta = 200m, fecha_ingreso = "1/8/2026 12:00 AM" },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);

        Assert.NotNull(resumen.ArbolComparativo);
        Assert.Equal(2, MaxNivel(resumen.ArbolComparativo!)); // Cliente=0, Zona=1, Matriz=2 -- nunca llega a un 4º nivel (Sucursal)
    }

    private static int MaxNivel(NodoComparativo nodo) =>
        nodo.Hijos.Count == 0 ? nodo.Nivel : nodo.Hijos.Values.Max(MaxNivel);
}
