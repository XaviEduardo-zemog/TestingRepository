namespace Testing.Application.GetAllViajes;

/// <summary>
/// Una fila de ZemogViajesEnZamAnual (más el LEFT JOIN a Sucursales), ya proyectada a los
/// campos que el Prompt 2 pide enriquecer. Cliente/Zona/Matriz son nullable porque vienen de
/// Sucursales vía LEFT JOIN -- si IdSucursal no tiene contraparte en Sucursales, esos tres
/// quedan en null (diagnosticable, ver CisEnrichmentBatchResult), nunca en "(sin dato)" oculto.
///
/// AJUSTE (2026-09-09): se agregan Ruta/Origen/EstadoOrigen -- pedidos explícitamente por el
/// usuario para completar el mismo set de columnas de su consulta manual de verificación
/// (Cliente/Zona/Matriz/Folio/Ruta/Origen/Destino/EstadoOrigen/EstadoDestino/TotalVenta/Ejes).
/// No participan en la jerarquía Cliente→Zona→Matriz→Sucursal ni en ningún cálculo -- quedan
/// disponibles para diagnóstico/uso futuro, igual que Destino/EstadoDestino ya lo estaban.
/// </summary>
public sealed record DatosCisViaje(
    string Folio,
    string? Cliente,
    string? Zona,
    string? Matriz,
    string Sucursal,
    int IdSucursal,
    string Ruta,
    string Origen,
    string Destino,
    string EstadoOrigen,
    string EstadoDestino,
    decimal TotalVenta,
    int? EjesEquipos,
    DateOnly FechaCalendario,
    string Trayecto);

public sealed record CisEnrichmentBatchResult(IReadOnlyDictionary<string, DatosCisViaje> PorFolio, IReadOnlyCollection<string> FoliosSinCoincidencia, IReadOnlySet<string> FoliosDuplicados)
{
    public static readonly CisEnrichmentBatchResult Vacio = new(new Dictionary<string, DatosCisViaje>(), [], new HashSet<string>());
}