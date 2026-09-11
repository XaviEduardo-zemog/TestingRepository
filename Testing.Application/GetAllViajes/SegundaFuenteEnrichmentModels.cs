namespace Testing.Application.GetAllViajes;

/// <summary>
/// Sucursales (CIS_DB), resuelta por SP._base = Sucursales.Nomenclatura -- fallback de
/// jerarquía cuando CIS_DB no pudo resolver por Folio. Nomenclatura está confirmada única
/// (verificado en vivo antes de esta implementación: 0 filas con Nomenclatura repetida).
/// </summary>
public sealed record DatosSucursalFallback(
    string? Cliente,
    string? Zona,
    string Matriz,
    int IdSucursal,
    string? Sucursal);

/// <summary>
/// RutasZam (CIS_DB), resuelta por el primer token de SP.ruta (antes del primer espacio) =
/// RutasZam.Codigo -- fallback de Origen/Destino/Kms. Codigo NO es único (confirmado: pueden
/// existir 2+ filas con el mismo Codigo y distinto Destino/Kms) -- ver
/// <see cref="SegundaFuenteBatchResult.RutasCodigosAmbiguos"/>.
/// </summary>
public sealed record DatosRutaFallback(
    string Codigo,
    string Ruta,
    string Origen,
    string Destino,
    decimal Kms);

/// <summary>
/// trafico_guia (ZemogDB), resuelta por SP.factura = TraficoGuium.NumGuia -- fallback de Venta
/// (la fuente correcta para lo que CIS_DB no resuelve, confirmado por decisión explícita del
/// usuario). El cruce con SP.no_viaje = TraficoGuium.NoViaje se valida en el handler, no aquí --
/// esta fila solo dice "qué hay en trafico_guia para esta Factura", nunca se acepta como match
/// sin verificar también NoViaje (la llave lógica es Factura + NoViaje, nunca NoViaje solo).
/// </summary>
public sealed record DatosTraficoGuiaFallback(
    string NumGuia,
    int NoViaje,
    decimal Subtotal,
    int KmsGuia,
    string? NoRemision,
    DateTime FechaGuia,
    string StatusGuia,
    int IdArea,
    int NoGuia);

/// <summary>
/// Resultado de la 2ª fuente de enriquecimiento (fallback), en lote (3 consultas independientes,
/// nunca una por fila). Se consulta ÚNICAMENTE para filas que ya fallaron en CIS_DB
/// (NoEncontrado/Duplicado) -- nunca compite con ni sustituye al enriquecimiento CIS_DB existente.
/// </summary>
public sealed record SegundaFuenteBatchResult(
    IReadOnlyDictionary<string, DatosSucursalFallback> PorBase,
    IReadOnlyDictionary<string, DatosRutaFallback> PorCodigoRuta,
    IReadOnlySet<string> RutasCodigosAmbiguos,
    IReadOnlyDictionary<string, DatosTraficoGuiaFallback> PorFactura)
{
    public static readonly SegundaFuenteBatchResult Vacio = new(
        new Dictionary<string, DatosSucursalFallback>(),
        new Dictionary<string, DatosRutaFallback>(),
        new HashSet<string>(),
        new Dictionary<string, DatosTraficoGuiaFallback>());
}