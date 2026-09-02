namespace Testing.Application.GetAllViajes;

/// <summary>Bloque 8.6 (RE_asigVals) — mezcla de Expedición (Comodato/Full/Sencillo) de UN nodo del árbol, último mes vs anterior, con $/viaje por tipo.</summary>
public sealed record AsignacionExpedicionDto(
    int Comodato, int Full, int Sencillo, int Total,
    decimal PctComodato, decimal? DeltaPuntosPorcentuales,
    decimal VentaPorViajeComodato, decimal VentaPorViajeFull, decimal VentaPorViajeSencillo);

/// <summary>
/// Replica el Resumen Ejecutivo de viajes_v14.html (RE_render()).
///
/// AJUSTE DE ESTA FASE (auditoría de "Tabla y Resumen Ejecutivo"): RE_render() se construye
/// DIRECTO sobre DATA -- confirmado línea por línea que no existe ninguna exclusión del mes de
/// corte/avance dentro de RE_render ni de ninguna función RE_ que llame. Esa regla ("avance",
/// día de corte &lt; 28 => comparar con los 2 meses cerrados anteriores) es EXCLUSIVA de
/// Presentación (PR_corteInfo/PR_build) -- este archivo YA NO la aplica. Si Presentación necesita
/// esa exclusión, filtra los viajes ANTES de llamar a Calcular() (ver SlidesPresentacionCalculator).
///
/// "DATA ya contiene valores proyectados del mes de corte": todo aquí usa TotalesPeriodo.De(v,
/// corte) / ContribucionViajeProyectada, igual que la vista principal -- antes de esta fase se
/// usaban sumas crudas sin proyectar, lo cual era inconsistente con "Resumen Ejecutivo se
/// construye directo sobre DATA" (que SÍ está proyectada).
/// </summary>
public static class ResumenEjecutivoCalculator
{
    private static readonly string[] NombresMes =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    private static readonly Func<ViajesDto, string?>[] NivelesArbol =
    [
        CamposDerivadosViajes.ObtenerCliente,
        CamposDerivadosViajes.ObtenerZona,
        v => v._base,
        v => v._base, // Sucursal = Matriz = _base, confirmado por el usuario.
    ];

    public static ResumenEjecutivoDto Calcular(IReadOnlyList<ViajesDto> viajesCargados, CorteMensual? corte)
    {
        var meses = CalcularMeses(viajesCargados);
        var viajesConFecha = viajesCargados.Where(v => EstaEnMeses(v, meses)).ToList();
        var hayComparativos = meses.Count >= 2;

        var nivelZemog = meses.Count == 0 ? null : CalcularBloqueNivel("Zemog", viajesConFecha, meses, corte);

        var porCliente = meses.Count == 0
            ? []
            : viajesConFecha
                .Select(CamposDerivadosViajes.ObtenerCliente)
                .Where(c => c is { Length: > 0 })
                .Distinct()
                .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .Select(cliente => new NivelPorClienteDto(
                    cliente!,
                    CalcularBloqueNivel(
                        cliente!,
                        viajesConFecha.Where(v => CamposDerivadosViajes.ObtenerCliente(v) == cliente).ToList(),
                        meses, corte)))
                .ToList();

        var arbol = hayComparativos ? ConstruirArbolComparativo(viajesConFecha, meses, corte) : null;

        var destinosCayendo = hayComparativos ? CalcularDestinosCayendo(viajesConFecha, meses, corte) : null;

        var agenciasDesaparecidas = meses.Count == 0
            ? []
            : CalcularAgenciasDesaparecidas(viajesConFecha, meses, corte);

        var operadores = OperadoresRotacionCalculator.CalcularOperadores(viajesConFecha, meses, corte);

        var rotacion = hayComparativos
            ? OperadoresRotacionCalculator.CalcularRotacion(viajesConFecha, meses, corte)
            : new RotacionOperadoresDto([], new RotacionSucursalDto("TOTAL", 0, 0, 0, null, "Sin datos", 0));

        var semaforo = CalcularSemaforo(nivelZemog, porCliente, arbol, destinosCayendo, agenciasDesaparecidas, rotacion, hayComparativos);

        return new ResumenEjecutivoDto(meses, hayComparativos, semaforo, nivelZemog, porCliente, arbol, destinosCayendo, agenciasDesaparecidas, operadores, rotacion);
    }

    // ---------- Meses presentes en los datos (SIN exclusión -- ver nota de clase) ----------

