namespace Testing.Application.GetAllViajes;

/// <summary>
/// Un mes presente en los datos cargados. El nombre "MesCerrado" es heredado de una fase
/// anterior (cuando se creía, incorrectamente, que Resumen Ejecutivo excluía el mes abierto) --
/// se conserva el nombre para no romper firmas en cascada, pero desde esta fase (auditoría de
/// ajuste fino): NO implica que el mes esté "cerrado" para Resumen Ejecutivo. RE_render() del
/// HTML original se construye directo sobre DATA, sin excluir el mes de corte -- confirmado
/// línea por línea, ver Artifact. Solo Presentación conserva una regla propia de exclusión del
/// mes de "avance" (día de corte &lt; 28), separada por completo de este tipo.
/// </summary>
public readonly record struct MesCerrado(int Anio, int Mes, string Etiqueta);

/// <summary>
/// Totales de un período, YA PROYECTADOS con CorteMensual.FactorPara cuando corresponde (mismo
/// criterio que la vista principal -- "DATA ya contiene valores proyectados del mes de corte").
/// Viajes es decimal (no int) para poder proyectarse con factorMes fraccionario (ej. ×3.1) sin
/// perder precisión por redondeos intermedios -- se redondea UNA sola vez, al formatear para
/// mostrar (Math.Round en cada Razor), nunca durante la acumulación.
/// TotalVenta = subtotal_factura, fuente temporal confirmada por el usuario (ver §54.85).
/// $/KM es SIEMPRE razón de acumulados (Venta/Kms de este mismo TotalesPeriodo ya sumado) --
/// nunca promedio de razones de fila individual.
/// </summary>
public sealed record TotalesPeriodo(decimal Viajes, decimal Kms, decimal Venta)
{
    public decimal PorKm => Kms > 0 ? Venta / Kms : 0;
    public decimal PorViaje => Viajes > 0 ? Venta / Viajes : 0;

    public static readonly TotalesPeriodo Vacio = new(0, 0, 0);

    public static TotalesPeriodo Sumar(TotalesPeriodo a, TotalesPeriodo b) => new(a.Viajes + b.Viajes, a.Kms + b.Kms, a.Venta + b.Venta);

    /// <summary>Contribución proyectada de UN viaje -- construye directo desde ContribucionViajeProyectada, el único punto que lee las columnas físicas del DTO.</summary>
    public static TotalesPeriodo De(ViajesDto viaje, CorteMensual? corte) => new(
        ContribucionViajeProyectada.Viajes(viaje, corte),
        ContribucionViajeProyectada.Kms(viaje, corte),
        ContribucionViajeProyectada.Venta(viaje, corte));
}

public enum SeveridadAlerta { Neutral, Positiva, Negativa }

/// <summary>Una línea del semáforo (Bloque 8.1). Replica los sem.push(...) de RE_render() en viajes_v14.html.</summary>
public sealed record AlertaSemaforo(string Texto, SeveridadAlerta Severidad);

/// <summary>
/// Replica RE_bloqueNivel() — usado tal cual para "Zemog · Nivel general" (8.2) y, una vez por
/// cliente, para "Por Cliente" (8.3). "PrimerMesDelAnio"/"HayComparativoVsEnero" son nombres
/// heredados: el HTML llama a esto "vs Enero (avance del año)" en la UI, pero la variable real es
/// literal "mesesOrdenados[0]" (el primer mes de TODO el rango cargado, sin filtrar por año) --
/// confirmado en esta fase. Se conserva el nombre de la propiedad, se corrige el cálculo (ya NO
/// filtra por año).
/// </summary>
public sealed record BloqueNivelDto(
    string Titulo,
    MesCerrado? MesAnterior,
    TotalesPeriodo? TotalesAnterior,
    MesCerrado MesUltimo,
    TotalesPeriodo TotalesUltimo,
    MesCerrado PrimerMesDelAnio,
    TotalesPeriodo TotalesPrimerMesDelAnio,
    IReadOnlyList<(MesCerrado Mes, TotalesPeriodo Totales)> Tendencia)
{
    public decimal? DeltaViajesPctVsAnterior =>
        TotalesAnterior is { Viajes: > 0 }
            ? (TotalesUltimo.Viajes - TotalesAnterior.Viajes) / TotalesAnterior.Viajes * 100
            : null;

    public decimal? DeltaKmsPctVsAnterior =>
        TotalesAnterior is { Kms: > 0 }
            ? (TotalesUltimo.Kms - TotalesAnterior.Kms) / TotalesAnterior.Kms * 100
            : null;

    public decimal? DeltaVentaPctVsAnterior =>
        TotalesAnterior is { Venta: > 0 }
            ? (TotalesUltimo.Venta - TotalesAnterior.Venta) / TotalesAnterior.Venta * 100
            : null;

    public decimal? DeltaPkmPctVsAnterior =>
        TotalesAnterior is { Kms: > 0 } anterior && anterior.PorKm > 0
            ? (TotalesUltimo.PorKm - anterior.PorKm) / anterior.PorKm * 100
            : null;

    public bool HayComparativoVsEnero => PrimerMesDelAnio != MesUltimo;

    public decimal? DeltaViajesPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Viajes > 0
            ? (TotalesUltimo.Viajes - TotalesPrimerMesDelAnio.Viajes) / TotalesPrimerMesDelAnio.Viajes * 100
            : null;

    public decimal? DeltaKmsPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Kms > 0
            ? (TotalesUltimo.Kms - TotalesPrimerMesDelAnio.Kms) / TotalesPrimerMesDelAnio.Kms * 100
            : null;

    public decimal? DeltaVentaPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Venta > 0
            ? (TotalesUltimo.Venta - TotalesPrimerMesDelAnio.Venta) / TotalesPrimerMesDelAnio.Venta * 100
            : null;

    /// <summary>
    /// AJUSTE — Correcciones puntuales: faltaba el equivalente a DeltaPkmPctVsAnterior para la
    /// comparativa "vs Enero (avance del año)" -- RE_tablaComparativa aplica la misma fila $/KM a
    /// ambas comparativas (vs mes anterior Y vs Enero), esta propiedad solo cubría la primera.
    /// </summary>
    public decimal? DeltaPkmPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Kms > 0 && TotalesPrimerMesDelAnio.PorKm > 0
            ? (TotalesUltimo.PorKm - TotalesPrimerMesDelAnio.PorKm) / TotalesPrimerMesDelAnio.PorKm * 100
            : null;

    /// <summary>Peor/mejor mes por Venta -- sobre TODA la Tendencia (todo el rango cargado, sin filtrar por año; RE_porMes/RE_tablaTendencia tampoco filtran por año).</summary>
    public (MesCerrado Mes, decimal Venta)? PeorMesDelAnio => MesConVentaExtrema(masAlta: false);

    public (MesCerrado Mes, decimal Venta)? MejorMesDelAnio => MesConVentaExtrema(masAlta: true);

    private (MesCerrado Mes, decimal Venta)? MesConVentaExtrema(bool masAlta)
    {
        if (Tendencia.Count == 0)
            return null;

        var elegido = masAlta
            ? Tendencia.OrderByDescending(t => t.Totales.Venta).First()
            : Tendencia.OrderBy(t => t.Totales.Venta).First();

        return (elegido.Mes, elegido.Totales.Venta);
    }
}

