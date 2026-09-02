namespace Testing.Application.GetAllViajes;

public static class OperadoresRotacionCalculator
{
    public static OperadoresResumenDto CalcularOperadores(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados, CorteMensual? corte)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var porSucursal = new Dictionary<string, Dictionary<string, Dictionary<(int, int), TotalesPeriodo>>>();

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.ContainsKey((fecha.Value.Year, fecha.Value.Month)))
                continue;

            var sucursal = v._base ?? "(sin dato)";
            var operador = v.operador1 ?? "(sin dato)";
            var claveMes = (fecha.Value.Year, fecha.Value.Month);
            var contribucion = TotalesPeriodo.De(v, corte);

            if (!porSucursal.TryGetValue(sucursal, out var porOperador))
                porSucursal[sucursal] = porOperador = [];
            if (!porOperador.TryGetValue(operador, out var porMes))
                porOperador[operador] = porMes = [];

            porMes[claveMes] = TotalesPeriodo.Sumar(porMes.GetValueOrDefault(claveMes, TotalesPeriodo.Vacio), contribucion);
        }

        return new OperadoresResumenDto(
            porSucursal.Keys.OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToList(),
            mesesCerrados,
            porSucursal.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyDictionary<string, IReadOnlyDictionary<(int, int), TotalesPeriodo>>)kv.Value.ToDictionary(
                    kv2 => kv2.Key,
                    kv2 => (IReadOnlyDictionary<(int, int), TotalesPeriodo>)kv2.Value)));
    }

    public static RotacionOperadoresDto CalcularRotacion(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados, CorteMensual? corte)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = mesesCerrados[^1];
        var anterior = mesesCerrados[^2];
        var primerMes = mesesCerrados[0];

        var minPorOperador = new Dictionary<string, MesCerrado>();
        var maxPorOperador = new Dictionary<string, MesCerrado>();
        var sucursalPorOperador = new Dictionary<string, string>();
        var ventaPorOperador = new Dictionary<string, decimal>();

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = mesesCerrados.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var operador = v.operador1 ?? "(sin dato)";
            var sucursal = v._base ?? "(sin dato)";

            if (!minPorOperador.TryGetValue(operador, out var min) || EsAnterior(mes, min))
                minPorOperador[operador] = mes;

            if (!maxPorOperador.TryGetValue(operador, out var max) || EsAnterior(max, mes))
            {
                maxPorOperador[operador] = mes;
                sucursalPorOperador[operador] = sucursal;
            }

            ventaPorOperador[operador] = ventaPorOperador.GetValueOrDefault(operador) + ContribucionViajeProyectada.Venta(v, corte);
        }

        var acumuladoPorSucursal = new Dictionary<string, (int Activos, int Altas, int Bajas, decimal VentaBajas)>();
        int nActTotal = 0, nAltasTotal = 0, nBajasTotal = 0;
        decimal ventaBajasTotal = 0;

        foreach (var operador in maxPorOperador.Keys)
        {
            var sucursal = sucursalPorOperador[operador];
            var max = maxPorOperador[operador];
            var min = minPorOperador[operador];

            // Nombrar también el valor por defecto es obligatorio: si se deja (0,0,0,0) sin
            // nombres, la inferencia de tipos de GetValueOrDefault descarta los nombres del
            // resultado (error real reportado en la Fase 8, corregido aquí desde el origen).
            var acc = acumuladoPorSucursal.GetValueOrDefault(sucursal, (Activos: 0, Altas: 0, Bajas: 0, VentaBajas: 0m));

            var esActivo = max.Anio == ultimo.Anio && max.Mes == ultimo.Mes;
            var esAlta = EsAnterior(primerMes, min); // min > primerMes
            var esBaja = !esActivo;

            // Activo y Alta NO son mutuamente excluyentes.
            if (esActivo) { acc.Activos++; nActTotal++; }
            if (esAlta) { acc.Altas++; nAltasTotal++; }
            if (esBaja)
            {
                acc.Bajas++;
                nBajasTotal++;
                var venta = ventaPorOperador.GetValueOrDefault(operador);
                acc.VentaBajas += venta;
                ventaBajasTotal += venta;
            }

            acumuladoPorSucursal[sucursal] = acc;
        }

        var viajesPorSucursalMes = new Dictionary<(string Sucursal, MesCerrado Mes), decimal>();
        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null)
                continue;

            MesCerrado mes;
            if (fecha.Value.Year == ultimo.Anio && fecha.Value.Month == ultimo.Mes)
                mes = ultimo;
            else if (fecha.Value.Year == anterior.Anio && fecha.Value.Month == anterior.Mes)
                mes = anterior;
            else
                continue;

            var sucursal = v._base ?? "(sin dato)";
            var clave = (sucursal, mes);
            viajesPorSucursalMes[clave] = viajesPorSucursalMes.GetValueOrDefault(clave) + ContribucionViajeProyectada.Viajes(v, corte);
        }

        var filas = new List<RotacionSucursalDto>();
        decimal totalA = 0, totalB = 0;

        foreach (var (sucursal, (activos, altas, bajas, ventaBajas)) in acumuladoPorSucursal.OrderBy(kv => kv.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var a = viajesPorSucursalMes.GetValueOrDefault((sucursal, anterior));
            var b = viajesPorSucursalMes.GetValueOrDefault((sucursal, ultimo));
            totalA += a;
            totalB += b;

            filas.Add(new RotacionSucursalDto(sucursal, activos, altas, bajas, a, b, CalcularDeltaViajes(a, b), CalcularLectura(bajas, a, b), ventaBajas));
        }

        var total = new RotacionSucursalDto("TOTAL", nActTotal, nAltasTotal, nBajasTotal, totalA, totalB, CalcularDeltaViajes(totalA, totalB), CalcularLectura(nBajasTotal, totalA, totalB), ventaBajasTotal);

        return new RotacionOperadoresDto(filas, total);
    }

    private static decimal? CalcularDeltaViajes(decimal a, decimal b) => a > 0 ? (b - a) / a * 100 : null;

    private static string CalcularLectura(int bajas, decimal a, decimal b)
    {
        if (bajas == 0)
            return "Sin bajas";

        var dv = a > 0 ? (b - a) / a * 100 : 0m;
        return dv >= -5 ? "Volumen sostenido" : "⚠ Volumen a la baja";
    }

    private static bool EsAnterior(MesCerrado a, MesCerrado b) => a.Anio != b.Anio ? a.Anio < b.Anio : a.Mes < b.Mes;
}