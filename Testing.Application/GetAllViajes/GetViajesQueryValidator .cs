using FluentValidation;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryValidator : AbstractValidator<GetViajesQuery>
{
    public GetViajesQueryValidator()
    {
        RuleFor(x => x.FechaInicio)
            .LessThanOrEqualTo(x => x.FechaFin)
            .WithMessage("La fecha de inicio no puede ser posterior a la fecha fin.");
    }
}