public sealed record NivelPorClienteDto(string Cliente, BloqueNivelDto Bloque);

public sealed class NodoComparativo
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required int Nivel { get; init; }
    public Dictionary<string, NodoComparativo> Hijos { get; } = [];
    public TotalesPeriodo Ultimo { get; set; } = TotalesPeriodo.Vacio;
    public TotalesPeriodo Anterior { get; set; } = TotalesPeriodo.Vacio;
    public TotalesPeriodo Anual { get; set; } = TotalesPeriodo.Vacio;
    public Dictionary<string, decimal> ArmadoUltimo { get; } = [];
    public Dictionary<string, decimal> ArmadoAnterior { get; } = [];
    public Dictionary<string, decimal> ArmadoVentaUltimo { get; } = [];
    public Dictionary<string, decimal> ArmadoVentaAnterior { get; } = [];
    public bool IsExpanded { get; set; }
}

/// <summary>Bloque 8.7 (RE_jerFrec) — una fila de la tabla de frecuencia, a CUALQUIER nivel del árbol (no solo Matriz+). Señal se calcula para todas; el semáforo (8.1) es el único que filtra a nivel&gt;=2.</summary>
public sealed record FilaFrecuenciaDto(int Nivel, string Label, decimal ViajesAnterior, decimal ViajesUltimo, decimal? DeltaPorcentaje, decimal VentaUltimo, bool Alerta);

/// <summary>Bloque 8.7 — solo la lista de alertas (nivel&gt;=2, para el semáforo). La tabla completa (todos los niveles) vive en FilaFrecuenciaDto / ResumenArbolComparativo.razor.</summary>
public sealed record AlertaFrecuencia(string Matriz, decimal DeltaPorcentaje);

public sealed record DestinoCayendoDto(string Destino, string Matriz, decimal ViajesAnterior, decimal ViajesActual, decimal VentaAnterior, decimal VentaActual)
{
    public decimal DeltaVenta => VentaActual - VentaAnterior; // negativo = caída
    public bool SeDejoDeDar => ViajesActual == 0 && ViajesAnterior > 0;
}

public sealed record DestinosCayendoResumenDto(
    int TotalConCaida,
    decimal ImpactoTotal,
    IReadOnlyList<DestinoCayendoDto> Top25)
{
    public decimal TotalTop25 => Top25.Sum(d => d.DeltaVenta);
}

public sealed record AgenciaDesaparecidaDto(
    string Destino,
    string Matriz,
    MesCerrado UltimoMesActivo,
    decimal ViajesEnEseMes,
    int MesesActiva,
    decimal VentaAcumulada)
{
    public decimal VentaPromedio => MesesActiva > 0 ? VentaAcumulada / MesesActiva : 0;
}

public sealed record ResumenEjecutivoDto(
    IReadOnlyList<MesCerrado> MesesCerrados,
    bool HayComparativos,
    IReadOnlyList<AlertaSemaforo> Semaforo,
    BloqueNivelDto? NivelZemog,
    IReadOnlyList<NivelPorClienteDto> PorCliente,
    NodoComparativo? ArbolComparativo,
    DestinosCayendoResumenDto? DestinosCayendo,
    IReadOnlyList<AgenciaDesaparecidaDto> AgenciasDesaparecidas,
    OperadoresResumenDto Operadores,
    RotacionOperadoresDto Rotacion,
    IReadOnlyList<(string Valor, decimal Viajes)> ArmadosDesconocidos);