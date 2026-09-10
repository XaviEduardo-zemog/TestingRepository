using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Prompt 5 -- cierra un hueco de cobertura detectado en la revisión final: ME/MD → Modelo y el
/// orden Arca-antes-Modelo se habían validado por inspección de código + datos reales (Prompt 4),
/// pero no existía ninguna prueba automatizada dedicada a ellos.
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
}
