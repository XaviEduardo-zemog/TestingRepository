using System.Globalization;
using Testing.Components.Shared;

namespace Testing.Features;

/// <summary>
/// Columna de métrica de Tabla Pivote / Árbol de Jerarquía (Etapa C, docs/ETAPA_C_METRICAS_COLUMNAS.md).
/// Antes era un record privado duplicado carácter por carácter en TablaPivoteViajes.razor y
/// ArbolJerarquiaViajes.razor -- ahora es uno solo, público, en Testing.Features (mismo namespace
/// que MetricasMes/FilaPivote/NodoJerarquia, ya importado en ambos componentes).
/// </summary>
public sealed record ColumnaMetrica(string Clave, string Etiqueta, Func<MetricasMes, decimal> Selector, Func<decimal, string> Formato, bool EsSumable);

/// <summary>
/// Motor compartido de columnas/métricas de Tabla Pivote y Árbol de Jerarquía (Etapa C). Presentation
/// puro: no contiene ninguna regla de negocio nueva, solo la construcción de columnas y el formato/
/// comparación de celdas que ya existían -- duplicados byte a byte -- en ambos componentes. Ninguna
/// fórmula cambió: cada método es el cuerpo original copiado tal cual.
///
/// ClaseDelta/ClaseCelda (clases tp-*/aj-*) NO se incluyen aquí a propósito -- quedan locales en cada
/// componente, ver docs/ETAPA_C_METRICAS_COLUMNAS.md §4.
/// </summary>
public static class MetricasColumnasHelper
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    /// <summary>
    /// Construye la lista de columnas visibles según los 7 flags Mostrar* -- mismo orden, mismas
    /// claves/etiquetas/selectores/formatos/EsSumable que ya usaba cada componente por separado.
    /// Reutiliza FormatoUi.Numero/FormatoUi.Dinero (Etapa B), no duplica formato.
    /// </summary>
    public static List<ColumnaMetrica> ConstruirColumnas(
        bool mostrarViajes,
        bool mostrarKms,
        bool mostrarPeaje,
        bool mostrarVenta,
        bool mostrarPkm,
        bool mostrarKmPorViaje,
        bool mostrarPvj)
    {
        var columnas = new List<ColumnaMetrica>();
        if (mostrarViajes) columnas.Add(new("viajes", "Viajes", m => m.Viajes, FormatoUi.Numero, true));
        if (mostrarKms) columnas.Add(new("kms", "KMS", m => m.Km, FormatoUi.Numero, true));
        if (mostrarPeaje) columnas.Add(new("peaje", "Peaje", m => m.Peaje, FormatoUi.Dinero, true));
        if (mostrarVenta) columnas.Add(new("venta", "Venta", m => m.Venta, FormatoUi.Dinero, true));
        if (mostrarPkm) columnas.Add(new("pkm", "$/KM", m => m.VentaPorKm, FormatoUi.Dinero, false));
        if (mostrarKmPorViaje) columnas.Add(new("kmv", "km/Viaje", m => m.KmPorViaje, FormatoUi.Numero, false));
        if (mostrarPvj) columnas.Add(new("pvj", "$/Viaje", m => m.VentaPorViaje, FormatoUi.Dinero, false));
        return columnas;
    }

    public static decimal ObtenerMetricaPorClave(MetricasMes m, string clave) => clave switch
    {
        "viajes" => m.Viajes,
        "kms" => m.Km,
        "peaje" => m.Peaje,
        "venta" => m.Venta,
        "pkm" => m.VentaPorKm,
        "kmv" => m.KmPorViaje,
        "pvj" => m.VentaPorViaje,
        _ => 0,
    };

    public static decimal Participacion(decimal valor, decimal total) => total > 0 ? valor / total * 100 : 0;

    public static decimal PorcentajeCambio(decimal a, decimal b) => a != 0 ? (b - a) / a * 100 : 0;

    public static string FormatoDeltaCelda(decimal a, decimal b, Func<decimal, string> formato)
    {
        if (a == 0 && b == 0)
            return "—";

        var d = b - a;
        return (d >= 0 ? "+" : "-") + formato(Math.Abs(d));
    }

    public static string FormatoPctCelda(decimal a, decimal b)
    {
        if (a == 0)
            return b != 0 ? "nuevo" : "—";

        var pct = PorcentajeCambio(a, b);
        return (pct >= 0 ? "+" : "") + pct.ToString("0.0", Cultura) + "%";
    }

    /// <summary>
    /// "0.0" + "%" con un espacio inicial -- formato de la celda de % participación en Tabla/Árbol.
    /// Distinto a propósito de FormatoUi.Porcentaje (sin espacio, usado por la barra Ejes en
    /// ConsultaViajes.razor) -- ver docs/ETAPA_B_FORMATO_UI.md §3.
    /// </summary>
    public static string FormatoPorcentaje(decimal v) => $" {v.ToString("0.0", Cultura)}%";
}