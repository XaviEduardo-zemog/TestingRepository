using Testing.Application.GetAllViajes;

namespace Testing.Application.Abstractions.Data;

/// <summary>
/// Puerto de Application hacia CIS_DB
/// </summary>
public interface ICisViajeEnrichmentRepository
{
    /// <summary>
    /// Resuelve TODOS los candidatos de Folio en UNA sola consulta en lote
    /// </summary>
    Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> candidatosFolio, CancellationToken cancellationToken = default);
}