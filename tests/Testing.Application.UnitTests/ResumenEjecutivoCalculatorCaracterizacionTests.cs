using Testing.Application.GetAllViajes;

namespace Testing.Application.UnitTests;

/// <summary>
/// Tests de caracterización (Etapa Clean G, docs/ETAPA_CLEAN_G_TESTS_RESUMEN.md): congelan el
/// comportamiento ACTUAL de los bloques de ResumenEjecutivoCalculator que la auditoría marcó sin
/// cobertura suficiente (CalcularDestinosCayendo, CalcularAgenciasDesaparecidas, CalcularSemaforo,
/// ConstruirTablaFrecuencia, CalcularArmadosDesconocidos), para que la Etapa H pueda dividir la
/// clase con menor riesgo. No imponen una lógica "mejor": si un texto aquí parece raro (por
/// ejemplo, "1 destinos" en singular), es exactamente lo que el código produce hoy -- no se
/// corrige en este archivo.
/// </summary>
public sealed class ResumenEjecutivoCalculatorCaracterizacionTests
{
    private static string Fecha(int dia, int mes, int anio) => $"{dia}/{mes}/{anio} 12:00 AM";

    // ---------- 1. CalcularDestinosCayendo ----------

    [Fact]
    public void CalcularDestinosCayendo_detecta_caida_de_venta_y_ordena_por_mayor_perdida()
    {
        var viajes = new List<ViajesDto>
        {
            // CDMX/M1: venta baja de 1000 a 600 -> cae, pero sigue con viajes (no "se dejó de dar").
            new() { cis_destino = "CDMX", cis_matriz = "M1", cis_trayecto = "Ida", cis_total_venta = 1000m, fecha_ingreso = Fecha(15, 7, 2026) },
            new() { cis_destino = "CDMX", cis_matriz = "M1", cis_trayecto = "Ida", cis_total_venta = 600m, fecha_ingreso = Fecha(15, 8, 2026) },

            // Monterrey/M2: venta SUBE de 500 a 800 -> no debe aparecer como caída.
            new() { cis_destino = "Monterrey", cis_matriz = "M2", cis_trayecto = "Ida", cis_total_venta = 500m, fecha_ingreso = Fecha(15, 7, 2026) },
            new() { cis_destino = "Monterrey", cis_matriz = "M2", cis_trayecto = "Ida", cis_total_venta = 800m, fecha_ingreso = Fecha(15, 8, 2026) },

            // Puebla/M3: venta 700 en julio y NINGÚN viaje en agosto -> "se dejó de dar", mayor caída absoluta.
            new() { cis_destino = "Puebla", cis_matriz = "M3", cis_trayecto = "Ida", cis_total_venta = 700m, fecha_ingreso = Fecha(15, 7, 2026) },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);

        Assert.NotNull(resumen.DestinosCayendo);
        var destinos = resumen.DestinosCayendo!;

        Assert.Equal(2, destinos.TotalConCaida);
        Assert.Equal(-1100m, destinos.ImpactoTotal);
        Assert.Equal(2, destinos.Top25.Count);

        // Orden actual: ascendente por DeltaVenta -> el más negativo (mayor pérdida) primero.
        Assert.Equal("Puebla", destinos.Top25[0].Destino);
        Assert.Equal(-700m, destinos.Top25[0].DeltaVenta);
        Assert.True(destinos.Top25[0].SeDejoDeDar);

        Assert.Equal("CDMX", destinos.Top25[1].Destino);
        Assert.Equal(-400m, destinos.Top25[1].DeltaVenta);
        Assert.False(destinos.Top25[1].SeDejoDeDar);

        Assert.DoesNotContain(destinos.Top25, d => d.Destino == "Monterrey");
    }

    // ---------- 2. CalcularAgenciasDesaparecidas ----------

