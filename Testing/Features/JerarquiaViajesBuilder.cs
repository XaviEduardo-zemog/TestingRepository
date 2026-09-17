using Testing.Application.GetAllViajes;

namespace Testing.Features;

/// <summary>
/// Construcción del árbol Cliente › Zona › Matriz (o + Tipo de tarifa) (Etapa Clean F,
/// docs/ETAPA_CLEAN_F_PIVOTE_ARBOL.md), extraída de ConsultaViajes.razor. Presentation puro:
/// reutiliza ContribucionViajeProyectada y CamposDerivadosViajes.ObtenerFechaNegocio tal cual --
/// ninguna regla de negocio nueva ni movida a Application. Los niveles (NivelesJerarquia /
/// NivelesJerarquiaConTarifa) siguen viviendo en ConsultaViajes.razor y se reciben como parámetro.
/// </summary>
public static class JerarquiaViajesBuilder
{
    // Replica la construcción del árbol de tablaJerarquia() en viajes_v14.html: cada viaje
    // acumula en la RAÍZ y en TODOS sus ancestros (Cliente, Cliente>Zona, ...), no solo en su
    // nodo hoja — así Raiz.PorMes/Total ya son el gran total de la tabla, sin cálculo aparte.
    // "jerarquia_tarifa" usa un 5º nivel (Tipo de tarifa) -- mismo mecanismo, un arreglo de
    // selectores más largo, pasado por el llamador.
    public static NodoJerarquia Construir(
        IEnumerable<ViajesDto> viajesFiltrados,
        Func<ViajesDto, string?>[] niveles,
        CorteMensual? corte,
        NodoJerarquia? raizAnterior)
    {
        var nuevo = new NodoJerarquia { Id = "", Label = "TOTAL", Nivel = -1 };

        foreach (var viaje in viajesFiltrados)
        {
            var fecha = CamposDerivadosViajes.ObtenerFechaNegocio(viaje);
            if (fecha is null)
                continue;

            var mesClave = fecha.Value.ToString("yyyy-MM");
            Acumular(nuevo, mesClave, viaje, corte);

            var nodo = nuevo;
            foreach (var nivel in niveles)
            {
                var clave = nivel(viaje) is { Length: > 0 } valor ? valor : "(sin dato)";
                nodo = ObtenerOCrearHijo(nodo, clave);
                Acumular(nodo, mesClave, viaje, corte);
            }
        }

        if (raizAnterior is not null)
            PreservarExpansion(nuevo, raizAnterior);

        return nuevo;
    }

    private static NodoJerarquia ObtenerOCrearHijo(NodoJerarquia padre, string clave)
    {
        if (padre.Hijos.TryGetValue(clave, out var existente))
            return existente;

        var nuevoHijo = new NodoJerarquia
        {
            Id = $"{padre.Id}>{clave}",
            Label = clave,
            Nivel = padre.Nivel + 1,
            IsExpanded = padre.Nivel + 1 == 0,
        };
        padre.Hijos[clave] = nuevoHijo;
        return nuevoHijo;
    }

    // Viajes ya NO se redondea por fila (ver C-P0-02): se acumula el valor decimal proyectado tal
    // cual y se redondea una sola vez, al mostrar, en el Razor consumidor.
    private static void Acumular(NodoJerarquia nodo, string mesClave, ViajesDto viaje, CorteMensual? corte)
    {
        var metrica = new MetricasMes(
            ContribucionViajeProyectada.Viajes(viaje, corte),
            ContribucionViajeProyectada.Kms(viaje, corte),
            ContribucionViajeProyectada.Peaje(viaje, corte),
            ContribucionViajeProyectada.Venta(viaje, corte));

        nodo.PorMes[mesClave] = MetricasMes.Sumar(nodo.ObtenerMes(mesClave), metrica);
        nodo.Total = MetricasMes.Sumar(nodo.Total, metrica);
    }

    // Conserva IsExpanded entre reconstrucciones del árbol (nuevos filtros/consulta) para que
    // el usuario no pierda lo que ya expandió — el HTML no lo necesita porque EXPAND vive
    // aparte del árbol; aquí IsExpanded vive en el nodo, así que hay que copiarlo explícitamente
    // del árbol viejo al nuevo, emparejando por clave de nivel.
    private static void PreservarExpansion(NodoJerarquia nuevo, NodoJerarquia anterior)
    {
        foreach (var (clave, hijoNuevo) in nuevo.Hijos)
        {
            if (anterior.Hijos.TryGetValue(clave, out var hijoAnterior))
            {
                hijoNuevo.IsExpanded = hijoAnterior.IsExpanded;
                PreservarExpansion(hijoNuevo, hijoAnterior);
            }
        }
    }
}