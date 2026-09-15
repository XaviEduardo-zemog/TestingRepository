namespace Testing.Features;

public sealed class ConsultaViajesFilterModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime FechaFin { get; set; } = DateTime.Today;
}