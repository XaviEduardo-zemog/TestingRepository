namespace Testing.Application.GetAllViajes;

/// <summary>
/// Un KPI comparado mes actual vs. mes anterior, listo para pintar en KpiCard.
/// Disponible=false solo si no hubo datos que agregar (nunca por falta de Venta -- TotalVenta =
/// subtotal_factura, confirmado por el usuario para esta fase, ver §54.85 / auditoría Venta).
/// </summary>
public sealed record KpiComparativo(
    string Clave,
    string Etiqueta,
    bool Disponible,
    string? MotivoPendiente,
    decimal? ValorActual,
    decimal? ValorAnterior,
    string? EtiquetaMesActual,
    string? EtiquetaMesAnterior)
{
    public decimal? DeltaPorcentaje =>
        Disponible && ValorActual is not null && ValorAnterior is > 0
            ? (ValorActual - ValorAnterior) / ValorAnterior * 100
            : null;

    /// <summary>Hay mes anterior pero su valor para este KPI es 0 — "sin base en {mes}" en el HTML.</summary>
    public bool SinBaseAnterior => Disponible && EtiquetaMesAnterior is not null && ValorAnterior is 0;

    /// <summary>No hay un segundo mes con datos para comparar — "sin mes anterior" en el HTML.</summary>
    public bool SinMesAnterior => Disponible && EtiquetaMesAnterior is null;
}

internal sealed record MetricaMensual(int Anio, int Mes, decimal Viajes, decimal Kms, decimal Venta);

public static class KpisComparativaCalculator
{
    private static readonly string[] NombresMes =
        ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

    public static IReadOnlyList<KpiComparativo> Calcular(IEnumerable<ViajesDto> viajesFiltrados, CorteMensual? corte)
    {
        var porMes = viajesFiltrados
            .Select(v => (Fecha: CamposDerivadosViajes.ObtenerFechaNegocio(v), Viaje: v))
            .Where(x => x.Fecha is not null)
            .GroupBy(x => (Anio: x.Fecha!.Value.Year, Mes: x.Fecha.Value.Month))
            .Select(g => new MetricaMensual(
                g.Key.Anio,
                g.Key.Mes,
                g.Sum(x => ContribucionViajeProyectada.Viajes(x.Viaje, corte)),
                g.Sum(x => ContribucionViajeProyectada.Kms(x.Viaje, corte)),
                g.Sum(x => ContribucionViajeProyectada.Venta(x.Viaje, corte))))
            .OrderBy(m => m.Anio).ThenBy(m => m.Mes)
            .ToList();

        var actual = porMes.Count > 0 ? porMes[^1] : null;
        var anterior = porMes.Count > 1 ? porMes[^2] : null;

        var etiquetaActual = Etiqueta(actual);
        var etiquetaAnterior = Etiqueta(anterior);

        decimal? kmPorViajeActual = actual is null ? null : actual.Viajes > 0 ? actual.Kms / actual.Viajes : 0;
        decimal? kmPorViajeAnterior = anterior is null ? null : anterior.Viajes > 0 ? anterior.Kms / anterior.Viajes : 0;

        // $/KM y $/Viaje: razón de acumulados del propio mes -- NUNCA promedio de razones de viaje.
        decimal? pkmActual = actual is null ? null : actual.Kms > 0 ? actual.Venta / actual.Kms : 0;
        decimal? pkmAnterior = anterior is null ? null : anterior.Kms > 0 ? anterior.Venta / anterior.Kms : 0;
        decimal? pvjActual = actual is null ? null : actual.Viajes > 0 ? actual.Venta / actual.Viajes : 0;
        decimal? pvjAnterior = anterior is null ? null : anterior.Viajes > 0 ? anterior.Venta / anterior.Viajes : 0;

        return
        [
            new("viajes", "Viajes", true, null, actual?.Viajes, anterior?.Viajes, etiquetaActual, etiquetaAnterior),
            new("kms", "KMS", true, null, actual?.Kms, anterior?.Kms, etiquetaActual, etiquetaAnterior),
            new("venta", "Venta", true, null, actual?.Venta, anterior?.Venta, etiquetaActual, etiquetaAnterior),
            new("pkm", "$/KM", true, null, pkmActual, pkmAnterior, etiquetaActual, etiquetaAnterior),
            new("kpv", "KM/Viaje", true, null, kmPorViajeActual, kmPorViajeAnterior, etiquetaActual, etiquetaAnterior),
            new("pvj", "$/Viaje", true, null, pvjActual, pvjAnterior, etiquetaActual, etiquetaAnterior),
        ];
    }

    private static string? Etiqueta(MetricaMensual? m) =>
        m is null ? null : $"{NombresMes[m.Mes - 1]} {(m.Anio % 100):00}";
}