namespace Testing.Application.GetAllViajes;

/// <summary>
/// Una fila de ZemogViajesEnZamAnual (más el LEFT JOIN a Sucursales), ya proyectada a los
/// campos que el Prompt 2 pide enriquecer. Cliente/Zona/Matriz son nullable porque vienen de
/// Sucursales vía LEFT JOIN -- si IdSucursal no tiene contraparte en Sucursales, esos tres
/// quedan en null (diagnosticable, ver CisEnrichmentBatchResult), nunca en "(sin dato)" oculto.
/// Ruta/Origen/EstadoOrigen NO se incluyen todavía -- no se necesitan para esta etapa (ver
/// decisión técnica en el Artifact); agregarlos después es trivial (una columna más en la
/// proyección de CisViajeEnrichmentRepository).
/// </summary>
public sealed record DatosCisViaje(
    string Folio,
    string? Cliente,
    string? Zona,
    string? Matriz,
    string Sucursal,
    int IdSucursal,
    string Destino,
    string EstadoDestino,
    decimal TotalVenta,
    int? EjesEquipos,
    DateOnly FechaCalendario,
    string Trayecto);

/// <summary>
/// Resultado de una consulta EN LOTE (nunca una por viaje) contra CIS_DB para un conjunto de
/// Folios. Separa explícitamente los tres casos que el Prompt 2 pide diagnosticar sin ocultar:
/// PorFolio (coincidencia única y segura), FoliosSinCoincidencia (el SP trae ese no_remision
/// pero CIS no tiene esa fila) y FoliosDuplicados (CIS tiene MÁS de una fila con el mismo
/// Folio -- se reportan, nunca se resuelven con First()/Single() ni se sobrescriben en el
/// diccionario).
/// </summary>
public sealed record CisEnrichmentBatchResult(IReadOnlyDictionary<string, DatosCisViaje> PorFolio, IReadOnlyCollection<string> FoliosSinCoincidencia, IReadOnlySet<string> FoliosDuplicados)
{
    public static readonly CisEnrichmentBatchResult Vacio = new(new Dictionary<string, DatosCisViaje>(), [], new HashSet<string>());
}