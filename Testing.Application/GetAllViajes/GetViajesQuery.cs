using MediatR;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed record GetViajesQuery(DateTime FechaInicio, DateTime FechaFin) : IRequest<Result<IReadOnlyList<ViajesDto>>>;