namespace Testing.Application.GetAllViajes;

/// <summary>
/// Fase 9 — Operadores (Bloque 8.9) y Rotación (Bloque 8.10), extraídos de
/// ResumenEjecutivoCalculator a su propio archivo: son la parte del Resumen Ejecutivo de mayor
/// valor operativo hoy (no dependen de Venta) y esta fase las trata como una unidad propia,
/// testeable por separado. ResumenEjecutivoCalculator.Calcular() delega aquí — ver el Artifact
/// sobre por qué NO se creó una query MediatR nueva: no consulta la base de datos, transforma
/// viajes ya cargados, igual que el resto del Resumen Ejecutivo desde la Fase 8.
///
/// Reglas exactas (dadas por el usuario en la Fase 9):
///   Activo:  último mes de actividad == último mes del periodo.
///   Alta:    primer mes de actividad &gt; primer mes del periodo.
///   Baja:    último mes de actividad != último mes del periodo.
///   Lectura: "Sin bajas" si Bajas==0; si no, "Volumen sostenido" si Δ%Viajes &gt;= -5,
///            si no "⚠ Volumen a la baja".
/// Los ratios del TOTAL (KM/Viaje, y $/KM el día que exista Venta) se calculan como
/// KmTotal/ViajesTotal — NUNCA como promedio de los ratios individuales de cada operador.
/// </summary>
public static class OperadoresRotacionCalculator
{
    // ---------- Bloque 8.9 — Operadores (replica RE_prepOperadores) ----------

    public static OperadoresResumenDto CalcularOperadores(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados)
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
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            var contribucion = new TotalesPeriodo(esIda ? 1 : 0, v.kms_viaje ?? 0);

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

    // ---------- Bloque 8.10 — Rotación de operadores (replica RE_seccionRotacion) ----------
    // Requiere al menos 2 meses cerrados (mismo guard que el resto de comparativos del Resumen
    // Ejecutivo) — el llamador (ResumenEjecutivoCalculator) es responsable de no invocar este
    // método con menos de 2 meses.

    public static RotacionOperadoresDto CalcularRotacion(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = mesesCerrados[^1];
        var anterior = mesesCerrados[^2];
        var primerMes = mesesCerrados[0];

        var minPorOperador = new Dictionary<string, MesCerrado>();
        var maxPorOperador = new Dictionary<string, MesCerrado>();
        var sucursalPorOperador = new Dictionary<string, string>();

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
                sucursalPorOperador[operador] = sucursal; // sucursal de la aparición más reciente del operador
            }
        }

        var acumuladoPorSucursal = new Dictionary<string, (int Activos, int Altas, int Bajas)>();
        int nActTotal = 0, nAltasTotal = 0, nBajasTotal = 0;

        foreach (var operador in maxPorOperador.Keys)
        {
            var sucursal = sucursalPorOperador[operador];
            var max = maxPorOperador[operador];
            var min = minPorOperador[operador];

            // Nombrar también el valor por defecto es obligatorio: si se deja (0, 0, 0) sin
            // nombres, la inferencia de tipos de GetValueOrDefault combina ese literal (sin
            // nombre) con el tipo del diccionario (con nombre) y el compilador DESCARTA los
            // nombres del resultado — acc quedaría tipado como (int, int, int), sin .Activos/
            // .Altas/.Bajas (error real reportado tras la Fase 8, corregido aquí desde el origen).
            var acc = acumuladoPorSucursal.GetValueOrDefault(sucursal, (Activos: 0, Altas: 0, Bajas: 0));

            // Activo: último mes de actividad == último mes del periodo.
            var esActivo = max.Anio == ultimo.Anio && max.Mes == ultimo.Mes;
            // Alta: primer mes de actividad > primer mes del periodo.
            var esAlta = EsAnterior(primerMes, min); // min > primerMes
            // Baja: último mes de actividad != último mes del periodo.
            var esBaja = !esActivo;

            // Activo y Alta NO son mutuamente excluyentes — un operador nuevo que ya trabajó
            // en el último mes cuenta en ambas columnas, igual que el HTML.
            if (esActivo) { acc.Activos++; nActTotal++; }
            if (esAlta) { acc.Altas++; nAltasTotal++; }
            if (esBaja) { acc.Bajas++; nBajasTotal++; }

            acumuladoPorSucursal[sucursal] = acc;
        }

        var viajesPorSucursalMes = new Dictionary<(string Sucursal, MesCerrado Mes), int>();
        foreach (var v in viajes)
        {
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            if (!esIda)
                continue;

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
            viajesPorSucursalMes[(sucursal, mes)] = viajesPorSucursalMes.GetValueOrDefault((sucursal, mes)) + 1;
        }

        var filas = new List<RotacionSucursalDto>();
        int totalA = 0, totalB = 0;

        foreach (var (sucursal, (activos, altas, bajas)) in acumuladoPorSucursal.OrderBy(kv => kv.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var a = viajesPorSucursalMes.GetValueOrDefault((sucursal, anterior));
            var b = viajesPorSucursalMes.GetValueOrDefault((sucursal, ultimo));
            totalA += a;
            totalB += b;

            filas.Add(new RotacionSucursalDto(sucursal, activos, altas, bajas, CalcularDeltaViajes(a, b), CalcularLectura(bajas, a, b)));
        }

        // El Δ% y la Lectura del TOTAL se recalculan sobre ΣViajes anterior/ΣViajes último de
        // TODAS las sucursales — nunca promediando el Δ% de cada sucursal individual.
        var total = new RotacionSucursalDto("TOTAL", nActTotal, nAltasTotal, nBajasTotal, CalcularDeltaViajes(totalA, totalB), CalcularLectura(nBajasTotal, totalA, totalB));

        return new RotacionOperadoresDto(filas, total);
    }

    // Delta expuesto a la UI: null si no hay base (más honesto que el 0% que devuelve el HTML
    // cuando v.a===0). CalcularLectura, en cambio, replica el fallback a 0 EXACTO del HTML para
    // la regla de "Lectura" (dv=v.a?...: 0), a propósito — dos tratamientos distintos del mismo
    // caso borde, cada uno justificado por separado.
    private static decimal? CalcularDeltaViajes(int a, int b) => a > 0 ? (decimal)(b - a) / a * 100 : null;

    private static string CalcularLectura(int bajas, int a, int b)
    {
        if (bajas == 0)
            return "Sin bajas";

        var dv = a > 0 ? (decimal)(b - a) / a * 100 : 0m;
        return dv >= -5 ? "Volumen sostenido" : "⚠ Volumen a la baja";
    }

    private static bool EsAnterior(MesCerrado a, MesCerrado b) => a.Anio != b.Anio ? a.Anio < b.Anio : a.Mes < b.Mes;
}