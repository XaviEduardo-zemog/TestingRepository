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

        var folios = filasSp
            .Select(v => v.no_remision)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f!)
            .Distinct()
            .ToList();

        var enriquecimiento = folios.Count == 0 ? CisEnrichmentBatchResult.Vacio : await cisRepository.ObtenerPorFoliosAsync(folios, cancellationToken);

        var viajes = filasSp
            .Select(sp => Enriquecer(sp, enriquecimiento))
            .ToList();

        return Result.Success<IReadOnlyList<ViajesDto>>(viajes);
    }

    private static ViajesDto Enriquecer(SpViajesDto sp, CisEnrichmentBatchResult enriquecimiento)
    {
        if (string.IsNullOrWhiteSpace(sp.no_remision))
            return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.NoAplica);

        if (enriquecimiento.FoliosDuplicados.Contains(sp.no_remision))
            return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.Duplicado);

        return enriquecimiento.PorFolio.TryGetValue(sp.no_remision, out var cis)
            ? ViajesEnriquecidoMapper.Enriquecer(sp, cis, EstadoEnriquecimientoCis.Encontrado)
            : ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.NoEncontrado);
    }

    private static object? ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}