    private static List<MesCerrado> CalcularMeses(IReadOnlyList<ViajesDto> viajes) =>
        viajes
            .Select(CamposDerivadosViajes.ObtenerFechaNegocio)
            .Where(f => f is not null)
            .Select(f => (Anio: f!.Value.Year, Mes: f.Value.Month))
            .Distinct()
            .OrderBy(m => m.Anio).ThenBy(m => m.Mes)
            .Select(m => new MesCerrado(m.Anio, m.Mes, EtiquetaMes(m.Anio, m.Mes)))
            .ToList();

    private static string EtiquetaMes(int anio, int mes) => $"{NombresMes[mes - 1]} {anio}";

    private static bool EstaEnMeses(ViajesDto v, IReadOnlyList<MesCerrado> meses)
    {
        var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
        return fecha is not null && meses.Any(m => m.Anio == fecha.Value.Year && m.Mes == fecha.Value.Month);
    }

    // ---------- Bloques 8.2/8.3 — Nivel general / Por Cliente (replica RE_bloqueNivel) ----------

    private static BloqueNivelDto CalcularBloqueNivel(string titulo, IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var totalesPorMes = meses.ToDictionary(m => m, _ => TotalesPeriodo.Vacio);

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            totalesPorMes[mes] = TotalesPeriodo.Sumar(totalesPorMes[mes], TotalesPeriodo.De(v, corte));
        }

        var ultimo = meses[^1];
        var anterior = meses.Count > 1 ? meses[^2] : (MesCerrado?)null;
        // "Primer mes" = mesesOrdenados[0], SIN filtrar por año (confirmado en esta fase -- ver
        // nota de clase de BloqueNivelDto). La UI sigue llamándolo "avance del año" (texto
        // heredado del HTML), pero el dato real es el primer mes de TODO el rango cargado.
        var primerMes = meses[0];

