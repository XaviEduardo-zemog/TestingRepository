using Microsoft.EntityFrameworkCore;
using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;
using Testing.Infrastructure.Persistence.CIS_DB;
using Testing.Infrastructure.Persistence.Zam;

namespace Testing.Infrastructure.Persistence;

/// <summary>
/// Única implementación real de <see cref="ISegundaFuenteEnrichmentRepository"/>. 3 consultas en
/// lote independientes (nunca una por fila), reutilizando los DbContext ya registrados -- no
/// requiere ninguna cadena de conexión nueva:
/// - Sucursales por _base y RutasZam por código: CIS_DB, vía IDbContextFactory&lt;CisContext&gt;
///   (el mismo que ya usa CisViajeEnrichmentRepository).
/// - trafico_guia por Factura: ZemogDB, vía IDbContextFactory&lt;ZemogContext&gt; (el mismo que
///   ya usa ApplicationDbContext para el SP).
/// Todas usan AsNoTracking (solo lectura).
/// </summary>
internal sealed class SegundaFuenteEnrichmentRepository(
    IDbContextFactory<CisContext> cisContextFactory,
    IDbContextFactory<ZemogContext> zemogContextFactory) : ISegundaFuenteEnrichmentRepository
{
    public async Task<SegundaFuenteBatchResult> ObtenerAsync(
        IReadOnlyCollection<string> basesDistintas,
        IReadOnlyCollection<string> codigosRutaDistintos,
        IReadOnlyCollection<string> facturasDistintas,
        CancellationToken cancellationToken = default)
    {
        var porBase = basesDistintas.Count == 0
            ? new Dictionary<string, DatosSucursalFallback>()
            : await ObtenerSucursalesPorBaseAsync(basesDistintas, cancellationToken);

        var (porCodigoRuta, codigosAmbiguos) = codigosRutaDistintos.Count == 0
            ? (new Dictionary<string, DatosRutaFallback>(), new HashSet<string>())
            : await ObtenerRutasPorCodigoAsync(codigosRutaDistintos, cancellationToken);

        var porFactura = facturasDistintas.Count == 0
            ? new Dictionary<string, DatosTraficoGuiaFallback>()
            : await ObtenerGuiasPorFacturaAsync(facturasDistintas, cancellationToken);

        return new SegundaFuenteBatchResult(porBase, porCodigoRuta, codigosAmbiguos, porFactura);
    }

    // a) Sucursales por _base = Sucursales.Nomenclatura -- 1 consulta en lote, CIS_DB.
    // Nomenclatura confirmada única en vivo antes de implementar esto (0 filas repetidas) --
    // ToDictionaryAsync es seguro, no requiere manejo de ambigüedad como RutasZam.
    private async Task<Dictionary<string, DatosSucursalFallback>> ObtenerSucursalesPorBaseAsync(
        IReadOnlyCollection<string> basesDistintas, CancellationToken cancellationToken)
    {
        await using var context = await cisContextFactory.CreateDbContextAsync(cancellationToken);

        var sucursales = await context.Sucursales.AsNoTracking()
            .Where(s => basesDistintas.Contains(s.Nomenclatura))
            .Select(s => new { s.Nomenclatura, s.NombreCorto, s.Region, s.IdSucursal, s.Nombre })
            .ToListAsync(cancellationToken);

        return sucursales.ToDictionary(
            s => s.Nomenclatura,
            s => new DatosSucursalFallback(s.NombreCorto, s.Region, s.Nomenclatura, s.IdSucursal, s.Nombre));
    }

    // b) RutasZam por código de ruta -- 1 consulta en lote, CIS_DB. Codigo NO es único: se agrupa
    // y todo código con 2+ filas se reporta como ambiguo y se excluye de PorCodigoRuta -- nunca
    // se resuelve con First()/Single().
    private async Task<(Dictionary<string, DatosRutaFallback> PorCodigo, HashSet<string> Ambiguos)> ObtenerRutasPorCodigoAsync(
        IReadOnlyCollection<string> codigosDistintos, CancellationToken cancellationToken)
    {
        await using var context = await cisContextFactory.CreateDbContextAsync(cancellationToken);

        var rutas = await context.RutasZams.AsNoTracking()
            .Where(r => codigosDistintos.Contains(r.Codigo))
            .ToListAsync(cancellationToken);

        var porCodigo = new Dictionary<string, DatosRutaFallback>();
        var ambiguos = new HashSet<string>();

        foreach (var grupo in rutas.GroupBy(r => r.Codigo))
        {
            if (grupo.Count() > 1)
            {
                ambiguos.Add(grupo.Key);
                continue;
            }

            var r = grupo.Single();
            porCodigo[grupo.Key] = new DatosRutaFallback(r.Codigo, r.Ruta, r.Origen, r.Destino, r.Kms);
        }

        return (porCodigo, ambiguos);
    }

    // c) trafico_guia por Factura = TraficoGuium.NumGuia -- 1 consulta en lote, ZemogDB.
    // NumGuia tiene índice único real (confirmado en el análisis previo) -- filtrar solo por
    // NumGuia ya garantiza como máximo 1 fila. El cruce con no_viaje (llave lógica completa:
    // Factura + NoViaje) se valida en el handler, no aquí -- este método solo expone "qué hay en
    // trafico_guia para esta Factura".
    private async Task<Dictionary<string, DatosTraficoGuiaFallback>> ObtenerGuiasPorFacturaAsync(
        IReadOnlyCollection<string> facturasDistintas, CancellationToken cancellationToken)
    {
        await using var context = await zemogContextFactory.CreateDbContextAsync(cancellationToken);

        var guias = await context.TraficoGuia.AsNoTracking()
            .Where(t => facturasDistintas.Contains(t.NumGuia))
            .Select(t => new
            {
                t.NumGuia,
                t.NoViaje,
                t.Subtotal,
                t.KmsGuia,
                t.NoRemision,
                t.FechaGuia,
                t.StatusGuia,
                t.IdArea,
                t.NoGuia,
            })
            .ToListAsync(cancellationToken);

        // NumGuia es único por índice -- ToDictionary no debería fallar nunca; si lo hiciera,
        // sería una violación real de la restricción de unicidad de la base y debe propagarse
        // como error, no ocultarse.
        return guias.ToDictionary(
            g => g.NumGuia,
            g => new DatosTraficoGuiaFallback(g.NumGuia, g.NoViaje, g.Subtotal, g.KmsGuia, g.NoRemision, g.FechaGuia, g.StatusGuia, g.IdArea, g.NoGuia));
    }
}