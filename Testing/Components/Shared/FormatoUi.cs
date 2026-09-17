using System.Globalization;
using Testing.Application.GetAllViajes;

namespace Testing.Components.Shared;

public static class FormatoUi
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    /// <summary>"N0" redondeado -- igual que Math.Round(v).ToString("N0", Cultura) en cada componente.</summary>
    public static string Numero(decimal valor) => Math.Round(valor).ToString("N0", Cultura);

    /// <summary>"N0" para valores ya enteros (p. ej. MesesActiva en ResumenAgenciasDesaparecidas.razor).</summary>
    public static string Numero(int valor) => valor.ToString("N0", Cultura);

    /// <summary>"C0" -- igual que v.ToString("C0", Cultura) en cada componente.</summary>
    public static string Dinero(decimal valor) => valor.ToString("C0", Cultura);

    /// <summary>
    /// "0.0" + "%", sin signo -- igual que el FormatoPct que usaba ConsultaViajes.razor para la
    /// barra Ejes. NO es el mismo formato que TablaPivoteViajes.razor/ArbolJerarquiaViajes.razor
    /// (esos llevan un espacio inicial " 0.0%" para las celdas de participación) -- esa variante
    /// se dejó fuera a propósito, ver docs/ETAPA_B_FORMATO_UI.md §3.
    /// </summary>
    public static string Porcentaje(decimal valor) => valor.ToString("0.0", Cultura) + "%";

    /// <summary>
    /// "—" si es null; si no, ResumenEjecutivoCalculator.FormatoPorcentaje(valor) (con signo +/-).
    /// Igual que FormatoDeltaPct/FormatoPct repetido en ResumenBloqueNivel.razor,
    /// ResumenRotacionOperadores.razor, ResumenArbolComparativo.razor y ResumenPresentacion.razor.
    /// </summary>
    public static string DeltaPorcentaje(decimal? valor) => valor is null ? "—" : ResumenEjecutivoCalculator.FormatoPorcentaje(valor.Value);

    /// <summary>
    /// Clase CSS re-pos/re-neg/re-neutro -- igual que ClaseDelta repetido en
    /// ResumenBloqueNivel.razor, ResumenRotacionOperadores.razor y ResumenArbolComparativo.razor.
    /// Nombrado "Resumen" a propósito: TablaPivoteViajes.razor/ArbolJerarquiaViajes.razor tienen su
    /// propio ClaseDelta(a, b) con clases tp-/aj- (firma y prefijo distintos) -- ese NO debe usar
    /// este método, queda fuera de esta etapa (ver docs/ETAPA_B_FORMATO_UI.md §3).
    /// </summary>
    public static string ClaseDeltaResumen(decimal? valor) => valor switch
    {
        null => "re-neutro",
        > 0 => "re-pos",
        < 0 => "re-neg",
        _ => "re-neutro",
    };
}