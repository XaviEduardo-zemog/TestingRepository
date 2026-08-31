namespace Testing.Application.GetAllViajes;

/// <summary>
/// Replica el Resumen Ejecutivo de viajes_v14.html (RE_render()).
/// </summary>
public static class ResumenEjecutivoCalculator
{
    public const string PendienteVenta = "Requiere Venta — sin fuente localizada, ver §54.85";

    private static readonly string[] NombresMes =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    private static readonly Func<ViajesDto, string?>[] NivelesArbol =
    [
        CamposDerivadosViajes.ObtenerCliente,
        CamposDerivadosViajes.ObtenerZona,
        v => v._base,
        v => v._base, // Sucursal: mismo fallback que la Fase 7 (§54.108) — sin columna confirmada en el SP.
    ];

    public static ResumenEjecutivoDto Calcular(IReadOnlyList<ViajesDto> viajesCargados, CorteMensual? corte)
    {
        var (mesesCerrados, mesAbiertoExcluido, etiquetaMesAbierto) = CalcularMesesCerrados(viajesCargados, corte);

        var viajesCerrados = viajesCargados.Where(v => EstaEnMesesCerrados(v, mesesCerrados)).ToList();
        var hayComparativos = mesesCerrados.Count >= 2;

        var nivelZemog = mesesCerrados.Count == 0 ? null : CalcularBloqueNivel("Zemog", viajesCerrados, mesesCerrados);

        var porCliente = mesesCerrados.Count == 0
            ? []
            : viajesCerrados
                .Select(CamposDerivadosViajes.ObtenerCliente)
                .Where(c => c is { Length: > 0 })
                .Distinct()
                .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .Select(cliente => new NivelPorClienteDto(
                    cliente!,
                    CalcularBloqueNivel(
                        cliente!,
                        viajesCerrados.Where(v => CamposDerivadosViajes.ObtenerCliente(v) == cliente).ToList(),
                        mesesCerrados)))
                .ToList();

        var arbol = hayComparativos ? ConstruirArbolComparativo(viajesCerrados, mesesCerrados) : null;

        var agenciasDesaparecidas = mesesCerrados.Count == 0
            ? []
            : CalcularAgenciasDesaparecidas(viajesCerrados, mesesCerrados);

        // Fase 9: Operadores/Rotación ya no viven aquí — se delega a OperadoresRotacionCalculator.
        var operadores = OperadoresRotacionCalculator.CalcularOperadores(viajesCerrados, mesesCerrados);

        var rotacion = hayComparativos
            ? OperadoresRotacionCalculator.CalcularRotacion(viajesCerrados, mesesCerrados)
            : new RotacionOperadoresDto([], new RotacionSucursalDto("TOTAL", 0, 0, 0, null, "Sin datos"));

        var semaforo = CalcularSemaforo(nivelZemog, porCliente, arbol, agenciasDesaparecidas, rotacion, hayComparativos);

        return new ResumenEjecutivoDto(
            mesesCerrados, mesAbiertoExcluido, etiquetaMesAbierto, hayComparativos,
            semaforo, nivelZemog, porCliente, arbol, agenciasDesaparecidas, operadores, rotacion);
    }

    // ---------- Meses cerrados (replica PR_corteInfo — ver nota de clase) ----------

    private static (List<MesCerrado> Meses, bool Excluido, string? EtiquetaExcluida) CalcularMesesCerrados(
        IReadOnlyList<ViajesDto> viajes, CorteMensual? corte)
    {
        var meses = viajes
            .Select(CamposDerivadosViajes.ObtenerFechaNegocio)
            .Where(f => f is not null)
            .Select(f => (Anio: f!.Value.Year, Mes: f.Value.Month))
            .Distinct()
            .OrderBy(m => m.Anio).ThenBy(m => m.Mes)
            .Select(m => new MesCerrado(m.Anio, m.Mes, EtiquetaMes(m.Anio, m.Mes)))
            .ToList();

        if (corte is null || meses.Count == 0)
            return (meses, false, null);

        var ultimo = meses[^1];
        var esAvance = corte.DiaCorte < 28 && ultimo.Anio == corte.Anio && ultimo.Mes == corte.Mes;

        if (!esAvance)
            return (meses, false, null);

        var etiquetaExcluida = ultimo.Etiqueta;
        meses.RemoveAt(meses.Count - 1);
        return (meses, true, etiquetaExcluida);
    }

    private static string EtiquetaMes(int anio, int mes) => $"{NombresMes[mes - 1]} {anio}";

