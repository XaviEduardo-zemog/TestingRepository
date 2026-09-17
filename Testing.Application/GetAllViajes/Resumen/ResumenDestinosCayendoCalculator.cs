namespace Testing.Application.GetAllViajes;

/// <summary>
/// Cálculo del bloque "Destinos que estamos dejando de dar" -- extraído de
/// ResumenEjecutivoCalculator en la Etapa Clean H (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md).
/// </summary>
internal static class ResumenDestinosCayendoCalculator
{
    internal static DestinosCayendoResumenDto CalcularDestinosCayendo(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte, Func<ViajesDto, DateTime?> fechaDe)
    {
        var ultimo = meses[^1];
        var anterior = meses[^2];

        var acumPorGrupoMes = new Dictionary<(string Destino, string Matriz, MesCerrado Mes), TotalesPeriodo>();

        foreach (var v in viajes)
        {
            var fecha = fechaDe(v);
            if (fecha is null)
                continue;

            MesCerrado mes;
            if (fecha.Value.Year == ultimo.Anio && fecha.Value.Month == ultimo.Mes) mes = ultimo;
            else if (fecha.Value.Year == anterior.Anio && fecha.Value.Month == anterior.Mes) mes = anterior;
            else continue;

            var destino = CamposDerivadosViajes.ObtenerDestino(v) ?? "(sin dato)";
            var matriz = CamposDerivadosViajes.ObtenerMatriz(v) ?? "(sin dato)";
            var clave = (destino, matriz, mes);
            acumPorGrupoMes[clave] = TotalesPeriodo.Sumar(acumPorGrupoMes.GetValueOrDefault(clave, TotalesPeriodo.Vacio), TotalesPeriodo.De(v, corte));
        }

        var grupos = acumPorGrupoMes.Keys.Select(k => (k.Destino, k.Matriz)).Distinct();

        var candidatos = grupos
            .Select(g =>
            {
                var a = acumPorGrupoMes.GetValueOrDefault((g.Destino, g.Matriz, anterior), TotalesPeriodo.Vacio);
                var b = acumPorGrupoMes.GetValueOrDefault((g.Destino, g.Matriz, ultimo), TotalesPeriodo.Vacio);
                return new DestinoCayendoDto(g.Destino, g.Matriz, a.Viajes, b.Viajes, a.Venta, b.Venta);
            })
            .Where(d => d.VentaAnterior > 0 && d.VentaActual < d.VentaAnterior)
            .OrderBy(d => d.DeltaVenta) // ascendente = más negativo (mayor pérdida) primero
            .ToList();

        return new DestinosCayendoResumenDto(
            TotalConCaida: candidatos.Count,
            ImpactoTotal: candidatos.Sum(d => d.DeltaVenta),
            Top25: candidatos.Take(25).ToList());
    }
}