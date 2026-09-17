namespace Testing.Application.GetAllViajes;

/// <summary>
/// Cálculo del bloque "Agencias que ya no aparecen" -- extraído de ResumenEjecutivoCalculator en
/// la Etapa Clean H (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md).
/// </summary>
internal static class ResumenAgenciasDesaparecidasCalculator
{
    internal static AgenciasDesaparecidasResumenDto CalcularAgenciasDesaparecidas(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte, Func<ViajesDto, DateTime?> fechaDe)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = meses[^1];

        var porAgencia = new Dictionary<(string Destino, string Matriz), Dictionary<MesCerrado, decimal>>();
        var ventaPorAgenciaMes = new Dictionary<(string Destino, string Matriz), Dictionary<MesCerrado, decimal>>();

        foreach (var v in viajes)
        {
            var fecha = fechaDe(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var destino = CamposDerivadosViajes.ObtenerDestino(v) ?? "(sin dato)";
            var matriz = CamposDerivadosViajes.ObtenerMatriz(v) ?? "(sin dato)";
            var clave = (destino, matriz);

            if (!ventaPorAgenciaMes.TryGetValue(clave, out var porMesVenta))
                ventaPorAgenciaMes[clave] = porMesVenta = [];
            porMesVenta[mes] = porMesVenta.GetValueOrDefault(mes) + ContribucionViajeProyectada.Venta(v, corte);

            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            if (!esIda)
                continue; // "presencia"/conteo de viajes: replica r.viaje (0 en tramos de Regreso)

            if (!porAgencia.TryGetValue(clave, out var porMes))
                porAgencia[clave] = porMes = [];

            porMes[mes] = porMes.GetValueOrDefault(mes) + ContribucionViajeProyectada.Viajes(v, corte);
        }

        var resultado = new List<AgenciaDesaparecidaDto>();
        foreach (var ((destino, matriz), porMes) in porAgencia)
        {
            var ultimoVisto = porMes.Keys.OrderBy(m => m.Anio).ThenBy(m => m.Mes).Last();
            if (ultimoVisto.Anio == ultimo.Anio && ultimoVisto.Mes == ultimo.Mes)
                continue; // sigue activa en el último mes

            var ventaAcumulada = ventaPorAgenciaMes.TryGetValue((destino, matriz), out var porMesVenta) ? porMesVenta.Values.Sum() : 0m;

            resultado.Add(new AgenciaDesaparecidaDto(destino, matriz, ultimoVisto, porMes[ultimoVisto], porMes.Count, ventaAcumulada));
        }

        var ordenado = resultado
            .OrderByDescending(a => a.VentaAcumulada)
            .ThenBy(a => a.Destino, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new AgenciasDesaparecidasResumenDto(
            TotalDesaparecidas: ordenado.Count,
            VentaAcumuladaTotal: ordenado.Sum(a => a.VentaAcumulada),
            Top30: ordenado.Take(30).ToList());
    }
}