using Microsoft.EntityFrameworkCore;
using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Infrastructure.Persistence.CIS_DB;

/// <summary>
/// Única implementación real de <see cref="ICisViajeEnrichmentRepository"/>: 1 consulta en lote
/// contra CIS_DB para TODOS los candidatos de Folio a la vez (nunca una por fila -- Prompt 2,
/// punto 7). Usa AsNoTracking (solo lectura, nunca se van a guardar cambios en CIS_DB desde
/// aquí) y un LEFT JOIN explícito a Sucursales (no la navigation property) para que una fila de
/// ZemogViajesEnZamAnual sin Sucursal real NUNCA se pierda silenciosamente -- solo trae
/// NombreCorto/Region/Nomenclatura en null.
/// </summary>
internal sealed class CisViajeEnrichmentRepository(IDbContextFactory<CisContext> contextFactory) : ICisViajeEnrichmentRepository
{
    public async Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> candidatosFolio, CancellationToken cancellationToken = default)
    {
        if (candidatosFolio.Count == 0)
            return CisEnrichmentBatchResult.Vacio;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var filas = await (
            from v in context.ZemogViajesEnZamAnuals.AsNoTracking()
            where candidatosFolio.Contains(v.Folio)
            join s in context.Sucursales.AsNoTracking()
                on v.IdSucursal equals s.IdSucursal into sucursalesCoincidentes
            from sucursal in sucursalesCoincidentes.DefaultIfEmpty()
            select new
            {
                v.Folio,
                v.FolioComplemento,
                v.IdSucursal,
                v.Sucursal,
                v.Origen,
                v.Destino,
                v.EstadoOrigen,
                v.EstadoDestino,
                v.TotalVenta,
                v.EjesEquipos,
                v.FechaCalendario,
                v.Trayecto,
                v.Operacion,
                NombreCorto = sucursal != null ? sucursal.NombreCorto : null,
                Region = sucursal != null ? sucursal.Region : null,
                Nomenclatura = sucursal != null ? sucursal.Nomenclatura : null,
            }
        ).ToListAsync(cancellationToken);

        var porFolio = new Dictionary<string, DatosCisViaje>();
        var duplicados = new HashSet<string>();

        foreach (var grupo in filas.GroupBy(f => f.Folio))
        {
            if (grupo.Count() > 1)
            {
                duplicados.Add(grupo.Key);
                continue;
            }

            var fila = grupo.Single();
            porFolio[grupo.Key] = new DatosCisViaje(
                fila.Folio,
                fila.FolioComplemento,
                fila.IdSucursal,
                fila.Sucursal,
                fila.NombreCorto,
                fila.Region,
                fila.Nomenclatura,
                fila.Origen,
                fila.Destino,
                fila.EstadoOrigen,
                fila.EstadoDestino,
                fila.TotalVenta,
                fila.EjesEquipos,
                fila.FechaCalendario,
                fila.Trayecto,
                fila.Operacion);
        }

        return new CisEnrichmentBatchResult(porFolio, duplicados);
    }
}