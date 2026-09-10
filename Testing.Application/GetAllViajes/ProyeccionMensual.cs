namespace Testing.Application.GetAllViajes;

public sealed record CorteMensual(int Anio, int Mes, int DiaCorte, int DiasEnMes)
{
    /// <summary>DiasEnMes / DiaCorte — mismo cálculo que factorMes en viajes_v14.html.</summary>
    public decimal Factor => DiaCorte > 0 ? (decimal)DiasEnMes / DiaCorte : 1m;

    public decimal FactorPara(int anio, int mes) => anio == Anio && mes == Mes ? Factor : 1m;

    /// <summary>
    /// Factor que le corresponde a un viaje según SU PROPIA fecha de negocio — no el factor
    /// "del grupo" al que pertenezca. Necesario porque un grupo (ej. "Agrupar por" Semana)
    /// puede mezclar viajes de más de un mes; cada viaje se proyecta con el factor de su
    /// propio mes, igual que construirDatos() en viajes_v14.html aplica el factor fila por
    /// fila al cargar, antes de cualquier agrupación.
    /// </summary>
    public decimal FactorPara(ViajesDto viaje)
    {
        var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(viaje);
        return fecha is null ? 1m : FactorPara(fecha.Value.Year, fecha.Value.Month);
    }

    public static CorteMensual? Calcular(IEnumerable<ViajesDto> viajes)
    {
        DateTime? corte = null;

        foreach (var viaje in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(viaje);
            if (fecha is not null && (corte is null || fecha > corte))
                corte = fecha;
        }

        if (corte is null)
            return null;

        return new CorteMensual(corte.Value.Year, corte.Value.Month, corte.Value.Day, DateTime.DaysInMonth(corte.Value.Year, corte.Value.Month));
    }
}

public static class ContribucionViajeProyectada
{
    public static decimal Viajes(ViajesDto viaje, CorteMensual? corte) =>
        (CamposDerivadosViajes.ObtenerMovimiento(viaje) == "Ida" ? 1m : 0m) * (corte?.FactorPara(viaje) ?? 1m);

    public static decimal Kms(ViajesDto viaje, CorteMensual? corte) =>
        (viaje.kms_viaje ?? 0) * (corte?.FactorPara(viaje) ?? 1m);

    public static decimal Peaje(ViajesDto viaje, CorteMensual? corte) =>
        ((viaje.peaje_efectivo ?? 0) + (viaje.peaje_electronico ?? 0)) * (corte?.FactorPara(viaje) ?? 1m);

    public static decimal Venta(ViajesDto viaje, CorteMensual? corte)
    {
        var venta = viaje.cis_total_venta ?? 0m;
        if (venta == 1.00m)
            venta = 0m;

        return venta * (corte?.FactorPara(viaje) ?? 1m);
    }
}