    [Fact]
    public void CalcularAgenciasDesaparecidas_detecta_agencia_sin_actividad_en_el_ultimo_mes_y_ordena_por_venta()
    {
        var viajes = new List<ViajesDto>
        {
            // Toluca/M5: activa en Jun y Jul, pero NO en Ago (último mes) -> desaparecida.
            new() { cis_destino = "Toluca", cis_matriz = "M5", cis_trayecto = "Ida", cis_total_venta = 100m, fecha_ingreso = Fecha(10, 6, 2026) },
            new() { cis_destino = "Toluca", cis_matriz = "M5", cis_trayecto = "Ida", cis_total_venta = 150m, fecha_ingreso = Fecha(10, 7, 2026) },

            // Puebla/M4: solo activa en Jun -> también desaparecida, con menor venta acumulada.
            new() { cis_destino = "Puebla", cis_matriz = "M4", cis_trayecto = "Ida", cis_total_venta = 50m, fecha_ingreso = Fecha(10, 6, 2026) },

            // Monterrey/M2: activa en Jun, Jul y Ago (último mes) -> NO desaparecida.
            new() { cis_destino = "Monterrey", cis_matriz = "M2", cis_trayecto = "Ida", cis_total_venta = 200m, fecha_ingreso = Fecha(10, 6, 2026) },
            new() { cis_destino = "Monterrey", cis_matriz = "M2", cis_trayecto = "Ida", cis_total_venta = 250m, fecha_ingreso = Fecha(10, 7, 2026) },
            new() { cis_destino = "Monterrey", cis_matriz = "M2", cis_trayecto = "Ida", cis_total_venta = 300m, fecha_ingreso = Fecha(10, 8, 2026) },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);
        var agencias = resumen.AgenciasDesaparecidas;

        Assert.Equal(2, agencias.TotalDesaparecidas);
        Assert.Equal(300m, agencias.VentaAcumuladaTotal);
        Assert.Equal(2, agencias.Top30.Count);

        // Orden actual: descendente por VentaAcumulada.
        var toluca = agencias.Top30[0];
        Assert.Equal("Toluca", toluca.Destino);
        Assert.Equal("M5", toluca.Matriz);
        Assert.Equal(2, toluca.MesesActiva);
        Assert.Equal(250m, toluca.VentaAcumulada);
        Assert.Equal(125m, toluca.VentaPromedio);
        Assert.Equal(7, toluca.UltimoMesActivo.Mes);
        Assert.Equal(2026, toluca.UltimoMesActivo.Anio);
        Assert.Equal(1m, toluca.ViajesEnEseMes);

        var puebla = agencias.Top30[1];
        Assert.Equal("Puebla", puebla.Destino);
        Assert.Equal(1, puebla.MesesActiva);
        Assert.Equal(50m, puebla.VentaAcumulada);

        Assert.DoesNotContain(agencias.Top30, a => a.Destino == "Monterrey");
    }

    // ---------- 3. CalcularSemaforo ----------

    [Fact]
    public void CalcularSemaforo_genera_alertas_positiva_negativa_y_neutral_segun_datos()
    {
        var viajes = new List<ViajesDto>
        {
            // CDMX/MTY (Arca/Norte): venta sube de 1000 a 1500 -> contribuye a un total Zemog/cliente positivo.
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_destino = "CDMX", cis_trayecto = "Ida", cis_total_venta = 1000m, fecha_ingreso = Fecha(15, 7, 2026) },
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_destino = "CDMX", cis_trayecto = "Ida", cis_total_venta = 1500m, fecha_ingreso = Fecha(15, 8, 2026) },

