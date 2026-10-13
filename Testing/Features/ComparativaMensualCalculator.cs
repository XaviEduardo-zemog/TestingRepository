using System.Net;
using Microsoft.AspNetCore.Components;
using Testing.Components.Shared;

namespace Testing.Features;

/// <summary>
/// Calculador estático (sin DI, igual convención que SegmentosEjesCalculator/PivoteViajesBuilder/
/// JerarquiaViajesBuilder) que arma el contrato de la vista "Gráfica" (ComparativaMensualDto) a
/// partir de lo que RecalcularPivote() ya calculó en ConsultaViajes.razor: filasPivote (modo plano)
/// o raizJerarquia (modo jerarquía) + mesesPivote. No reconstruye ni recalcula el pivote/árbol.
/// </summary>
public static class ComparativaMensualCalculator
{
    public const decimal UmbralEstabilidadPct = 1m;
    public const int TopNPorDefecto = 15;
    public const string SeparadorRuta = " › ";
    public const string TextoSinEstables = "No hay grupos suficientemente estables en este corte.";

    /// <summary>Adaptador privado: unifica FilaPivote (modo plano) y hoja de NodoJerarquia (modo jerarquía).</summary>
    private readonly record struct GrupoFuente(string Nombre, MetricasMes Anterior, MetricasMes Actual);

    public static ComparativaMensualDto? Calcular(
        string agruparPor,
        string etiquetaAgrupacion,
        IReadOnlyList<FilaPivote> filasPivote,
        NodoJerarquia? raizJerarquia,
        IReadOnlyList<MesColumna> meses,
        int topN = TopNPorDefecto)
    {
        if (agruparPor is "dia" or "semana")
            return null;

        if (meses.Count < 2)
            return null;

        var esJerarquia = agruparPor is "jerarquia" or "jerarquia_tarifa";

        if (esJerarquia && raizJerarquia is null)
            return null;

        var claveAnt = meses[^2].Clave;
        var claveAct = meses[^1].Clave;

        var fuentes = esJerarquia
            ? RecolectarFuentesJerarquia(raizJerarquia!, claveAnt, claveAct)
            : filasPivote
                .Select(f => new GrupoFuente(f.Dimension, f.ObtenerMes(claveAnt), f.ObtenerMes(claveAct)))
                .ToList();

        fuentes = fuentes
            .Where(g => !(g.Anterior.Viajes == 0 && g.Actual.Viajes == 0))
            .ToList();

        if (fuentes.Count == 0)
            return null;

        var grupos = fuentes.Select(ClasificarGrupo).ToList();

        var totalGrupos = grupos.Count;
        var bajaron = grupos.Where(g => g.Tendencia == TendenciaGrupo.Bajo).ToList();
        var subieron = grupos.Where(g => g.Tendencia == TendenciaGrupo.Subio).ToList();
        var estables = grupos.Where(g => g.Tendencia == TendenciaGrupo.Estable).ToList();

        var insightPerdidos = ConstruirInsightPerdidos(bajaron, totalGrupos);
        var insightGanadores = ConstruirInsightGanadores(subieron, totalGrupos);
        var insightEstables = ConstruirInsightEstables(estables);

        var gruposTop = grupos
            .OrderByDescending(g => Math.Abs(g.DeltaViajes))
            .ThenBy(g => g.Nombre, StringComparer.Ordinal)
            .Take(topN)
            .ToList();

        return new ComparativaMensualDto(
            meses[^2].Etiqueta,
            meses[^1].Etiqueta,
            etiquetaAgrupacion,
            totalGrupos,
            bajaron.Count,
            subieron.Count,
            estables.Count,
            insightPerdidos,
            insightGanadores,
            insightEstables,
            gruposTop);
    }

    private static List<GrupoFuente> RecolectarFuentesJerarquia(NodoJerarquia raiz, string claveAnt, string claveAct)
    {
        var destino = new List<GrupoFuente>();
        var ruta = new List<string>();

        foreach (var hijo in raiz.Hijos.Values)
            RecolectarHojas(hijo, ruta, destino, claveAnt, claveAct);

        return destino;
    }

    private static void RecolectarHojas(NodoJerarquia nodo, List<string> ruta, List<GrupoFuente> destino, string claveAnt, string claveAct)
    {
        ruta.Add(nodo.Label);

        if (nodo.Hijos.Count == 0)
        {
            destino.Add(new GrupoFuente(string.Join(SeparadorRuta, ruta), nodo.ObtenerMes(claveAnt), nodo.ObtenerMes(claveAct)));
        }
        else
        {
            foreach (var hijo in nodo.Hijos.Values)
                RecolectarHojas(hijo, ruta, destino, claveAnt, claveAct);
        }

        ruta.RemoveAt(ruta.Count - 1);
    }

