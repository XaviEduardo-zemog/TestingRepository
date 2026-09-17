using Testing.Application.GetAllViajes;
using Testing.Components.Shared;

namespace Testing.Features;

public sealed record SegmentoEjes(string Etiqueta, decimal Viajes, decimal Porcentaje, string Clase, string Tooltip);

public static class SegmentosEjesCalculator
{
    private static readonly string[] OrdenEjes = ["Sencillo", "Comodato", "Full", "Sin clasificar"];

    private static readonly Dictionary<string, string> ClasesEjes = new()
    {
        ["Sencillo"] = "vz-ejes-sencillo",
        ["Comodato"] = "vz-ejes-comodato",
        ["Full"] = "vz-ejes-full",
        ["Sin clasificar"] = "vz-ejes-sinclasificar",
    };

    public static List<SegmentoEjes> Calcular(IEnumerable<ViajesDto> viajesFiltrados, CorteMensual? corte)
    {
        var acumulado = new Dictionary<string, decimal>();

        foreach (var v in viajesFiltrados)
        {
            var contribucion = ContribucionViajeProyectada.Viajes(v, corte);
            if (contribucion == 0)
                continue; // tramos de Regreso (u otros sin contribución analítica) no aportan a la barra

            var etiqueta = CamposDerivadosViajes.ClasificarArmado(v) ?? "Sin clasificar";
            acumulado[etiqueta] = acumulado.GetValueOrDefault(etiqueta) + contribucion;
        }

        var total = acumulado.Values.Sum();
        if (total <= 0)
            return []; // sin viajes analíticos clasificables -- no se muestra la barra

        return OrdenEjes
            .Where(acumulado.ContainsKey)
            .Select(etiqueta =>
            {
                var viajes = acumulado[etiqueta];
                var porcentaje = viajes / total * 100;
                var tooltip = $"{etiqueta}: {FormatoUi.Numero(viajes)} viajes ({FormatoUi.Porcentaje(porcentaje)})";
                return new SegmentoEjes(etiqueta, viajes, porcentaje, ClasesEjes[etiqueta], tooltip);
            })
            .ToList();
    }

    public static string TextoAccesible(IReadOnlyList<SegmentoEjes> segmentos) =>
        "Distribución de viajes por Ejes/Asignación: " + string.Join(" · ", segmentos.Select(s => s.Tooltip));
}