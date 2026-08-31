namespace Testing.Features;

public sealed record MesColumna(string Clave, string Etiqueta);

public sealed record MetricasMes(int Viajes, decimal Km, decimal Peaje)
{
    public decimal KmPorViaje => Viajes > 0 ? Km / Viajes : 0;

    public static readonly MetricasMes Vacio = new(0, 0, 0);

    public static MetricasMes Sumar(MetricasMes a, MetricasMes b) =>
        new(a.Viajes + b.Viajes, a.Km + b.Km, a.Peaje + b.Peaje);
}

public sealed class FilaPivote
{
    public required string Dimension { get; init; }
    public Dictionary<string, MetricasMes> PorMes { get; } = [];
    public MetricasMes Total { get; set; } = MetricasMes.Vacio;

    public MetricasMes ObtenerMes(string claveMes) => PorMes.GetValueOrDefault(claveMes, MetricasMes.Vacio);
}