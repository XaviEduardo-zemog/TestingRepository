namespace Testing.Application.GetAllViajes;

/// <summary>
/// Una fila de ZemogViajesEnZamAnual (CIS_DB), con el LEFT JOIN a Sucursales --
/// NombreCorto/Region/Nomenclatura son nullable porque vienen del LEFT JOIN: si IdSucursal no tiene contraparte en Sucursales, quedan en
/// null -- nunca se inventa un valor ni se cae de vuelta a datos del SP.
/// </summary>
public sealed record DatosCisViaje(
    string Folio,
    string? FolioComplemento,
    int IdSucursal,
    string Sucursal,
    string? NombreCorto,
    string? Region,
    string? Nomenclatura,
    string Origen,
    string Destino,
    string EstadoOrigen,
    string EstadoDestino,
    decimal TotalVenta,
    int? EjesEquipos,
    DateOnly FechaCalendario,
    string Trayecto,
    string Operacion);

/// <summary>
/// Resultado de una consulta EN LOTE (nunca una por fila) contra CIS_DB para un conjunto de
/// candidatos de Folio. Separa los 2 casos que Prompt 2 exige diagnosticar sin ocultar:
/// PorFolio (coincidencia única) y FoliosDuplicados (2+ filas de CIS_DB comparten el mismo
/// Folio -- se reportan, nunca se resuelven con First()/Single() ni se sobrescriben con
/// Dictionary[key] = value).
/// </summary>
public sealed record CisEnrichmentBatchResult(
    IReadOnlyDictionary<string, DatosCisViaje> PorFolio,
    IReadOnlySet<string> FoliosDuplicados)
{
    public static readonly CisEnrichmentBatchResult Vacio = new(new Dictionary<string, DatosCisViaje>(), new HashSet<string>());
}