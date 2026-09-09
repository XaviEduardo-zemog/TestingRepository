using Microsoft.EntityFrameworkCore;
using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Infrastructure.Persistence.CIS_DB;

internal sealed class CisViajeEnrichmentRepository(IDbContextFactory<CisContext> contextFactory) : ICisViajeEnrichmentRepository
{
    public async Task<CisEnrichmentBatchResult> ObtenerPorFoliosAsync(IReadOnlyCollection<string> folios, CancellationToken cancellationToken = default)
    {
        if (folios.Count == 0)
            return CisEnrichmentBatchResult.Vacio;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var filas = await (
            from v in context.ZemogViajesEnZamAnuals.AsNoTracking()
            where folios.Contains(v.Folio)
            join s in context.Sucursales.AsNoTracking()
                on v.IdSucursal equals s.IdSucursal into sucursalesCoincidentes
            from sucursal in sucursalesCoincidentes.DefaultIfEmpty()
            select new
            {
                v.Folio,
                v.IdSucursal,
                v.Sucursal,
                v.Ruta,
                v.Origen,
                v.Destino,
                v.EstadoOrigen,
                v.EstadoDestino,
                v.TotalVenta,
                v.EjesEquipos,
                v.FechaCalendario,
                v.Trayecto,
                Cliente = sucursal != null ? sucursal.NombreCorto : null,
                Zona = sucursal != null ? sucursal.Region : null,
                Matriz = sucursal != null ? sucursal.Nomenclatura : null,
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
                fila.Cliente,
                fila.Zona,
                fila.Matriz,
                fila.Sucursal,
                fila.IdSucursal,
                fila.Ruta,
                fila.Origen,
                fila.Destino,
                fila.EstadoOrigen,
                fila.EstadoDestino,
                fila.TotalVenta,
                fila.EjesEquipos,
                fila.FechaCalendario,
                fila.Trayecto);
        }

        var foliosSinCoincidencia = folios
            .Except(filas.Select(f => f.Folio))
            .ToList();

        return new CisEnrichmentBatchResult(porFolio, foliosSinCoincidencia, duplicados);
    }
}