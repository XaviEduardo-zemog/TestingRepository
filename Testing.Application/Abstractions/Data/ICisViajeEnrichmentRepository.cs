using Testing.Application.GetAllViajes;

namespace Testing.Application.Abstractions.Data;

/// <summary>
/// Abstracción de Application para el enriquecimiento de viajes con CIS_DB -- mismo patrón que
/// IApplicationDbContext (interfaz aquí, implementación en Infrastructure). GetViajesQueryHandler
/// depende de esta interfaz, NUNCA de CisContext directamente (Prompt 2, sección 1).
/// </summary>
public interface ICisViajeEnrichmentRepository
{
    /// <summary>
    /// Busca en UNA sola consulta en lote (nunca una por viaje) los datos de CIS_DB para todos
    /// los Folios recibidos. folios ya debe venir sin duplicados y sin valores null/vacíos --
    /// filtrar eso es responsabilidad del llamador (GetViajesQueryHandler), porque solo él sabe
    /// qué filas del SP tienen no_remision null (viajes cancelados, no aplica buscarlos).
    /// </summary>
    Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> folios, CancellationToken cancellationToken = default);
}