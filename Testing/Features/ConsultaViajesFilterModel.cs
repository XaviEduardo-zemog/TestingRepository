using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Features;

public sealed class ConsultaViajesFilterModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime FechaFin { get; set; } = DateTime.Today;
    public string TipoFecha { get; set; } = "";
    public string? Areas { get; set; }
    public string? IdUnidad { get; set; }
    public string? Estados { get; set; }
    public string? IdRuta { get; set; }
    public string? IdOperador { get; set; }
    public string? NoRemision { get; set; }
}
