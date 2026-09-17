namespace Testing.Application.GetAllViajes;

/// <summary>
/// Cálculo de los valores de EjesEquipos/armado que no encajan en Sencillo/Comodato/Full --
/// extraído de ResumenEjecutivoCalculator en la Etapa Clean H
/// (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md).
/// </summary>
internal static class ResumenArmadosDesconocidosCalculator
{
    internal static List<(string Valor, decimal Viajes)> CalcularArmadosDesconocidos(IReadOnlyList<ViajesDto> viajes)
    {
        var acumulado = new Dictionary<string, decimal>();

        foreach (var v in viajes)
        {
            if (CamposDerivadosViajes.ObtenerMovimiento(v) != "Ida")
                continue;

            if (CamposDerivadosViajes.ClasificarArmado(v) is not null)
                continue; // ya se clasificó como Full o Sencillo

            var crudo = CamposDerivadosViajes.NormalizarArmadoCrudo(v);
            if (crudo is null)
                continue; // sin dato, no es una categoría desconocida

            acumulado[crudo] = acumulado.GetValueOrDefault(crudo) + 1;
        }

        return acumulado
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }
}