    private static GrupoComparativo ClasificarGrupo(GrupoFuente f)
    {
        var dV = f.Actual.Viajes - f.Anterior.Viajes;
        var dVta = f.Actual.Venta - f.Anterior.Venta;
        var pctV = f.Anterior.Viajes > 0 ? MetricasColumnasHelper.PorcentajeCambio(f.Anterior.Viajes, f.Actual.Viajes) : (decimal?)null;
        var pctVta = f.Anterior.Venta > 0 ? MetricasColumnasHelper.PorcentajeCambio(f.Anterior.Venta, f.Actual.Venta) : (decimal?)null;

        var esEstable = pctV is not null
            && Math.Abs(pctV.Value) <= UmbralEstabilidadPct
            && (pctVta is null ? dVta >= 0 : pctVta.Value >= -UmbralEstabilidadPct);

        TendenciaGrupo tendencia;
        if (esEstable)
            tendencia = TendenciaGrupo.Estable;
        else if (dV < 0)
            tendencia = TendenciaGrupo.Bajo;
        else if (dV > 0)
            tendencia = TendenciaGrupo.Subio;
        else
            tendencia = TendenciaGrupo.SinCambioViajes;

        var esRevisar = tendencia == TendenciaGrupo.Bajo
            && f.Anterior.Km > 0 && f.Actual.Km > 0
            && f.Actual.VentaPorKm > f.Anterior.VentaPorKm;

        var (badge, badgeVariant) = tendencia switch
        {
            TendenciaGrupo.Bajo when esRevisar => ("Revisar: $/km sube pese a la caída", Variant.Warning),
            TendenciaGrupo.Bajo => ("Menos viajes", Variant.Danger),
            TendenciaGrupo.Subio => ("Más viajes", Variant.Success),
            TendenciaGrupo.Estable => ("Estable", Variant.Neutral),
            _ => ("Sin cambio en viajes", Variant.Neutral),
        };

        return new GrupoComparativo(
            f.Nombre,
            f.Anterior.Viajes, f.Actual.Viajes, dV, pctV,
            f.Anterior.Venta, f.Actual.Venta, dVta, pctVta,
            tendencia, badge, badgeVariant);
    }

    private static InsightResumen ConstruirInsightPerdidos(List<GrupoComparativo> bajaron, int totalGrupos)
    {
        var n = bajaron.Count;
        var sumaDv = bajaron.Sum(g => g.DeltaViajes);
        var valorFormateado = "-" + FormatoUi.Numero(Math.Abs(sumaDv));
        var variante = n == 0 ? Variant.Neutral : Variant.Danger;

        if (n == 0)
            return new InsightResumen(valorFormateado, variante, new MarkupString("Ningún grupo bajó viajes en este corte."));

        var peor = bajaron.OrderBy(g => g.DeltaViajes).First();
        var nombrePeor = WebUtility.HtmlEncode(peor.Nombre);
        var texto = $"<strong>{n}</strong> de {totalGrupos} grupos bajaron; la mayor caída: <strong>{nombrePeor}</strong> ({WebUtility.HtmlEncode(FormatoUi.Numero(Math.Abs(peor.DeltaViajes)))} viajes).";

        return new InsightResumen(valorFormateado, variante, new MarkupString(texto));
    }

    private static InsightResumen ConstruirInsightGanadores(List<GrupoComparativo> subieron, int totalGrupos)
    {
        var n = subieron.Count;
        var sumaDv = subieron.Sum(g => g.DeltaViajes);
        var valorFormateado = "+" + FormatoUi.Numero(sumaDv);
        var variante = n == 0 ? Variant.Neutral : Variant.Success;

        if (n == 0)
            return new InsightResumen(valorFormateado, variante, new MarkupString("Ningún grupo subió viajes en este corte."));

        var mejor = subieron.OrderByDescending(g => g.DeltaViajes).First();
        var nombreMejor = WebUtility.HtmlEncode(mejor.Nombre);
        var texto = $"<strong>{n}</strong> de {totalGrupos} grupos subieron; el mayor aumento: <strong>{nombreMejor}</strong> ({WebUtility.HtmlEncode(FormatoUi.Numero(mejor.DeltaViajes))} viajes).";

        return new InsightResumen(valorFormateado, variante, new MarkupString(texto));
    }

    private static InsightResumen ConstruirInsightEstables(List<GrupoComparativo> estables)
    {
        var n = estables.Count;

        if (n == 0)
            return new InsightResumen("0", Variant.Neutral, new MarkupString(TextoSinEstables));

        var citado = estables.OrderByDescending(g => g.ViajesMesActual).First();
        var nombreCitado = WebUtility.HtmlEncode(citado.Nombre);
        var texto = $"<strong>{n}</strong> grupos se mantuvieron (±1% viajes, venta estable o al alza), p. ej. <strong>{nombreCitado}</strong>.";

        return new InsightResumen(FormatoUi.Numero(n), Variant.Neutral, new MarkupString(texto));
    }
}
