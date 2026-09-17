namespace Testing.Application.GetAllViajes;

public sealed record AsignacionExpedicionDto(
    decimal Comodato, decimal Full, decimal Sencillo, decimal Total,
    decimal? PctComodato, decimal? DeltaPuntosPorcentuales,
    decimal VentaPorViajeComodato, decimal VentaPorViajeFull, decimal VentaPorViajeSencillo);

public static class ResumenEjecutivoCalculator
{
    public static ResumenEjecutivoDto Calcular(IReadOnlyList<ViajesDto> viajesCargados, CorteMensual? corte)
    {
        var fechaCache = new Dictionary<ViajesDto, DateTime?>(viajesCargados.Count);
        DateTime? FechaDe(ViajesDto v)
        {
            if (fechaCache.TryGetValue(v, out var f))
                return f;

            f = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            fechaCache[v] = f;
            return f;
        }

        var meses = ResumenBloqueNivelCalculator.CalcularMeses(viajesCargados, FechaDe);
        var viajesConFecha = viajesCargados.Where(v => ResumenBloqueNivelCalculator.EstaEnMeses(v, meses, FechaDe)).ToList();
        var hayComparativos = meses.Count >= 2;

        var nivelZemog = meses.Count == 0 ? null : ResumenBloqueNivelCalculator.CalcularBloqueNivel("Zemog", viajesConFecha, meses, corte, FechaDe);

        var porCliente = meses.Count == 0
            ? []
            : viajesConFecha
                .Select(CamposDerivadosViajes.ObtenerCliente)
                .Where(c => c is { Length: > 0 })
                .Distinct()
                .OrderBy(CamposDerivadosViajes.PrioridadCliente)
                .ThenBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .Select(cliente =>
                {
                    var viajesCliente = viajesConFecha.Where(v => CamposDerivadosViajes.ObtenerCliente(v) == cliente).ToList();
                    var mesesCliente = ResumenBloqueNivelCalculator.CalcularMeses(viajesCliente, FechaDe);
                    return new NivelPorClienteDto(cliente!, ResumenBloqueNivelCalculator.CalcularBloqueNivel(cliente!, viajesCliente, mesesCliente, corte, FechaDe));
                })
                .ToList();

        var arbol = hayComparativos ? ResumenArbolComparativoBuilder.ConstruirArbolComparativo(viajesConFecha, meses, corte, FechaDe) : null;

        var destinosCayendo = hayComparativos ? ResumenDestinosCayendoCalculator.CalcularDestinosCayendo(viajesConFecha, meses, corte, FechaDe) : null;

        var agenciasDesaparecidas = meses.Count == 0
            ? new AgenciasDesaparecidasResumenDto(0, 0, [])
            : ResumenAgenciasDesaparecidasCalculator.CalcularAgenciasDesaparecidas(viajesConFecha, meses, corte, FechaDe);

        var operadores = OperadoresRotacionCalculator.CalcularOperadores(viajesConFecha, meses, corte);

        var rotacion = hayComparativos
            ? OperadoresRotacionCalculator.CalcularRotacion(viajesConFecha, meses, corte)
            : new RotacionOperadoresDto([], new RotacionSucursalDto("TOTAL", 0, 0, 0, 0, 0, null, "Sin datos", 0));

        var semaforo = ResumenSemaforoCalculator.CalcularSemaforo(nivelZemog, porCliente, arbol, destinosCayendo, agenciasDesaparecidas, rotacion, hayComparativos);

        var armadosDesconocidos = ResumenArmadosDesconocidosCalculator.CalcularArmadosDesconocidos(viajesConFecha);

        return new ResumenEjecutivoDto(meses, hayComparativos, semaforo, nivelZemog, porCliente, arbol, destinosCayendo, agenciasDesaparecidas, operadores, rotacion, armadosDesconocidos);
    }

    public static AsignacionExpedicionDto CalcularAsignacion(NodoComparativo nodo) => ResumenArbolComparativoBuilder.CalcularAsignacion(nodo);

    public static List<FilaFrecuenciaDto> ConstruirTablaFrecuencia(NodoComparativo raiz, Comparison<NodoComparativo>? comparadorHijos = null) =>
        ResumenFrecuenciaCalculator.ConstruirTablaFrecuencia(raiz, comparadorHijos);

    public static string FormatoPorcentaje(decimal v) => (v >= 0 ? "+" : "") + v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";

    public static string FormatoDinero(decimal v) => v.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
}