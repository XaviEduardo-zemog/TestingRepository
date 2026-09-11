using Testing.Application.GetAllViajes;

namespace Testing.Application.Abstractions.Data;

/// <summary>
/// Puerto de Application hacia la 2ª fuente de enriquecimiento (fallback): Sucursales/RutasZam
/// (CIS_DB) + trafico_guia (ZemogDB). Se usa ÚNICAMENTE para filas que ya fallaron en el
/// enriquecimiento CIS_DB por Folio (NoEncontrado/Duplicado) -- nunca compite con
/// <see cref="ICisViajeEnrichmentRepository"/>, nunca se consulta para filas que CIS_DB ya
/// resolvió.
/// </summary>
public interface ISegundaFuenteEnrichmentRepository
{
    /// <summary>
    /// Resuelve, en 3 consultas en lote independientes (nunca una por fila), Sucursales por
    /// _base, RutasZam por código de ruta, y trafico_guia por Factura, para los conjuntos de
    /// valores distintos recibidos.
    /// </summary>
    Task<SegundaFuenteBatchResult> ObtenerAsync(
        IReadOnlyCollection<string> basesDistintas,
        IReadOnlyCollection<string> codigosRutaDistintos,
        IReadOnlyCollection<string> facturasDistintas,
        CancellationToken cancellationToken = default);
}