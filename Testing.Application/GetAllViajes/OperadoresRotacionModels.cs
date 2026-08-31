namespace Testing.Application.GetAllViajes;

/// <summary>Bloque 8.9 — una fila de la tabla de operadores para la sucursal/meses seleccionados.</summary>
public sealed record OperadorFilaDto(string Operador, int Viajes, decimal Kms)
{
    public decimal KmPorViaje => Viajes > 0 ? Kms / Viajes : 0;
}

/// <summary>
/// Bloque 8.9 — preagregado Sucursal → Operador → (Año,Mes) → Totales, construido UNA vez
/// (replica RE_prepOperadores). La UI (ResumenOperadores.razor) recombina sumando los meses que
/// el usuario seleccione, sin volver a recorrer los viajes — igual que el HTML (§54.13).
/// </summary>
public sealed record OperadoresResumenDto(
    IReadOnlyList<string> Sucursales,
    IReadOnlyList<MesCerrado> MesesDisponibles,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<(int Anio, int Mes), TotalesPeriodo>>> PorSucursalOperadorMes);

/// <summary>Bloque 8.10 — una sucursal y su resumen de rotación. VtaBajas del HTML no se modela (Pendiente, requiere Venta).</summary>
public sealed record RotacionSucursalDto(
    string Sucursal,
    int Activos,
    int Altas,
    int Bajas,
    decimal? DeltaViajesPorcentaje,
    string Lectura);

public sealed record RotacionOperadoresDto(
    IReadOnlyList<RotacionSucursalDto> PorSucursal,
    RotacionSucursalDto Total);