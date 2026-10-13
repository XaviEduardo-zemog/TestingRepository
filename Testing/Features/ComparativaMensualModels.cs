using Microsoft.AspNetCore.Components;
using Testing.Components.Shared;

namespace Testing.Features;

/// <summary>
/// Contrato de la vista "Gráfica" de ConsultaViajes.razor (VistaComparativaViajes.razor).
/// Presentation puro: lo produce ComparativaMensualCalculator a partir de lo YA calculado por
/// RecalcularPivote() (filasPivote / raizJerarquia + mesesPivote). Sin DI, igual que
/// SegmentosEjesCalculator / PivoteViajesBuilder.
/// </summary>
public enum TendenciaGrupo
{
    /// <summary>DeltaViajes &lt; 0 y no clasifica como estable.</summary>
    Bajo,

    /// <summary>DeltaViajes &gt; 0 y no clasifica como estable.</summary>
    Subio,

    /// <summary>|Δ% viajes| &lt;= umbral y Δ% venta &gt;= -umbral (con base anterior &gt; 0).</summary>
    Estable,

    /// <summary>DeltaViajes == 0 exacto pero la venta cayó más allá del umbral. No cuenta en ninguna tarjeta.</summary>
    SinCambioViajes,
}

/// <summary>
/// Una fila del ranking de barras divergentes. Viajes es decimal (valor proyectado por
/// CorteMensual, ver C-P0-02): se redondea UNA vez al mostrar con FormatoUi.Numero(decimal).
/// Los porcentajes están en puntos porcentuales (12.5 = 12.5 %), null = sin base en el mes
/// anterior (se muestra con FormatoUi.DeltaPorcentaje(decimal?) -> "—").
/// </summary>
public sealed record GrupoComparativo(
    string Nombre,
    decimal ViajesMesAnterior,
    decimal ViajesMesActual,
    decimal DeltaViajes,
    decimal? DeltaViajesPct,
    decimal VentaMesAnterior,
    decimal VentaMesActual,
    decimal DeltaVenta,
    decimal? DeltaVentaPct,
    TendenciaGrupo Tendencia,
    string Badge,
    Variant BadgeVariant);

/// <summary>
/// TextoExplicativo ya viene saneado: todo texto variable (nombres de grupo) pasa por
/// WebUtility.HtmlEncode dentro de ComparativaMensualCalculator antes de envolverse en &lt;strong&gt;.
/// </summary>
public sealed record InsightResumen(string ValorFormateado, Variant Variante, MarkupString TextoExplicativo);

public sealed record ComparativaMensualDto(
    string MesAnteriorLabel,
    string MesActualLabel,
    string EtiquetaAgrupacion,
    int TotalGrupos,
    int GruposBajaron,
    int GruposSubieron,
    int GruposEstables,
    InsightResumen InsightPerdidos,
    InsightResumen InsightGanadores,
    InsightResumen InsightEstables,
    IReadOnlyList<GrupoComparativo> Grupos);
