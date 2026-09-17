namespace Testing.Application.GetAllViajes;

/// <summary>
/// Tabla de frecuencia (aplanado del árbol comparativo con compactación de nodos de un solo hijo)
/// y las alertas que de ahí extrae el semáforo -- extraído de ResumenEjecutivoCalculator en la
/// Etapa Clean H (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md). ConstruirTablaFrecuencia es
/// internal aquí porque ResumenEjecutivoCalculator.ConstruirTablaFrecuencia (público, usado por
/// ResumenArbolComparativo.razor, ResumenEjecutivoWordExporter.cs y los tests) sigue existiendo
/// como wrapper delgado que delega en este método.
/// </summary>
internal static class ResumenFrecuenciaCalculator
{
    internal static List<FilaFrecuenciaDto> ConstruirTablaFrecuencia(NodoComparativo raiz, Comparison<NodoComparativo>? comparadorHijos = null)
    {
        var filas = new List<FilaFrecuenciaDto>();
        const int profundidadMaxima = 3;

        List<NodoComparativo> OrdenarHijos(NodoComparativo nodo, int nivelFila)
        {
            if (nivelFila == 0)
            {
                var porPrioridad = nodo.Hijos.Values.GroupBy(h => CamposDerivadosViajes.PrioridadCliente(h.Label)).OrderBy(g => g.Key);
                var resultado = new List<NodoComparativo>();
                foreach (var grupo in porPrioridad)
                {
                    var grupoOrdenado = grupo.ToList();
                    if (comparadorHijos is null)
                        grupoOrdenado = grupoOrdenado.OrderBy(h => h.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
                    else
                        grupoOrdenado.Sort(comparadorHijos);
                    resultado.AddRange(grupoOrdenado);
                }
                return resultado;
            }

            var hijos = nodo.Hijos.Values.ToList();
            if (comparadorHijos is null)
                hijos = hijos.OrderBy(h => h.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
            else
                hijos.Sort(comparadorHijos);
            return hijos;
        }

        void Caminar(NodoComparativo nodo, int nivelFila)
        {
            foreach (var hijoOriginal in OrdenarHijos(nodo, nivelFila))
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
    internal static List<AlertaFrecuencia> RecolectarAlertasFrecuencia(NodoComparativo raiz) =>
        ConstruirTablaFrecuencia(raiz)
            .Where(f => f.Nivel >= 2 && f.Alerta)
            .Select(f => new AlertaFrecuencia(f.Label, f.DeltaPorcentaje!.Value))
            .OrderBy(f => f.DeltaPorcentaje)
            .ToList();
}