using MediatR;
using Testing.Application.Abstractions.Data;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryHandler(IApplicationDbContext dbContext, ICisViajeEnrichmentRepository cisRepository)
    : IRequestHandler<GetViajesQuery, Result<IReadOnlyList<ViajesDto>>>
{
    private const string FormatoFecha = "yyyy-MM-dd";

    private const string SpConsultaViajes =
        "EXEC [Operaciones].[sp_ConsultaViajesZemog] " +
        "@fecha_inicio, @fecha_fin, @tipo_fecha, @areas, @id_unidad, " +
        "@estados, @id_ruta, @id_operador, @no_remision";

    public async Task<Result<IReadOnlyList<ViajesDto>>> Handle(GetViajesQuery request, CancellationToken cancellationToken)
    {
        QueryParameter[] parametros =
        [
            new("@fecha_inicio", request.FechaInicio.ToString(FormatoFecha)),
            new("@fecha_fin", request.FechaFin.ToString(FormatoFecha)),
            new("@tipo_fecha", ToDbValue(request.TipoFecha)),
            new("@areas", ToDbValue(request.Areas)),
            new("@id_unidad", ToDbValue(request.IdUnidad)),
            new("@estados", ToDbValue(request.Estados)),
            new("@id_ruta", ToDbValue(request.IdRuta)),
            new("@id_operador", ToDbValue(request.IdOperador)),
            new("@no_remision", ToDbValue(request.NoRemision)),
        ];

        var filasSp = await dbContext.QueryAsync<SpViajesDto>(SpConsultaViajes, parametros, cancellationToken);

        var filasSpActivas = filasSp.Where(sp => !EsCancelado(sp)).ToList();

        var folios = filasSpActivas
            .Select(sp => LimpiarFolio(sp.no_remision!)) // no_remision garantizado no vacío: EsCancelado ya excluyó los null/blancos arriba.
            .Distinct()
            .ToList();

        var enriquecimiento = folios.Count == 0 ? CisEnrichmentBatchResult.Vacio : await cisRepository.ObtenerPorFoliosAsync(folios, cancellationToken);

        var viajes = filasSpActivas
            .Select(sp => Enriquecer(sp, enriquecimiento))
            .Where(v => v.cis_estado == EstadoEnriquecimientoCis.Encontrado)
            .ToList();

        return Result.Success<IReadOnlyList<ViajesDto>>(viajes);
    }

    // Trim + comparación insensible a mayúsculas: detecta "Cancelado", "CANCELADO", " cancelado ",
    // "Cancelada", etc. no_remision NULL/vacío también cuenta como cancelado por sí solo.
    private static bool EsCancelado(SpViajesDto sp) =>
        string.IsNullOrWhiteSpace(sp.no_remision) || EsEstatusCancelado(sp.estatus_viaje);

    private static bool EsEstatusCancelado(string? estatusViaje)
    {
        if (string.IsNullOrWhiteSpace(estatusViaje))
            return false;

        var valor = estatusViaje.Trim();
        return valor.Equals("Cancelado", StringComparison.OrdinalIgnoreCase)
            || valor.Equals("Cancelada", StringComparison.OrdinalIgnoreCase);
    }

    private static ViajesDto Enriquecer(SpViajesDto sp, CisEnrichmentBatchResult enriquecimiento)
    {
        // En la práctica, ninguna fila con no_remision vacío llega aquí -- EsCancelado ya las
        // excluyó en Handle(). Se conserva esta rama como defensa.
        if (string.IsNullOrWhiteSpace(sp.no_remision))
            return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.NoAplica);

        var folio = LimpiarFolio(sp.no_remision);

        if (enriquecimiento.FoliosDuplicados.Contains(folio))
            return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.Duplicado);

        return enriquecimiento.PorFolio.TryGetValue(folio, out var cis)
            ? ViajesEnriquecidoMapper.Enriquecer(sp, cis, EstadoEnriquecimientoCis.Encontrado)
            : ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.NoEncontrado);
    }

    private static string LimpiarFolio(string noRemision)
    {
        var valor = noRemision.Trim();

        var indiceComa = valor.IndexOf(',');
        if (indiceComa >= 0)
            valor = valor[..indiceComa];

        var indiceGuion = valor.IndexOf('-');
        if (indiceGuion >= 0)
            valor = valor[..indiceGuion];

        return valor.Trim();
    }

    private static object? ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}