    private static bool EstaEnMesesCerrados(ViajesDto v, IReadOnlyList<MesCerrado> mesesCerrados)
    {
        var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
        return fecha is not null && mesesCerrados.Any(m => m.Anio == fecha.Value.Year && m.Mes == fecha.Value.Month);
    }

    // ---------- Bloques 8.2/8.3 — Nivel general / Por Cliente (replica RE_bloqueNivel) ----------

    private static BloqueNivelDto CalcularBloqueNivel(string titulo, IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var totalesPorMes = mesesCerrados.ToDictionary(m => m, _ => TotalesPeriodo.Vacio);

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = mesesCerrados.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            totalesPorMes[mes] = TotalesPeriodo.Sumar(totalesPorMes[mes], new TotalesPeriodo(esIda ? 1 : 0, v.kms_viaje ?? 0));
        }

        var ultimo = mesesCerrados[^1];
        var anterior = mesesCerrados.Count > 1 ? mesesCerrados[^2] : (MesCerrado?)null;
        var primerMesDelAnio = mesesCerrados.First(m => m.Anio == ultimo.Anio);

        return new BloqueNivelDto(
            titulo,
            anterior,
            anterior is null ? null : totalesPorMes[anterior.Value],
            ultimo,
            totalesPorMes[ultimo],
            primerMesDelAnio,
            totalesPorMes[primerMesDelAnio],
            mesesCerrados.Select(m => (m, totalesPorMes[m])).ToList());
    }

    // ---------- Bloques 8.4/8.6/8.7 — árbol comparativo compartido (replica RE_arbol) ----------

    private static NodoComparativo ConstruirArbolComparativo(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = mesesCerrados[^1];
        var anterior = mesesCerrados.Count > 1 ? mesesCerrados[^2] : (MesCerrado?)null;

        var raiz = new NodoComparativo { Id = "", Label = "TOTAL", Nivel = -1 };

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = mesesCerrados.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            var contribucion = new TotalesPeriodo(esIda ? 1 : 0, v.kms_viaje ?? 0);

            AcumularComparativo(raiz, mes, ultimo, anterior, contribucion, v.expedicion, esIda);

            var nodo = raiz;
            foreach (var nivelSelector in NivelesArbol)
            {
                var clave = nivelSelector(v) is { Length: > 0 } valor ? valor : "(sin dato)";
                nodo = ObtenerOCrearHijoComparativo(nodo, clave);
                AcumularComparativo(nodo, mes, ultimo, anterior, contribucion, v.expedicion, esIda);
            }
        }

        return raiz;
    }

    private static NodoComparativo ObtenerOCrearHijoComparativo(NodoComparativo padre, string clave)
    {
        if (padre.Hijos.TryGetValue(clave, out var existente))
            return existente;

        var nuevo = new NodoComparativo
        {
            Id = $"{padre.Id}>{clave}",
            Label = clave,
            Nivel = padre.Nivel + 1,
            IsExpanded = padre.Nivel + 1 == 0,
        };
        padre.Hijos[clave] = nuevo;
        return nuevo;
    }

    // Anual = solo meses cerrados DEL MISMO AÑO que "ultimo" ("avance del año") — parte de la
    // corrección del bug de agrupar por mes-sin-año del HTML (ver nota de clase).
    private static void AcumularComparativo(
        NodoComparativo nodo, MesCerrado mes, MesCerrado ultimo, MesCerrado? anterior,
        TotalesPeriodo contribucion, string? expedicion, bool esIda)
    {
        if (mes.Anio == ultimo.Anio)
            nodo.Anual = TotalesPeriodo.Sumar(nodo.Anual, contribucion);

        if (mes.Anio == ultimo.Anio && mes.Mes == ultimo.Mes)
        {
            nodo.Ultimo = TotalesPeriodo.Sumar(nodo.Ultimo, contribucion);
            if (esIda && expedicion is { Length: > 0 })
                nodo.ExpedicionUltimo[expedicion] = nodo.ExpedicionUltimo.GetValueOrDefault(expedicion) + 1;
        }
        else if (anterior is not null && mes.Anio == anterior.Value.Anio && mes.Mes == anterior.Value.Mes)
        {
            nodo.Anterior = TotalesPeriodo.Sumar(nodo.Anterior, contribucion);
            if (esIda && expedicion is { Length: > 0 })
                nodo.ExpedicionAnterior[expedicion] = nodo.ExpedicionAnterior.GetValueOrDefault(expedicion) + 1;
        }
    }

    // ---------- Bloque 8.6 — Asignación Comodato/Full/Sencillo (replica RE_asigVals) ----------

    public static (int Comodato, int Full, int Sencillo, int Total, decimal PctComodato, decimal? DeltaPuntosPorcentuales) CalcularAsignacion(NodoComparativo nodo)
    {
        var co = nodo.ExpedicionUltimo.GetValueOrDefault("Comodato");
        var fu = nodo.ExpedicionUltimo.GetValueOrDefault("Full");
        var se = nodo.ExpedicionUltimo.GetValueOrDefault("Sencillo");
        var tt = co + fu + se;
        var pc = tt > 0 ? (decimal)co / tt * 100 : 0m;

        var ca = nodo.ExpedicionAnterior.GetValueOrDefault("Comodato");
        var fa = nodo.ExpedicionAnterior.GetValueOrDefault("Full");
        var sa = nodo.ExpedicionAnterior.GetValueOrDefault("Sencillo");
        var ta = ca + fa + sa;
        var pa = ta > 0 ? (decimal?)((decimal)ca / ta * 100) : null;

        return (co, fu, se, tt, pc, pa is null ? null : pc - pa.Value);
    }

    // ---------- Bloque 8.7 — Frecuencia por agencia (replica RE_pintaJerFrec, regla de alerta) ----------

    public static decimal? CalcularDeltaFrecuenciaPct(NodoComparativo nodo) =>
        nodo.Anterior.Viajes > 0 ? (decimal)(nodo.Ultimo.Viajes - nodo.Anterior.Viajes) / nodo.Anterior.Viajes * 100 : null;

    // Nivel >= 2 = Matriz o más profundo (Cliente=0, Zona=1, Matriz=2, Sucursal=3), igual que
    // "f.lvl>=2" en el HTML — pero "f.lvl" en el HTML es el nivel de la FILA YA RENDERIZADA
    // (post colapso de cadenas de hijo único), no el nivel del nodo crudo. Recorrer el árbol
    // crudo sin colapsar contaría dos veces la alerta de Matriz y de Sucursal cuando son la
    // misma entidad (Sucursal = Matriz, fallback permanente hoy — ver §54.108/§54.121): por
    // eso aquí también se colapsa antes de evaluar, igual que hará ResumenArbolComparativo.razor
    // al pintar la tabla — un único candidato por fila renderizada, igual que el HTML.
    private static List<AlertaFrecuencia> RecolectarAlertasFrecuencia(NodoComparativo raiz)
    {
        var alertas = new List<AlertaFrecuencia>();
        const int profundidadMaxima = 4; // Cliente(0), Zona(1), Matriz(2), Sucursal(3)

        void Caminar(NodoComparativo nodo, int nivelFila)
        {
            foreach (var hijoOriginal in nodo.Hijos.Values)
            {
                var efectivo = hijoOriginal;
                var nivelEfectivo = nivelFila;
                while (nivelEfectivo < profundidadMaxima - 1 && efectivo.Hijos.Count == 1)
                {
                    efectivo = efectivo.Hijos.Values.Single();
                    nivelEfectivo++;
                }

                if (nivelFila >= 2 && efectivo.Anterior.Viajes >= 20)
                {
                    var delta = (decimal)(efectivo.Ultimo.Viajes - efectivo.Anterior.Viajes) / efectivo.Anterior.Viajes * 100;
                    if (delta <= -15)
                        alertas.Add(new AlertaFrecuencia(hijoOriginal.Label, delta));
                }

                var esHoja = nivelEfectivo == profundidadMaxima - 1 || efectivo.Hijos.Count == 0;
                if (!esHoja)
                    Caminar(efectivo, nivelFila + 1);
            }
        }

        Caminar(raiz, 0);
        return alertas.OrderBy(a => a.DeltaPorcentaje).ToList();
    }

    // ---------- Bloque 8.8 — Agencias que ya no aparecen (replica RE_seccionDesaparecidas) ----------

    private static List<AgenciaDesaparecidaDto> CalcularAgenciasDesaparecidas(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> mesesCerrados)
    {
        var mesPorClave = mesesCerrados.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = mesesCerrados[^1];

        var porAgencia = new Dictionary<(string Destino, string Matriz), Dictionary<MesCerrado, int>>();

        foreach (var v in viajes)
        {
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            if (!esIda)
                continue; // replica r.viaje (0 en tramos de Regreso): un Regreso no "visita" la agencia

            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = mesesCerrados.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var destino = CamposDerivadosViajes.ObtenerDestino(v) ?? "(sin dato)";
            var matriz = v._base ?? "(sin dato)";
            var clave = (destino, matriz);

            if (!porAgencia.TryGetValue(clave, out var porMes))
                porAgencia[clave] = porMes = [];

            porMes[mes] = porMes.GetValueOrDefault(mes) + 1;
        }

        var resultado = new List<AgenciaDesaparecidaDto>();
        foreach (var ((destino, matriz), porMes) in porAgencia)
        {
            var ultimoVisto = porMes.Keys.OrderBy(m => m.Anio).ThenBy(m => m.Mes).Last();
            if (ultimoVisto.Anio == ultimo.Anio && ultimoVisto.Mes == ultimo.Mes)
                continue; // sigue activa en el último mes cerrado

            resultado.Add(new AgenciaDesaparecidaDto(destino, matriz, ultimoVisto, porMes[ultimoVisto], porMes.Count));
        }

        // El HTML ordena por venta acumulada descendente y muestra Top 30 (las cuentas más
        // importantes perdidas). Sin Venta no hay criterio de "importancia" real que replicar
        // sin inventarlo — se ordena alfabéticamente y se muestran TODAS, sin cupo arbitrario.
        return resultado
            .OrderBy(a => a.Destino, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(a => a.Matriz, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ---------- Bloque 8.1 — Semáforo (replica los sem.push(...) de RE_render()) ----------

    private static List<AlertaSemaforo> CalcularSemaforo(
        BloqueNivelDto? nivelZemog,
        IReadOnlyList<NivelPorClienteDto> porCliente,
        NodoComparativo? arbol,
        IReadOnlyList<AgenciaDesaparecidaDto> agenciasDesaparecidas,
        RotacionOperadoresDto rotacion,
        bool hayComparativos)
    {
        var alertas = new List<AlertaSemaforo>();

        if (nivelZemog is null)
            return alertas;

        alertas.Add(new AlertaSemaforo($"Peor mes del año a nivel Zemog: Pendiente — {PendienteVenta}", SeveridadAlerta.Neutral));

        var etiquetaAnterior = nivelZemog.MesAnterior?.Etiqueta ?? "mes anterior";
        alertas.Add(new AlertaSemaforo($"Venta de {nivelZemog.MesUltimo.Etiqueta} vs {etiquetaAnterior}: Pendiente — {PendienteVenta}", SeveridadAlerta.Neutral));

        foreach (var c in porCliente)
        {
            var etiquetaAnteriorCliente = c.Bloque.MesAnterior?.Etiqueta ?? "mes anterior";
            alertas.Add(new AlertaSemaforo(
                $"{c.Cliente}: venta {c.Bloque.MesUltimo.Etiqueta} vs {etiquetaAnteriorCliente}: Pendiente — {PendienteVenta}",
                SeveridadAlerta.Neutral));
        }

        if (!hayComparativos)
            return alertas;

        alertas.Add(new AlertaSemaforo($"Destino con mayor caída: Pendiente — Bloque 8.5 requiere Venta, ver §54.122", SeveridadAlerta.Neutral));

        var alertasFrecuencia = arbol is null ? [] : RecolectarAlertasFrecuencia(arbol);
        if (alertasFrecuencia.Count > 0)
        {
            var top4 = alertasFrecuencia.Take(4).Select(a => $"{a.Matriz} ({FormatoPorcentaje(a.DeltaPorcentaje)})");
            var extra = alertasFrecuencia.Count > 4 ? $" y {alertasFrecuencia.Count - 4} más" : "";
            alertas.Add(new AlertaSemaforo(
                $"Matrices/sucursales con caída de viajes a revisar: {string.Join(", ", top4)}{extra}",
                SeveridadAlerta.Negativa));
        }

        if (agenciasDesaparecidas.Count > 0)
        {
            alertas.Add(new AlertaSemaforo(
                $"{agenciasDesaparecidas.Count} agencias/destinos ya no aparecen en {nivelZemog.MesUltimo.Etiqueta} (venta acumulada: Pendiente — {PendienteVenta})",
                SeveridadAlerta.Negativa));
        }

        alertas.Add(new AlertaSemaforo(
            $"Operadores: {rotacion.Total.Activos} activos en {nivelZemog.MesUltimo.Etiqueta}, {rotacion.Total.Altas} altas y {rotacion.Total.Bajas} dejaron de aparecer en el año",
            SeveridadAlerta.Neutral));

        return alertas;
    }

    public static string FormatoPorcentaje(decimal v) => (v >= 0 ? "+" : "") + v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
}