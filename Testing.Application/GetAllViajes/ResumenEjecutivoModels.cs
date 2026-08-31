namespace Testing.Application.GetAllViajes;

/// <summary>Un mes cerrado real (no el mes abierto/parcial) — ver ResumenEjecutivoCalculator.CalcularMesesCerrados.</summary>
public readonly record struct MesCerrado(int Anio, int Mes, string Etiqueta);

/// <summary>
/// Totales de un período. NO incluye Venta: TotalVenta sigue sin fuente (Fase 5 / §54.85) — no
/// se modela como propiedad nullable aquí a propósito, para que cada bloque que la necesite lo
/// declare explícitamente como Pendiente en su propio DTO, en vez de un null silencioso que
/// alguien podría malinterpretar como "cero viajes".
/// </summary>
public sealed record TotalesPeriodo(int Viajes, decimal Kms)
{
    public static readonly TotalesPeriodo Vacio = new(0, 0);

    public static TotalesPeriodo Sumar(TotalesPeriodo a, TotalesPeriodo b) => new(a.Viajes + b.Viajes, a.Kms + b.Kms);
}

public enum SeveridadAlerta { Neutral, Positiva, Negativa }

/// <summary>Una línea del semáforo (Bloque 8.1). Replica los sem.push(...) de RE_render() en viajes_v14.html.</summary>
public sealed record AlertaSemaforo(string Texto, SeveridadAlerta Severidad);

/// <summary>
/// Replica RE_bloqueNivel() — usado tal cual para "Zemog · Nivel general" (8.2) y, una vez por
/// cliente, para "Por Cliente" (8.3). Comparativo vs mes anterior, comparativo vs primer mes
/// cerrado del año en curso ("avance del año"), y la tendencia mes a mes de los meses cerrados.
/// "Peor mes" del HTML se define por Venta mínima — sin fuente, no se calcula (ver Fase 8 /
/// §54.120): esta clase no tiene una propiedad "PeorMes" en absoluto, a propósito.
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
            ? (decimal)(TotalesUltimo.Viajes - TotalesAnterior.Viajes) / TotalesAnterior.Viajes * 100
            : null;

    public decimal? DeltaKmsPctVsAnterior =>
        TotalesAnterior is { Kms: > 0 }
            ? (TotalesUltimo.Kms - TotalesAnterior.Kms) / TotalesAnterior.Kms * 100
            : null;

    public bool HayComparativoVsEnero => PrimerMesDelAnio != MesUltimo;

    public decimal? DeltaViajesPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Viajes > 0
            ? (decimal)(TotalesUltimo.Viajes - TotalesPrimerMesDelAnio.Viajes) / TotalesPrimerMesDelAnio.Viajes * 100
            : null;

    public decimal? DeltaKmsPctVsEnero =>
        HayComparativoVsEnero && TotalesPrimerMesDelAnio.Kms > 0
            ? (TotalesUltimo.Kms - TotalesPrimerMesDelAnio.Kms) / TotalesPrimerMesDelAnio.Kms * 100
            : null;
}

/// <summary>Un cliente y su BloqueNivelDto (Bloque 8.3 — misma lógica de 8.2, acotada a sus propios viajes).</summary>
public sealed record NivelPorClienteDto(string Cliente, BloqueNivelDto Bloque);

/// <summary>
/// Nodo del árbol comparativo Cliente›Zona›Matriz›Sucursal compartido por los Bloques 8.4
/// (comparativo), 8.6 (asignación Comodato/Full/Sencillo) y 8.7 (frecuencia) — igual que
/// RE_jerState.comp.arbol en viajes_v14.html, construido una sola vez y reutilizado por los 3.
/// ExpedicionUltimo/ExpedicionAnterior cuentan SOLO tramos de Ida por valor crudo de
/// "Expedicion" (replica n.em[r.exp]+=r.viaje, donde r.viaje ya es 0 para tramos de Regreso).
/// </summary>
public sealed class NodoComparativo
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required int Nivel { get; init; }
    public Dictionary<string, NodoComparativo> Hijos { get; } = [];
    public TotalesPeriodo Ultimo { get; set; } = TotalesPeriodo.Vacio;
    public TotalesPeriodo Anterior { get; set; } = TotalesPeriodo.Vacio;
    public TotalesPeriodo Anual { get; set; } = TotalesPeriodo.Vacio;
    public Dictionary<string, int> ExpedicionUltimo { get; } = [];
    public Dictionary<string, int> ExpedicionAnterior { get; } = [];
    public bool IsExpanded { get; set; }
}

/// <summary>Bloque 8.7 — una alerta de caída de frecuencia (Δ% viajes ≤ -15% con base ≥ 20, nivel Matriz o más profundo).</summary>
public sealed record AlertaFrecuencia(string Matriz, decimal DeltaPorcentaje);

/// <summary>
/// Bloque 8.8 — una agencia (Destino×Matriz) que dejó de aparecer: tuvo viajes > 0 en algún
/// mes cerrado del año, pero su último mes con viajes es anterior al último mes cerrado global.
/// VentaProm/VentaAcumulada del HTML no se modelan — dependen de Venta (Pendiente, ver §54.85).
/// </summary>
public sealed record AgenciaDesaparecidaDto(
    string Destino,
    string Matriz,
    MesCerrado UltimoMesActivo,
    int ViajesEnEseMes,
    int MesesActiva);

// OperadorFilaDto, OperadoresResumenDto, RotacionSucursalDto y RotacionOperadoresDto (Bloques
// 8.9/8.10) viven ahora en OperadoresRotacionModels.cs — extraídos en la Fase 9 a su propio
// archivo, tratados como una unidad independiente. Siguen en el mismo namespace
// (Testing.Application.GetAllViajes), así que ResumenEjecutivoDto no necesita ningún using
// adicional para referenciarlos.

/// <summary>
/// Resultado completo del Resumen Ejecutivo — construido UNA vez por ResumenEjecutivoCalculator.Calcular
/// y consumido de solo lectura por ResumenEjecutivo.razor y sus componentes hijos.
/// </summary>
public sealed record ResumenEjecutivoDto(
    IReadOnlyList<MesCerrado> MesesCerrados,
    bool MesAbiertoExcluido,
    string? EtiquetaMesAbiertoExcluido,
    bool HayComparativos,
    IReadOnlyList<AlertaSemaforo> Semaforo,
    BloqueNivelDto? NivelZemog,
    IReadOnlyList<NivelPorClienteDto> PorCliente,
    NodoComparativo? ArbolComparativo,
    IReadOnlyList<AgenciaDesaparecidaDto> AgenciasDesaparecidas,
    OperadoresResumenDto Operadores,
    RotacionOperadoresDto Rotacion);