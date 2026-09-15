using Testing.Application.GetAllViajes;

namespace Testing.Application.Abstractions.Data;

/// <summary>
/// Puerto de Application hacia CIS_DB para la FUENTE B del prototipo paralelo (ver
/// docs/PROTOTIPO_FUENTE_CIS.md): lectura directa de ZemogViajesEnZamAnual por rango de
/// FechaCalendario, sin pasar por el Stored Procedure ni por el matching de Folio. No reemplaza
/// <see cref="ICisViajeEnrichmentRepository"/> (Fuente A) -- ambos coexisten.
/// </summary>
public interface ICisViajesDirectosRepository
{
    /// <summary>
    /// Devuelve TODAS las filas de ZemogViajesEnZamAnual con FechaCalendario en [fechaInicio, fechaFin],
    /// con LEFT JOIN a Sucursales, en UNA sola consulta (sin N+1, sin joins 1:N).
    /// </summary>
    Task<IReadOnlyList<DatosCisViajeDirecto>> ObtenerPorRangoFechaAsync(
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken = default);
}