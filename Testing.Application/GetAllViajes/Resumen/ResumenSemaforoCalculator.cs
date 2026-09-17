namespace Testing.Application.GetAllViajes;

/// <summary>
/// Cálculo de las alertas del semáforo (Bloque 8.1) -- extraído de ResumenEjecutivoCalculator en
/// la Etapa Clean H (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md). Depende de
/// ResumenFrecuenciaCalculator (alertas de frecuencia) y de ResumenEjecutivoCalculator.FormatoDinero/
/// FormatoPorcentaje (formato de texto, se dejaron en la clase principal por ser triviales, ya
/// públicos y consumidos por otros 4 archivos -- ver §4 del documento de esta etapa).
/// </summary>
internal static class ResumenSemaforoCalculator
{
    // ---------- Bloque 8.1 — Semáforo (replica los sem.push(...) de RE_render()) ----------

    internal static List<AlertaSemaforo> CalcularSemaforo(
        BloqueNivelDto? nivelZemog,
        IReadOnlyList<NivelPorClienteDto> porCliente,
        NodoComparativo? arbol,
        DestinosCayendoResumenDto? destinosCayendo,
        AgenciasDesaparecidasResumenDto agenciasDesaparecidas,
        RotacionOperadoresDto rotacion,
        bool hayComparativos)
    {
        var alertas = new List<AlertaSemaforo>();

        if (nivelZemog is null)
            return alertas;

        var peor = nivelZemog.PeorMesDelAnio;
        alertas.Add(new AlertaSemaforo(
            peor is null ? "Peor mes a nivel Zemog: sin datos suficientes" : $"Peor mes a nivel Zemog: {peor.Value.Mes.Etiqueta} ({ResumenEjecutivoCalculator.FormatoDinero(peor.Value.Venta)})",
            SeveridadAlerta.Neutral));

        var etiquetaAnterior = nivelZemog.MesAnterior?.Etiqueta ?? "mes anterior";
        var deltaVenta = nivelZemog.DeltaVentaPctVsAnterior;
        alertas.Add(new AlertaSemaforo(
            $"Venta de {nivelZemog.MesUltimo.Etiqueta} vs {etiquetaAnterior}: {(deltaVenta is null ? "sin base en " + etiquetaAnterior : ResumenEjecutivoCalculator.FormatoPorcentaje(deltaVenta.Value))}",
            deltaVenta switch { > 0 => SeveridadAlerta.Positiva, < 0 => SeveridadAlerta.Negativa, _ => SeveridadAlerta.Neutral }));

        foreach (var c in porCliente)
        {
            var etiquetaAnteriorCliente = c.Bloque.MesAnterior?.Etiqueta ?? "mes anterior";
            var deltaVentaCliente = c.Bloque.DeltaVentaPctVsAnterior;
            alertas.Add(new AlertaSemaforo(
                $"{c.Cliente}: venta {c.Bloque.MesUltimo.Etiqueta} vs {etiquetaAnteriorCliente}: {(deltaVentaCliente is null ? "sin base en " + etiquetaAnteriorCliente : ResumenEjecutivoCalculator.FormatoPorcentaje(deltaVentaCliente.Value))}",
                deltaVentaCliente switch { > 0 => SeveridadAlerta.Positiva, < 0 => SeveridadAlerta.Negativa, _ => SeveridadAlerta.Neutral }));
        }

        if (!hayComparativos)
            return alertas;

        if (destinosCayendo is { TotalConCaida: > 0 })
        {
            var peorDestino = destinosCayendo.Top25[0];
            alertas.Add(new AlertaSemaforo(
                $"{destinosCayendo.TotalConCaida} destinos con caída de venta (impacto total {ResumenEjecutivoCalculator.FormatoDinero(destinosCayendo.ImpactoTotal)}); mayor caída: {peorDestino.Destino} ({ResumenEjecutivoCalculator.FormatoDinero(peorDestino.DeltaVenta)})",
                SeveridadAlerta.Negativa));
        }
        else
        {
            alertas.Add(new AlertaSemaforo("Destinos: ninguno con caída de venta este mes", SeveridadAlerta.Positiva));
        }

        var alertasFrecuencia = arbol is null ? [] : ResumenFrecuenciaCalculator.RecolectarAlertasFrecuencia(arbol);
        if (alertasFrecuencia.Count > 0)
        {
            var top4 = alertasFrecuencia.Take(4).Select(a => $"{a.Matriz} ({ResumenEjecutivoCalculator.FormatoPorcentaje(a.DeltaPorcentaje)})");
            var extra = alertasFrecuencia.Count > 4 ? $" y {alertasFrecuencia.Count - 4} más" : "";
            alertas.Add(new AlertaSemaforo(
                $"Matrices/sucursales con caída de viajes a revisar: {string.Join(", ", top4)}{extra}",
                SeveridadAlerta.Negativa));
        }

        if (agenciasDesaparecidas.TotalDesaparecidas > 0)
        {
            alertas.Add(new AlertaSemaforo(
                $"{agenciasDesaparecidas.TotalDesaparecidas} agencias/destinos ya no aparecen en {nivelZemog.MesUltimo.Etiqueta} (venta acumulada: {ResumenEjecutivoCalculator.FormatoDinero(agenciasDesaparecidas.VentaAcumuladaTotal)})",
                SeveridadAlerta.Negativa));
        }

        alertas.Add(new AlertaSemaforo(
            $"Operadores: {rotacion.Total.Activos} activos en {nivelZemog.MesUltimo.Etiqueta}, {rotacion.Total.Altas} altas y {rotacion.Total.Bajas} dejaron de aparecer (venta acumulada de bajas: {ResumenEjecutivoCalculator.FormatoDinero(rotacion.Total.VentaBajas)})",
            SeveridadAlerta.Neutral));

        return alertas;
    }
}