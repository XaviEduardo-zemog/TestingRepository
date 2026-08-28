using MediatR;
using Testing.Application.Abstractions.Data;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryHandler(IApplicationDbContext dbContext)
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

        var viajes = await dbContext.QueryAsync<ViajesDto>(SpConsultaViajes, parametros, cancellationToken);

        return Result.Success<IReadOnlyList<ViajesDto>>(viajes);
    }

    private static object? ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
