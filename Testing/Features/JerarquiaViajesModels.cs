namespace Testing.Features;

/// <summary>
/// Nodo del árbol Cliente › Zona › Matriz › Sucursal. PorMes/Total 
/// </summary>
public sealed class NodoJerarquia
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required int Nivel { get; init; }
    public Dictionary<string, NodoJerarquia> Hijos { get; } = [];
    public Dictionary<string, MetricasMes> PorMes { get; } = [];
    public MetricasMes Total { get; set; } = MetricasMes.Vacio;
    public bool IsExpanded { get; set; }

    public MetricasMes ObtenerMes(string claveMes) => PorMes.GetValueOrDefault(claveMes, MetricasMes.Vacio);
}

public sealed record FilaArbol(int Nivel, string Id, string Label, NodoJerarquia Nodo, bool EsHoja);