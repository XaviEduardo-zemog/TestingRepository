using Testing.Application.GetAllViajes;

namespace Testing.Features;

/// <summary>
/// Definición de un filtro de la consulta de viajes (Etapa Clean E,
/// docs/ETAPA_CLEAN_E_FILTROS_CASCADA.md). Antes era una clase privada anidada en
/// ConsultaViajes.razor -- mismo shape exacto, ahora pública en Testing.Features.
/// </summary>
public sealed class FiltroZemog
{
    public required string Clave { get; init; }
    public required string Etiqueta { get; init; }
    public required Func<ViajesDto, string?> Selector { get; init; }
    public bool Disponible { get; init; } = true;
    public string? MotivoPendiente { get; init; }
    public bool ConBuscador { get; init; }
    public HashSet<string> Seleccionados { get; set; } = [];
    public List<string> OpcionesDisponibles { get; set; } = [];
}

/// <summary>
/// Estado y cascada de los 15 filtros de ConsultaViajes.razor, extraído en la Etapa Clean D
/// (docs/ETAPA_CLEAN_E_FILTROS_CASCADA.md). Presentation puro: solo agrega/filtra sobre
/// ViajesDto usando los selectores ya existentes de CamposDerivadosViajes -- ninguna regla de
/// negocio nueva ni movida a Application. viajesCargados se recibe como parámetro en cada
/// método en vez de guardarse como campo: la carga de datos sigue siendo responsabilidad de
/// ConsultaViajes.razor, fuera de alcance de esta etapa.
/// </summary>
public sealed class FiltrosZemogState
{
    public List<FiltroZemog> Filtros { get; } = ConstruirFiltros();

    private static List<FiltroZemog> ConstruirFiltros() =>
    [
        new() { Clave = "cliente", Etiqueta = "Cliente", Selector = CamposDerivadosViajes.ObtenerCliente, ConBuscador = true },
        new() { Clave = "sucursal", Etiqueta = "Sucursal", Selector = CamposDerivadosViajes.ObtenerSucursal },
        new() { Clave = "mes", Etiqueta = "Mes", Selector = CamposDerivadosViajes.ObtenerMesEtiqueta },
        new() { Clave = "destino", Etiqueta = "Destino", Selector = CamposDerivadosViajes.ObtenerDestino, ConBuscador = true },
        new() { Clave = "expedicion", Etiqueta = "Expedición", Selector = CamposDerivadosViajes.ObtenerExpedicion },
        new() { Clave = "zona", Etiqueta = "Zona", Selector = CamposDerivadosViajes.ObtenerZona, ConBuscador = true },
        new() { Clave = "matriz", Etiqueta = "Matriz", Selector = CamposDerivadosViajes.ObtenerMatriz },
        new() { Clave = "anio", Etiqueta = "Año", Selector = CamposDerivadosViajes.ObtenerAnio },
        new() { Clave = "semana", Etiqueta = "Semana", Selector = CamposDerivadosViajes.ObtenerSemana },
        new() { Clave = "edestino", Etiqueta = "Estado Destino", Selector = CamposDerivadosViajes.ObtenerEstadoDestino },
        new() { Clave = "operador", Etiqueta = "Operador", Selector = v => v.operador1, ConBuscador = true },
        new() { Clave = "unidad", Etiqueta = "Unidad", Selector = v => v.id_unidad, ConBuscador = true },
        new() { Clave = "ejes", Etiqueta = "Ejes", Selector = CamposDerivadosViajes.ObtenerEjes },
        new() { Clave = "tarifa", Etiqueta = "Tipo de tarifa", Selector = CamposDerivadosViajes.ObtenerTarifa },
        new() { Clave = "movimiento", Etiqueta = "Movimiento", Selector = CamposDerivadosViajes.ObtenerMovimiento },
    ];

    // TEMPORAL MVP: cascada calculada en memoria sobre viajesCargados (el resultado
    // ya traído por GetViajesQuery). FINAL: si el volumen de datos crece, mover el
    // cálculo de opciones disponibles a Application/SQL (ver §54.27).
    public void RecalcularCascada(List<ViajesDto> viajesCargados, string? claveModificada)
    {
        if (claveModificada is null)
        {
            foreach (var f in Filtros.Where(f => f.Disponible))
            {
                f.OpcionesDisponibles = CalcularOpciones(viajesCargados, f);
                f.Seleccionados.IntersectWith(f.OpcionesDisponibles);
            }
            return;
        }

        var pasoElCambio = false;
        var filtrado = viajesCargados;

        foreach (var f in Filtros)
        {
            if (f.Disponible && f.Seleccionados.Count > 0)
                filtrado = filtrado.Where(v => f.Selector(v) is { } val && f.Seleccionados.Contains(val)).ToList();

            if (f.Clave == claveModificada)
                pasoElCambio = true;

            if (pasoElCambio && f.Clave != claveModificada && f.Disponible)
            {
                f.OpcionesDisponibles = CalcularOpciones(filtrado, f);
                f.Seleccionados.IntersectWith(f.OpcionesDisponibles);
            }
        }
    }

    private static List<string> CalcularOpciones(IEnumerable<ViajesDto> datos, FiltroZemog f)
    {
        var valores = datos.Select(f.Selector)
             .Where(v => !string.IsNullOrWhiteSpace(v))
             .Select(v => v!)
             .Distinct();

        return f.Clave == "cliente"
            ? valores.OrderBy(CamposDerivadosViajes.PrioridadCliente).ThenBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList()
            : valores.OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public List<ViajesDto> ObtenerViajesFiltrados(List<ViajesDto> viajesCargados)
    {
        IEnumerable<ViajesDto> resultado = viajesCargados;

        foreach (var f in Filtros.Where(f => f.Disponible && f.Seleccionados.Count > 0))
            resultado = resultado.Where(v => f.Selector(v) is { } val && f.Seleccionados.Contains(val));

        return resultado.ToList();
    }

    public void LimpiarTodo()
    {
        foreach (var f in Filtros)
            f.Seleccionados.Clear();
    }
}