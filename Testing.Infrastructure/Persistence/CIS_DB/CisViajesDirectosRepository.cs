using Microsoft.EntityFrameworkCore;
using Testing.Application.Abstractions.Data;
using Testing.Application.GetAllViajes;

namespace Testing.Infrastructure.Persistence.CIS_DB;

/// <summary>
/// Única implementación real de <see cref="ICisViajesDirectosRepository"/> -- Fuente B del
/// prototipo paralelo (ver docs/PROTOTIPO_FUENTE_CIS.md). Lee ZemogViajesEnZamAnual filtrando por
/// FechaCalendario, con LEFT JOIN explícito a Sucursales (no la navigation property, mismo patrón
/// que <see cref="CisViajeEnrichmentRepository"/>) para que una fila sin Sucursal real no se
/// pierda silenciosamente. AsNoTracking porque es solo lectura. Proyección explícita de columnas
/// (no SELECT *), UNA sola consulta, sin N+1, sin joins 1:N adicionales -- no se incluye RutasZam
/// ni trafico_guia (sin evidencia funcional que lo requiera, ver
/// docs/DISENO_CONSULTA_DIRECTA_CIS.md §6/§9/§10).
/// </summary>
internal sealed class CisViajesDirectosRepository(IDbContextFactory<CisContext> contextFactory) : ICisViajesDirectosRepository
{
    public async Task<IReadOnlyList<DatosCisViajeDirecto>> ObtenerPorRangoFechaAsync(
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var filas = await (
            from v in context.ZemogViajesEnZamAnuals.AsNoTracking()
            where v.FechaCalendario >= fechaInicio && v.FechaCalendario <= fechaFin
            join s in context.Sucursales.AsNoTracking()
                on v.IdSucursal equals s.IdSucursal into sucursalesCoincidentes
            from sucursal in sucursalesCoincidentes.DefaultIfEmpty()
            select new
            {
                v.Identificador,
                v.Folio,
                v.FolioComplemento,
                v.NoViaje,
                v.NoGuia,
                v.Factura,
                v.IdArea,
                v.IdSucursal,
                v.Sucursal,
                v.Trayecto,
                v.FechaCalendario,
                v.Origen,
                v.Destino,
                v.EstadoOrigen,
                v.EstadoDestino,
                v.Ruta,
                v.CodigoRuta,
                v.Expedicion,
                v.Operador1,
                v.IdOperador1,
                v.Operador2,
                v.IdOperador2,
                v.Unidad,
                v.Remolque1,
                v.Remolque2,
                v.Dolly,
                v.Kms,
                v.TotalVenta,
                v.EjesEquipos,
                v.MontoPeajeIave,
                v.MontoPeajeEfectivo,
                v.EstatusAsignacion,
                NombreCorto = sucursal != null ? sucursal.NombreCorto : null,
                Region = sucursal != null ? sucursal.Region : null,
                Nomenclatura = sucursal != null ? sucursal.Nomenclatura : null,
            }
        ).ToListAsync(cancellationToken);

        return filas.Select(f => new DatosCisViajeDirecto(
            f.Identificador,
            f.Folio,
            f.FolioComplemento,
            f.NoViaje,
            f.NoGuia,
            f.Factura,
            f.IdArea,
            f.IdSucursal,
            f.Sucursal,
            f.NombreCorto,
            f.Region,
            f.Nomenclatura,
            f.Trayecto,
            f.FechaCalendario,
            f.Origen,
            f.Destino,
            f.EstadoOrigen,
            f.EstadoDestino,
            f.Ruta,
            f.CodigoRuta,
            f.Expedicion,
            f.Operador1,
            f.IdOperador1,
            f.Operador2,
            f.IdOperador2,
            f.Unidad,
            f.Remolque1,
            f.Remolque2,
            f.Dolly,
            f.Kms,
            f.TotalVenta,
            f.EjesEquipos,
            f.MontoPeajeIave,
            f.MontoPeajeEfectivo,
            f.EstatusAsignacion))
            .ToList();
    }
}