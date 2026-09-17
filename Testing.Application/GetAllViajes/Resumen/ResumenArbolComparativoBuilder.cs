namespace Testing.Application.GetAllViajes;

/// <summary>
/// Construcción del árbol comparativo Cliente › Zona › Matriz y cálculo de la asignación
/// Comodato/Full/Sencillo -- extraído de ResumenEjecutivoCalculator en la Etapa Clean H
/// (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md). CalcularAsignacion es internal aquí porque
/// ResumenEjecutivoCalculator.CalcularAsignacion (público, usado por ResumenArbolComparativo.razor,
/// ResumenEjecutivoWordExporter.cs y SlidesPresentacionCalculator.cs) sigue existiendo como wrapper
/// delgado que delega en este método -- ningún consumidor externo cambia su firma.
/// </summary>
internal static class ResumenArbolComparativoBuilder
{
    private static readonly Func<ViajesDto, string?>[] NivelesArbol =
    [
        CamposDerivadosViajes.ObtenerCliente,
        CamposDerivadosViajes.ObtenerZona,
        CamposDerivadosViajes.ObtenerMatriz,
    ];

    // ---------- Bloques 8.4/8.6/8.7 — árbol comparativo compartido (replica RE_arbol) ----------

    internal static NodoComparativo ConstruirArbolComparativo(IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte, Func<ViajesDto, DateTime?> fechaDe)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var ultimo = meses[^1];
        var anterior = meses.Count > 1 ? meses[^2] : (MesCerrado?)null;

        var raiz = new NodoComparativo { Id = "", Label = "TOTAL", Nivel = -1 };

        foreach (var v in viajes)
        {
            var fecha = fechaDe(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            var esIda = CamposDerivadosViajes.ObtenerMovimiento(v) == "Ida";
            var contribucion = TotalesPeriodo.De(v, corte);
            // Asignacion se clasifica desde EjesEquipos en la fuente CIS directa
            // (5=Sencillo, 6=Comodato, 9=Full); "armado" queda solo como fallback historico.
            var armado = CamposDerivadosViajes.ClasificarArmado(v);

            AcumularComparativo(raiz, mes, ultimo, anterior, contribucion, armado, esIda);

            var nodo = raiz;
            foreach (var nivelSelector in NivelesArbol)
            {
                var clave = nivelSelector(v) is { Length: > 0 } valor ? valor : "(sin dato)";
                nodo = ObtenerOCrearHijoComparativo(nodo, clave);
                AcumularComparativo(nodo, mes, ultimo, anterior, contribucion, armado, esIda);
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

    private static void AcumularComparativo(
        NodoComparativo nodo, MesCerrado mes, MesCerrado ultimo, MesCerrado? anterior,
        TotalesPeriodo contribucion, string? armado, bool esIda)
    {
        nodo.Anual = TotalesPeriodo.Sumar(nodo.Anual, contribucion);

        if (mes.Anio == ultimo.Anio && mes.Mes == ultimo.Mes)
        {
            nodo.Ultimo = TotalesPeriodo.Sumar(nodo.Ultimo, contribucion);
            if (esIda && armado is { Length: > 0 })
                nodo.ArmadoUltimo[armado] = nodo.ArmadoUltimo.GetValueOrDefault(armado) + contribucion.Viajes;
            if (armado is { Length: > 0 })
                nodo.ArmadoVentaUltimo[armado] = nodo.ArmadoVentaUltimo.GetValueOrDefault(armado) + contribucion.Venta;
        }
        else if (anterior is not null && mes.Anio == anterior.Value.Anio && mes.Mes == anterior.Value.Mes)
        {
            nodo.Anterior = TotalesPeriodo.Sumar(nodo.Anterior, contribucion);
            if (esIda && armado is { Length: > 0 })
                nodo.ArmadoAnterior[armado] = nodo.ArmadoAnterior.GetValueOrDefault(armado) + contribucion.Viajes;
            if (armado is { Length: > 0 })
                nodo.ArmadoVentaAnterior[armado] = nodo.ArmadoVentaAnterior.GetValueOrDefault(armado) + contribucion.Venta;
        }
    }

    internal static AsignacionExpedicionDto CalcularAsignacion(NodoComparativo nodo)
    {
        var co = nodo.ArmadoUltimo.GetValueOrDefault("Comodato");
        var fu = nodo.ArmadoUltimo.GetValueOrDefault("Full");
        var se = nodo.ArmadoUltimo.GetValueOrDefault("Sencillo");
        var tt = co + fu + se;
        var pc = tt > 0 ? (decimal?)(co / tt * 100) : null;

        var ca = nodo.ArmadoAnterior.GetValueOrDefault("Comodato");
        var fa = nodo.ArmadoAnterior.GetValueOrDefault("Full");
        var sa = nodo.ArmadoAnterior.GetValueOrDefault("Sencillo");
        var ta = ca + fa + sa;
        var pa = ta > 0 ? (decimal?)(ca / ta * 100) : null;

        var vCo = nodo.ArmadoVentaUltimo.GetValueOrDefault("Comodato");
        var vFu = nodo.ArmadoVentaUltimo.GetValueOrDefault("Full");
        var vSe = nodo.ArmadoVentaUltimo.GetValueOrDefault("Sencillo");

        return new AsignacionExpedicionDto(
            co, fu, se, tt,
            PctComodato: pc,
            DeltaPuntosPorcentuales: pc is null || pa is null ? null : pc - pa,
            VentaPorViajeComodato: co > 0 ? vCo / co : 0,
            VentaPorViajeFull: fu > 0 ? vFu / fu : 0,
            VentaPorViajeSencillo: se > 0 ? vSe / se : 0);
    }
}