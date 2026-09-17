using Testing.Application.GetAllViajes;

namespace Testing.Features;

/// <summary>
/// Construcción de la tabla pivote plana (Etapa Clean F, docs/ETAPA_CLEAN_F_PIVOTE_ARBOL.md),
/// extraída de ConsultaViajes.razor. Presentation puro: reutiliza ContribucionViajeProyectada
/// (Ida=1/Regreso=0 + factor de corte) y los selectores de CamposDerivadosViajes ya existentes --
/// ninguna regla de negocio nueva ni movida a Application. agruparPor y la función de agrupación
/// (ObtenerValorAgrupacion) siguen viviendo en ConsultaViajes.razor y se reciben como parámetros.
/// </summary>
public static class PivoteViajesBuilder
{
    // TEMPORAL MVP: se arma en memoria sobre viajesFiltrados (el resultado ya traído por
    // GetViajesQuery, ya reducido por los filtros). FINAL: si el volumen real lo justifica, mover
    // esta agregación a SQL/Application (agrupar por dimensión y mes directo en la query) —
    // ver §54.22-54.23/§54.27.
    public static List<FilaPivote> Construir(
        IEnumerable<ViajesDto> viajesFiltrados,
        string agruparPor,
        Func<ViajesDto, string?> obtenerValorAgrupacion,
        CorteMensual? corte)
    {
        var esTemporal = agruparPor is "dia" or "semana";

        var grupos = viajesFiltrados
            .Select(v => (Viaje: v, Clave: obtenerValorAgrupacion(v)))
            .Where(x => x.Clave is not null)
            .GroupBy(x => x.Clave!, x => x.Viaje)
            .ToList();

        var filas = new List<FilaPivote>();

        foreach (var grupo in grupos)
        {
            var fila = new FilaPivote { Dimension = grupo.Key };

            if (esTemporal)
            {
                fila.Total = CalcularMetricas(grupo, corte);
            }
            else
            {
                foreach (var porMes in grupo.GroupBy(CamposDerivadosViajes.ObtenerMesClave))
                {
                    if (porMes.Key is null)
                        continue;

                    fila.PorMes[porMes.Key] = CalcularMetricas(porMes, corte);
                }

                fila.Total = fila.PorMes.Values.Aggregate(MetricasMes.Vacio, MetricasMes.Sumar);
            }

            filas.Add(fila);
        }

        return filas;
    }

    public static List<MesColumna> CalcularMeses(IEnumerable<ViajesDto> viajesFiltrados) =>
        viajesFiltrados
            .Select(v => (Clave: CamposDerivadosViajes.ObtenerMesClave(v), Etiqueta: CamposDerivadosViajes.ObtenerMesEtiqueta(v)))
            .Where(t => t.Clave is not null)
            .DistinctBy(t => t.Clave)
            .OrderBy(t => t.Clave)
            .Select(t => new MesColumna(t.Clave!, t.Etiqueta!))
            .ToList();

    private static MetricasMes CalcularMetricas(IEnumerable<ViajesDto> grupo, CorteMensual? corte)
    {
        var lista = grupo as IReadOnlyCollection<ViajesDto> ?? grupo.ToList();
        var viajes = lista.Sum(v => ContribucionViajeProyectada.Viajes(v, corte));
        var kms = lista.Sum(v => ContribucionViajeProyectada.Kms(v, corte));
        var peaje = lista.Sum(v => ContribucionViajeProyectada.Peaje(v, corte));
        var venta = lista.Sum(v => ContribucionViajeProyectada.Venta(v, corte));
        return new MetricasMes(viajes, kms, peaje, venta);
    }
}