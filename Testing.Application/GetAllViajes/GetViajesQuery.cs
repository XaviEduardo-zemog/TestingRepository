using MediatR;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed record GetViajesQuery(
    DateTime FechaInicio,
    DateTime FechaFin,
    string TipoFecha,
    string? Areas,
    string? IdUnidad,
    string? Estados,
    string? IdRuta,
    string? IdOperador,
    string? NoRemision) : IRequest<Result<IReadOnlyList<ViajesDto>>>;