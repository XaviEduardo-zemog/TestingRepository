using MediatR;
using Testing.Application.Abstractions.Data;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryHandler(ICisViajesDirectosRepository cisDirectoRepository)
    : IRequestHandler<GetViajesQuery, Result<IReadOnlyList<ViajesDto>>>
{
    public async Task<Result<IReadOnlyList<ViajesDto>>> Handle(GetViajesQuery request, CancellationToken cancellationToken)
    {
        var fechaInicio = DateOnly.FromDateTime(request.FechaInicio);
        var fechaFin = DateOnly.FromDateTime(request.FechaFin);

        var filasCis = await cisDirectoRepository.ObtenerPorRangoFechaAsync(fechaInicio, fechaFin, cancellationToken);

        var viajes = filasCis.Select(ViajesDirectosMapper.Mapear).ToList();

        CamposDerivadosViajes.AplicarViajeRedondo(viajes);

        return Result.Success<IReadOnlyList<ViajesDto>>(viajes);
    }
}