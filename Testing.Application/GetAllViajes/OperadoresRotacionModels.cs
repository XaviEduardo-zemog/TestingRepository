namespace Testing.Application.GetAllViajes;

public sealed record OperadorFilaDto(string Operador, decimal Viajes, decimal Kms, decimal Venta)
{
    public decimal KmPorViaje => Viajes > 0 ? Kms / Viajes : 0;
    public decimal VentaPorKm => Kms > 0 ? Venta / Kms : 0;
}

public sealed record OperadoresResumenDto(
    IReadOnlyList<string> Sucursales,
    IReadOnlyList<MesCerrado> MesesDisponibles,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<(int Anio, int Mes), TotalesPeriodo>>> PorSucursalOperadorMes);

public sealed record RotacionSucursalDto(
    string Sucursal,
    int Activos,
    int Altas,
    int Bajas,
    decimal ViajesAnterior,
    decimal ViajesActual,
    decimal? DeltaViajesPorcentaje,
    string Lectura,
    decimal VentaBajas);

public sealed record RotacionOperadoresDto(
    IReadOnlyList<RotacionSucursalDto> PorSucursal,
    RotacionSucursalDto Total);