            // Toluca/MTY (mismo cliente/zona/matriz): venta cae de 500 a 100 -> dispara la alerta de
            // Destinos Cayendo, pero el total combinado (1500 -> 1600) sigue siendo positivo.
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_destino = "Toluca", cis_trayecto = "Ida", cis_total_venta = 500m, fecha_ingreso = Fecha(15, 7, 2026) },
            new() { cis_cliente = "Arca", cis_zona = "Norte", cis_matriz = "MTY", cis_destino = "Toluca", cis_trayecto = "Ida", cis_total_venta = 100m, fecha_ingreso = Fecha(15, 8, 2026) },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);
        var alertas = resumen.Semaforo;

        // Se congela la cantidad Y el orden exactos que produce hoy CalcularSemaforo para este
        // dataset: peor mes (neutral), venta Zemog (positiva), venta del cliente Arca (positiva),
        // destinos cayendo (negativa), operadores (neutral). Sin alerta de frecuencia (la base de
        // viajes a nivel matriz es de solo 2, muy por debajo del umbral de 20) ni de agencias
        // desaparecidas (ambos destinos siguen activos en el último mes).
        Assert.Equal(5, alertas.Count);

        Assert.Equal(SeveridadAlerta.Neutral, alertas[0].Severidad);
        Assert.Contains("Peor mes a nivel Zemog", alertas[0].Texto);
        Assert.Contains("Jul 2026", alertas[0].Texto);

        Assert.Equal(SeveridadAlerta.Positiva, alertas[1].Severidad);
        Assert.Contains("Venta de Ago 2026 vs Jul 2026", alertas[1].Texto);

        Assert.Equal(SeveridadAlerta.Positiva, alertas[2].Severidad);
        Assert.Contains("Arca:", alertas[2].Texto);

        Assert.Equal(SeveridadAlerta.Negativa, alertas[3].Severidad);
        Assert.Contains("1 destinos con caída de venta", alertas[3].Texto);
        Assert.Contains("Toluca", alertas[3].Texto);

        Assert.Equal(SeveridadAlerta.Neutral, alertas[4].Severidad);
        Assert.Contains("Operadores:", alertas[4].Texto);
    }

    // ---------- 4. ConstruirTablaFrecuencia ----------

    [Fact]
    public void ConstruirTablaFrecuencia_respeta_compactacion_de_nodos_y_marca_alerta_por_caida_de_viajes()
    {
        // Árbol construido a mano (en vez de vía ViajesDto) para fijar exactamente la forma del
        // árbol y así congelar, sin ambigüedad, la regla de compactación de ConstruirTablaFrecuencia
        // (un ancestro con un único hijo "se salta": la fila usa la etiqueta del ancestro pero los
        // números del descendiente efectivo).
        var raiz = new NodoComparativo { Id = "", Label = "TOTAL", Nivel = -1 };
        var arca = new NodoComparativo { Id = ">Arca", Label = "Arca", Nivel = 0 };
        var norte = new NodoComparativo { Id = ">Arca>Norte", Label = "Norte", Nivel = 1 };
        var sur = new NodoComparativo { Id = ">Arca>Sur", Label = "Sur", Nivel = 1 };
        var mty = new NodoComparativo { Id = ">Arca>Norte>MTY", Label = "MTY", Nivel = 2 };
        var slp = new NodoComparativo { Id = ">Arca>Norte>SLP", Label = "SLP", Nivel = 2 };
        var gdl = new NodoComparativo { Id = ">Arca>Sur>GDL", Label = "GDL", Nivel = 2 };

        raiz.Hijos["Arca"] = arca;
        arca.Hijos["Norte"] = norte;
        arca.Hijos["Sur"] = sur;
        norte.Hijos["MTY"] = mty;
        norte.Hijos["SLP"] = slp;
        sur.Hijos["GDL"] = gdl;

        // MTY: base >= 20 viajes y caída >= 15% -> debe marcar Alerta = true.
        mty.Anterior = new TotalesPeriodo(30, 300, 30000m);
        mty.Ultimo = new TotalesPeriodo(20, 200, 20000m);

        // SLP: sube -> sin alerta.
        slp.Anterior = new TotalesPeriodo(10, 100, 10000m);
        slp.Ultimo = new TotalesPeriodo(12, 120, 12000m);

        // GDL: base menor a 20 viajes -> sin alerta aunque hubiera caída (aquí se queda plano).
        gdl.Anterior = new TotalesPeriodo(5, 50, 5000m);
        gdl.Ultimo = new TotalesPeriodo(5, 50, 5000m);

        var filas = ResumenEjecutivoCalculator.ConstruirTablaFrecuencia(raiz);

        Assert.Equal(5, filas.Count);
        Assert.DoesNotContain(filas, f => f.Label == "TOTAL"); // la raíz nunca genera fila propia

        Assert.Equal(0, filas[0].Nivel);
        Assert.Equal("Arca", filas[0].Label);

        Assert.Equal(1, filas[1].Nivel);
        Assert.Equal("Norte", filas[1].Label);

        var filaMty = filas[2];
        Assert.Equal(2, filaMty.Nivel);
        Assert.Equal("MTY", filaMty.Label);
        Assert.Equal(30m, filaMty.ViajesAnterior);
        Assert.Equal(20m, filaMty.ViajesUltimo);
        var deltaEsperadoMty = (20m - 30m) / 30m * 100m;
        Assert.Equal(deltaEsperadoMty, filaMty.DeltaPorcentaje);
        Assert.True(filaMty.Alerta);
        Assert.Equal(20000m, filaMty.VentaUltimo);

        var filaSlp = filas[3];
        Assert.Equal(2, filaSlp.Nivel);
        Assert.Equal("SLP", filaSlp.Label);
        Assert.False(filaSlp.Alerta);

        // Sur: compactado -- la fila queda etiquetada "Sur" (nivel 1, el nivel de iteración real)
        // pero con los NÚMEROS de GDL (el único hijo efectivo tras la compactación).
        var filaSur = filas[4];
        Assert.Equal(1, filaSur.Nivel);
        Assert.Equal("Sur", filaSur.Label);
        Assert.Equal(5m, filaSur.ViajesAnterior);
        Assert.Equal(5m, filaSur.ViajesUltimo);
        Assert.False(filaSur.Alerta);

        // Mismo filtro que usa CalcularSemaforo/RecolectarAlertasFrecuencia internamente
        // (nivel >= 2 && Alerta) -- aquí solo MTY cumple.
        var alertasDeFrecuencia = filas.Where(f => f.Nivel >= 2 && f.Alerta).ToList();
        Assert.Single(alertasDeFrecuencia);
        Assert.Equal("MTY", alertasDeFrecuencia[0].Label);
    }

    // ---------- 5. CalcularArmadosDesconocidos ----------

    [Fact]
    public void CalcularArmadosDesconocidos_excluye_conocidos_y_sin_dato_pero_incluye_valores_no_reconocidos()
    {
        var viajes = new List<ViajesDto>
        {
            // Conocidos (5/6/9) -- nunca deben aparecer como desconocidos.
            new() { cis_trayecto = "Ida", cis_ejes_equipos = 5, fecha_ingreso = Fecha(1, 7, 2026) }, // Sencillo
            new() { cis_trayecto = "Ida", cis_ejes_equipos = 6, fecha_ingreso = Fecha(1, 7, 2026) }, // Comodato
            new() { cis_trayecto = "Ida", cis_ejes_equipos = 9, fecha_ingreso = Fecha(1, 7, 2026) }, // Full

            // Desconocido numérico (7) repetido dos veces -> debe aparecer con conteo 2.
            new() { cis_trayecto = "Ida", cis_ejes_equipos = 7, fecha_ingreso = Fecha(1, 7, 2026) },
            new() { cis_trayecto = "Ida", cis_ejes_equipos = 7, fecha_ingreso = Fecha(1, 7, 2026) },

            // Desconocido por texto en "armado" (fallback, solo se usa si cis_ejes_equipos es null).
            new() { cis_trayecto = "Ida", cis_ejes_equipos = null, armado = "RARO", fecha_ingreso = Fecha(1, 7, 2026) },

            // Sin dato (ni ejes ni armado) -- NO debe contarse como desconocido.
            new() { cis_trayecto = "Ida", cis_ejes_equipos = null, armado = null, fecha_ingreso = Fecha(1, 7, 2026) },

            // Regreso con eje "desconocido" -- excluido porque solo se cuentan tramos Ida.
            new() { cis_trayecto = "Regreso", cis_ejes_equipos = 8, fecha_ingreso = Fecha(1, 7, 2026) },
        };

        var resumen = ResumenEjecutivoCalculator.Calcular(viajes, corte: null);
        var desconocidos = resumen.ArmadosDesconocidos;

        Assert.Equal(2, desconocidos.Count);

        // Orden actual: descendente por conteo de viajes.
        Assert.Equal("7", desconocidos[0].Valor);
        Assert.Equal(2m, desconocidos[0].Viajes);

        Assert.Equal("RARO", desconocidos[1].Valor);
        Assert.Equal(1m, desconocidos[1].Viajes);

        Assert.DoesNotContain(desconocidos, d => d.Valor is "5" or "6" or "9" or "8");
    }
}