        return new BloqueNivelDto(
            titulo, anterior, anterior is null ? null : totalesPorMes[anterior.Value],
            ultimo, totalesPorMes[ultimo], primerMes, totalesPorMes[primerMes],
            meses.Select(m => (m, totalesPorMes[m])).ToList());
    }

    // ---------- Bloques 8.4/8.6/8.7 — árbol comparativo compartido (replica RE_arbol) ----------

    private static NodoComparativo ConstruirArbolComparativo(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = meses[^1];
        var anterior = meses.Count > 1 ? meses[^2] : (MesCerrado?)null;

        var raiz = new NodoComparativo { Id = "", Label = "TOTAL", Nivel = -1 };

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            var contribucion = TotalesPeriodo.De(v, corte);

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

        var nuevo = new NodoComparativo { Id = $"{padre.Id}>{clave}", Label = clave, Nivel = padre.Nivel + 1, IsExpanded = padre.Nivel + 1 == 0 };
        padre.Hijos[clave] = nuevo;
        return nuevo;
    }

    // Anual = TODO el rango cargado, SIN filtrar por año -- confirmado en esta fase:
    // RE_arbol.suma() acumula n.y para cada fila de DATA incondicionalmente, sin comparar contra
    // el año del último mes. (Antes de esta fase se filtraba por año -- corregido.)
    private static void AcumularComparativo(
        NodoComparativo nodo, MesCerrado mes, MesCerrado ultimo, MesCerrado? anterior,
        TotalesPeriodo contribucion, string? expedicion, bool esIda)
    {
        nodo.Anual = TotalesPeriodo.Sumar(nodo.Anual, contribucion);

        if (mes.Anio == ultimo.Anio && mes.Mes == ultimo.Mes)
        {
            nodo.Ultimo = TotalesPeriodo.Sumar(nodo.Ultimo, contribucion);
            if (esIda && expedicion is { Length: > 0 })
                nodo.ExpedicionUltimo[expedicion] = nodo.ExpedicionUltimo.GetValueOrDefault(expedicion) + 1;
            if (expedicion is { Length: > 0 })
                nodo.ExpedicionVentaUltimo[expedicion] = nodo.ExpedicionVentaUltimo.GetValueOrDefault(expedicion) + contribucion.Venta;
        }
        else if (anterior is not null && mes.Anio == anterior.Value.Anio && mes.Mes == anterior.Value.Mes)
        {
            nodo.Anterior = TotalesPeriodo.Sumar(nodo.Anterior, contribucion);
            if (esIda && expedicion is { Length: > 0 })
                nodo.ExpedicionAnterior[expedicion] = nodo.ExpedicionAnterior.GetValueOrDefault(expedicion) + 1;
            if (expedicion is { Length: > 0 })
                nodo.ExpedicionVentaAnterior[expedicion] = nodo.ExpedicionVentaAnterior.GetValueOrDefault(expedicion) + contribucion.Venta;
        }
    }

    // ---------- Bloque 8.6 — Asignación Comodato/Full/Sencillo, UN nodo (replica RE_asigVals) ----------
    // Se llama una vez POR NODO renderizado (raíz y cada fila de la tabla jerárquica de
    // Asignación) -- RE_jerAsig es una tabla jerárquica completa, no un resumen de una sola fila
    // a nivel raíz (corregido en esta fase: antes solo se llamaba sobre la raíz).

    public static AsignacionExpedicionDto CalcularAsignacion(NodoComparativo nodo)
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

        var vCo = nodo.ExpedicionVentaUltimo.GetValueOrDefault("Comodato");
        var vFu = nodo.ExpedicionVentaUltimo.GetValueOrDefault("Full");
        var vSe = nodo.ExpedicionVentaUltimo.GetValueOrDefault("Sencillo");

        return new AsignacionExpedicionDto(
            co, fu, se, tt, pc, pa is null ? null : pc - pa.Value,
            VentaPorViajeComodato: co > 0 ? vCo / co : 0,
            VentaPorViajeFull: fu > 0 ? vFu / fu : 0,
            VentaPorViajeSencillo: se > 0 ? vSe / se : 0);
    }

    // ---------- Bloque 8.5 — Destinos que estamos dejando de dar (replica RE_seccionDestinos) ----------
    // Agrupa por Destino+Matriz (NO solo Destino). Condición: ventaAnterior>0 && ventaActual<
    // ventaAnterior, SIN umbral mínimo. Orden: DeltaVenta ascendente (mayor pérdida primero).
    // ImpactoTotal/TotalConCaida se calculan sobre TODOS los grupos que cumplen, ANTES del
    // recorte a Top 25 -- el TOTAL de la tabla en pantalla es solo la suma de esas 25 filas.

    private static DestinosCayendoResumenDto CalcularDestinosCayendo(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte)
    {
        var ultimo = meses[^1];
        var anterior = meses[^2];

        var acumPorGrupoMes = new Dictionary<(string Destino, string Matriz, MesCerrado Mes), TotalesPeriodo>();

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null)
                continue;

            MesCerrado mes;
            if (fecha.Value.Year == ultimo.Anio && fecha.Value.Month == ultimo.Mes) mes = ultimo;
            else if (fecha.Value.Year == anterior.Anio && fecha.Value.Month == anterior.Mes) mes = anterior;
            else continue;

            var destino = CamposDerivadosViajes.ObtenerDestino(v) ?? "(sin dato)";
            var matriz = v._base ?? "(sin dato)";
            var clave = (destino, matriz, mes);
            acumPorGrupoMes[clave] = TotalesPeriodo.Sumar(acumPorGrupoMes.GetValueOrDefault(clave, TotalesPeriodo.Vacio), TotalesPeriodo.De(v, corte));
        }

        var grupos = acumPorGrupoMes.Keys.Select(k => (k.Destino, k.Matriz)).Distinct();

        var candidatos = grupos
            .Select(g =>
            {
                var a = acumPorGrupoMes.GetValueOrDefault((g.Destino, g.Matriz, anterior), TotalesPeriodo.Vacio);
                var b = acumPorGrupoMes.GetValueOrDefault((g.Destino, g.Matriz, ultimo), TotalesPeriodo.Vacio);
                return new DestinoCayendoDto(g.Destino, g.Matriz, a.Viajes, b.Viajes, a.Venta, b.Venta);
            })
            .Where(d => d.VentaAnterior > 0 && d.VentaActual < d.VentaAnterior)
            .OrderBy(d => d.DeltaVenta) // ascendente = más negativo (mayor pérdida) primero
            .ToList();

        return new DestinosCayendoResumenDto(
            TotalConCaida: candidatos.Count,
            ImpactoTotal: candidatos.Sum(d => d.DeltaVenta),
            Top25: candidatos.Take(25).ToList());
    }

    // ---------- Bloque 8.7 — Frecuencia por agencia (replica RE_jerFrec) ----------
    // Tabla jerárquica completa (TODOS los niveles, no solo Matriz+) -- "Señal" se calcula para
    // cada fila; el semáforo (8.1) es el único que filtra a nivel>=2 (confirmado en esta fase:
    // antes se filtraba también la tabla, incorrectamente).

    public static List<FilaFrecuenciaDto> ConstruirTablaFrecuencia(NodoComparativo raiz)
    {
        var filas = new List<FilaFrecuenciaDto>();
        const int profundidadMaxima = 4;

        void Caminar(NodoComparativo nodo, int nivelFila)
        {
            foreach (var hijoOriginal in nodo.Hijos.Values.OrderBy(h => h.Label, StringComparer.CurrentCultureIgnoreCase))
            {
                var efectivo = hijoOriginal;
                var nivelEfectivo = nivelFila;
                while (nivelEfectivo < profundidadMaxima - 1 && efectivo.Hijos.Count == 1)
                {
                    efectivo = efectivo.Hijos.Values.Single();
                    nivelEfectivo++;
                }

                var delta = efectivo.Anterior.Viajes > 0 ? (efectivo.Ultimo.Viajes - efectivo.Anterior.Viajes) / efectivo.Anterior.Viajes * 100 : (decimal?)null;
                var alerta = efectivo.Anterior.Viajes >= 20 && delta is not null && delta <= -15;

                filas.Add(new FilaFrecuenciaDto(nivelFila, hijoOriginal.Label, efectivo.Anterior.Viajes, efectivo.Ultimo.Viajes, delta, efectivo.Ultimo.Venta, alerta));

                var esHoja = nivelEfectivo == profundidadMaxima - 1 || efectivo.Hijos.Count == 0;
                if (!esHoja)
                    Caminar(efectivo, nivelFila + 1);
            }
        }

        Caminar(raiz, 0);
        return filas;
    }

    // Solo para el semáforo (8.1): nivel Matriz o más profundo (>=2), y con alerta real.
    private static List<AlertaFrecuencia> RecolectarAlertasFrecuencia(NodoComparativo raiz) =>
        ConstruirTablaFrecuencia(raiz)
            .Where(f => f.Nivel >= 2 && f.Alerta)
            .Select(f => new AlertaFrecuencia(f.Label, f.DeltaPorcentaje!.Value))
            .OrderBy(f => f.DeltaPorcentaje)
            .ToList();

    // ---------- Bloque 8.8 — Agencias que ya no aparecen ----------

    private static List<AgenciaDesaparecidaDto> CalcularAgenciasDesaparecidas(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = meses[^1];

        var porAgencia = new Dictionary<(string Destino, string Matriz), Dictionary<MesCerrado, decimal>>();
        var ventaPorAgenciaMes = new Dictionary<(string Destino, string Matriz), Dictionary<MesCerrado, decimal>>();

        foreach (var v in viajes)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var destino = CamposDerivadosViajes.ObtenerDestino(v) ?? "(sin dato)";
            var matriz = v._base ?? "(sin dato)";
            var clave = (destino, matriz);

            if (!ventaPorAgenciaMes.TryGetValue(clave, out var porMesVenta))
                ventaPorAgenciaMes[clave] = porMesVenta = [];
            porMesVenta[mes] = porMesVenta.GetValueOrDefault(mes) + ContribucionViajeProyectada.Venta(v, corte);

            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            if (!esIda)
                continue; // "presencia"/conteo de viajes: replica r.viaje (0 en tramos de Regreso)

            if (!porAgencia.TryGetValue(clave, out var porMes))
                porAgencia[clave] = porMes = [];

            porMes[mes] = porMes.GetValueOrDefault(mes) + ContribucionViajeProyectada.Viajes(v, corte);
        }

        var resultado = new List<AgenciaDesaparecidaDto>();
        foreach (var ((destino, matriz), porMes) in porAgencia)
        {
            var ultimoVisto = porMes.Keys.OrderBy(m => m.Anio).ThenBy(m => m.Mes).Last();
            if (ultimoVisto.Anio == ultimo.Anio && ultimoVisto.Mes == ultimo.Mes)
                continue; // sigue activa en el último mes

            var ventaAcumulada = ventaPorAgenciaMes.TryGetValue((destino, matriz), out var porMesVenta) ? porMesVenta.Values.Sum() : 0m;

            resultado.Add(new AgenciaDesaparecidaDto(destino, matriz, ultimoVisto, porMes[ultimoVisto], porMes.Count, ventaAcumulada));
        }

        return resultado
            .OrderByDescending(a => a.VentaAcumulada)
            .ThenBy(a => a.Destino, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ---------- Bloque 8.1 — Semáforo (replica los sem.push(...) de RE_render()) ----------

    private static List<AlertaSemaforo> CalcularSemaforo(
        BloqueNivelDto? nivelZemog,
        IReadOnlyList<NivelPorClienteDto> porCliente,
        NodoComparativo? arbol,
        DestinosCayendoResumenDto? destinosCayendo,
        IReadOnlyList<AgenciaDesaparecidaDto> agenciasDesaparecidas,
        RotacionOperadoresDto rotacion,
        bool hayComparativos)
    {
        var alertas = new List<AlertaSemaforo>();

        if (nivelZemog is null)
            return alertas;

        var peor = nivelZemog.PeorMesDelAnio;
        alertas.Add(new AlertaSemaforo(
            peor is null ? "Peor mes a nivel Zemog: sin datos suficientes" : $"Peor mes a nivel Zemog: {peor.Value.Mes.Etiqueta} ({FormatoDinero(peor.Value.Venta)})",
            SeveridadAlerta.Neutral));

        var etiquetaAnterior = nivelZemog.MesAnterior?.Etiqueta ?? "mes anterior";
        var deltaVenta = nivelZemog.DeltaVentaPctVsAnterior;
        alertas.Add(new AlertaSemaforo(
            $"Venta de {nivelZemog.MesUltimo.Etiqueta} vs {etiquetaAnterior}: {(deltaVenta is null ? "sin base en " + etiquetaAnterior : FormatoPorcentaje(deltaVenta.Value))}",
            deltaVenta switch { > 0 => SeveridadAlerta.Positiva, < 0 => SeveridadAlerta.Negativa, _ => SeveridadAlerta.Neutral }));

        foreach (var c in porCliente)
        {
            var etiquetaAnteriorCliente = c.Bloque.MesAnterior?.Etiqueta ?? "mes anterior";
            var deltaVentaCliente = c.Bloque.DeltaVentaPctVsAnterior;
            alertas.Add(new AlertaSemaforo(
                $"{c.Cliente}: venta {c.Bloque.MesUltimo.Etiqueta} vs {etiquetaAnteriorCliente}: {(deltaVentaCliente is null ? "sin base en " + etiquetaAnteriorCliente : FormatoPorcentaje(deltaVentaCliente.Value))}",
                deltaVentaCliente switch { > 0 => SeveridadAlerta.Positiva, < 0 => SeveridadAlerta.Negativa, _ => SeveridadAlerta.Neutral }));
        }

        if (!hayComparativos)
            return alertas;

        if (destinosCayendo is { TotalConCaida: > 0 })
        {
            var peorDestino = destinosCayendo.Top25[0];
            alertas.Add(new AlertaSemaforo(
                $"{destinosCayendo.TotalConCaida} destinos con caída de venta (impacto total {FormatoDinero(destinosCayendo.ImpactoTotal)}); mayor caída: {peorDestino.Destino} ({FormatoDinero(peorDestino.DeltaVenta)})",
                SeveridadAlerta.Negativa));
        }
        else
        {
            alertas.Add(new AlertaSemaforo("Destinos: ninguno con caída de venta este mes", SeveridadAlerta.Positiva));
        }

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
            var ventaTotal = agenciasDesaparecidas.Sum(a => a.VentaAcumulada);
            alertas.Add(new AlertaSemaforo(
                $"{agenciasDesaparecidas.Count} agencias/destinos ya no aparecen en {nivelZemog.MesUltimo.Etiqueta} (venta acumulada: {FormatoDinero(ventaTotal)})",
                SeveridadAlerta.Negativa));
        }

        alertas.Add(new AlertaSemaforo(
            $"Operadores: {rotacion.Total.Activos} activos en {nivelZemog.MesUltimo.Etiqueta}, {rotacion.Total.Altas} altas y {rotacion.Total.Bajas} dejaron de aparecer (venta acumulada de bajas: {FormatoDinero(rotacion.Total.VentaBajas)})",
            SeveridadAlerta.Neutral));

        return alertas;
    }

    public static string FormatoPorcentaje(decimal v) => (v >= 0 ? "+" : "") + v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";

    public static string FormatoDinero(decimal v) => v.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
}