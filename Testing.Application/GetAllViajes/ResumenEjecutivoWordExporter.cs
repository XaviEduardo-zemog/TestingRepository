using System.Globalization;
using System.Net;
using System.Text;

namespace Testing.Application.GetAllViajes;

public static class ResumenEjecutivoWordExporter
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    private const string ColorAcento = "#8A6604";
    private const string ColorHeaderBg = "#16181C";
    private const string ColorHeaderTxt = "#FFFFFF";
    private const string ColorTotalBg = "#F1EFE9";

    private const int ProfundidadMaximaArbol = 3; // Cliente > Zona > Matriz -- Sucursal ya no es nivel del árbol.

    public static string Generar(ResumenEjecutivoDto resumen)
    {
        var sb = new StringBuilder();

        sb.Append("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.Append("<head><meta charset=\"utf-8\"><title>Resumen Ejecutivo Zemog</title></head><body style=\"font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#191B1E;\">");

        sb.Append($"<h1 style=\"color:{ColorAcento};font-size:18pt;\">Resumen Ejecutivo — Zemog</h1>");
        sb.Append($"<p style=\"color:#5E6167;font-size:10pt;\">{WebUtility.HtmlEncode(Subtitulo(resumen))}</p>");

        EscribirSemaforo(sb, resumen);
        if (resumen.NivelZemog is not null)
            EscribirBloqueNivel(sb, "Zemog · Nivel general", resumen.NivelZemog);
        foreach (var c in resumen.PorCliente)
            EscribirBloqueNivel(sb, $"Por Cliente — {c.Cliente}", c.Bloque);
        if (resumen.ArbolComparativo is not null)
        {
            EscribirArbolComparativo(sb, resumen.ArbolComparativo);
            EscribirAsignacion(sb, resumen.ArbolComparativo);
            EscribirFrecuencia(sb, resumen.ArbolComparativo);
        }
        EscribirDestinosCayendo(sb, resumen);
        EscribirAgenciasDesaparecidas(sb, resumen);
        EscribirOperadores(sb, resumen);
        EscribirRotacion(sb, resumen);

        sb.Append($"<p style=\"color:#75787E;font-size:9pt;margin-top:24px;\">Generado automáticamente desde Viajes Zemog el {DateTime.Now:dd/MM/yyyy HH:mm}. Venta = CIS.TotalVenta.</p>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Subtitulo(ResumenEjecutivoDto resumen)
    {
        if (resumen.MesesCerrados.Count == 0)
            return "Sin datos disponibles";

        return resumen.MesesCerrados.Count == 1
            ? $"Mes: {resumen.MesesCerrados[0].Etiqueta}"
            : $"Meses: {resumen.MesesCerrados[0].Etiqueta} – {resumen.MesesCerrados[^1].Etiqueta}";
    }

    private static void EscribirSemaforo(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Lo más importante</h2>");
        if (resumen.Semaforo.Count == 0)
        {
            sb.Append("<p>Sin datos.</p>");
            return;
        }

        sb.Append("<ul>");
        foreach (var a in resumen.Semaforo)
            sb.Append($"<li>{WebUtility.HtmlEncode(a.Texto)}</li>");
        sb.Append("</ul>");
    }

    private static void EscribirBloqueNivel(StringBuilder sb, string titulo, BloqueNivelDto bloque)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">{WebUtility.HtmlEncode(titulo)}</h2>");
        sb.Append(AbrirTabla("Mes", "Viajes", "KM", "Venta", "$/KM"));

        foreach (var (mes, totales) in bloque.Tendencia)
            sb.Append(FilaTabla(mes.Etiqueta, FormatoN0(totales.Viajes), FormatoN0(totales.Kms), FormatoDinero(totales.Venta), FormatoDinero(totales.PorKm)));

        sb.Append(CerrarTabla());

        if (bloque.PeorMesDelAnio is not null)
            sb.Append($"<p style=\"font-size:9pt;\">Peor mes (por venta): <b>{bloque.PeorMesDelAnio.Value.Mes.Etiqueta}</b> ({FormatoDinero(bloque.PeorMesDelAnio.Value.Venta)}). Mejor mes: <b>{bloque.MejorMesDelAnio!.Value.Mes.Etiqueta}</b> ({FormatoDinero(bloque.MejorMesDelAnio.Value.Venta)}).</p>");
    }

    private static void EscribirArbolComparativo(StringBuilder sb, NodoComparativo raiz)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Por Cliente › Zona › Matriz</h2>");
        sb.Append(AbrirTabla("Nivel", "Viajes", "%", "KM", "%", "Venta", "%", "Viajes año", "KM año", "Venta año"));

        foreach (var fila in AplanarComparativo(raiz))
        {
            var deltaViajes = Delta(fila.Nodo.Anterior.Viajes, fila.Nodo.Ultimo.Viajes);
            var deltaKm = Delta(fila.Nodo.Anterior.Kms, fila.Nodo.Ultimo.Kms);
            var deltaVenta = Delta(fila.Nodo.Anterior.Venta, fila.Nodo.Ultimo.Venta);

            sb.Append(FilaTabla(
                Sangria(fila.Nivel) + fila.Label,
                FormatoN0(fila.Nodo.Ultimo.Viajes), FormatoPct(deltaViajes),
                FormatoN0(fila.Nodo.Ultimo.Kms), FormatoPct(deltaKm),
                FormatoDinero(fila.Nodo.Ultimo.Venta), FormatoPct(deltaVenta),
                FormatoN0(fila.Nodo.Anual.Viajes), FormatoN0(fila.Nodo.Anual.Kms), FormatoDinero(fila.Nodo.Anual.Venta)));
        }

        var deltaViajesTot = Delta(raiz.Anterior.Viajes, raiz.Ultimo.Viajes);
        var deltaKmTot = Delta(raiz.Anterior.Kms, raiz.Ultimo.Kms);
        var deltaVentaTot = Delta(raiz.Anterior.Venta, raiz.Ultimo.Venta);
        sb.Append($"<tr style=\"background:{ColorTotalBg};font-weight:bold;\">");
        sb.Append(
            Celda("TOTAL") + Celda(FormatoN0(raiz.Ultimo.Viajes)) + Celda(FormatoPct(deltaViajesTot)) +
            Celda(FormatoN0(raiz.Ultimo.Kms)) + Celda(FormatoPct(deltaKmTot)) +
            Celda(FormatoDinero(raiz.Ultimo.Venta)) + Celda(FormatoPct(deltaVentaTot)) +
            Celda(FormatoN0(raiz.Anual.Viajes)) + Celda(FormatoN0(raiz.Anual.Kms)) + Celda(FormatoDinero(raiz.Anual.Venta)));
        sb.Append("</tr>");
        sb.Append(CerrarTabla());
    }

    private sealed record FilaArbolWord(int Nivel, string Label, NodoComparativo Nodo);

    private static List<FilaArbolWord> AplanarComparativo(NodoComparativo raiz)
    {
        var filas = new List<FilaArbolWord>();

        void Caminar(NodoComparativo nodo, int nivelFila)
        {
            foreach (var hijoOriginal in nodo.Hijos.Values.OrderBy(h => h.Label, StringComparer.CurrentCultureIgnoreCase))
            {
                var efectivo = hijoOriginal;
                var nivelEfectivo = nivelFila;

                while (nivelEfectivo < ProfundidadMaximaArbol - 1 && efectivo.Hijos.Count == 1)
                {
                    efectivo = efectivo.Hijos.Values.Single();
                    nivelEfectivo++;
                }

                filas.Add(new FilaArbolWord(nivelFila, hijoOriginal.Label, efectivo));

                var esHoja = nivelEfectivo == ProfundidadMaximaArbol - 1 || efectivo.Hijos.Count == 0;
                if (!esHoja)
                    Caminar(efectivo, nivelFila + 1);
            }
        }

        Caminar(raiz, 0);
        return filas;
    }

    private static string Sangria(int nivel) => string.Concat(Enumerable.Repeat("— ", nivel));

    private static void EscribirAsignacion(StringBuilder sb, NodoComparativo raiz)
    {
        var a = ResumenEjecutivoCalculator.CalcularAsignacion(raiz);
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Asignación Comodato / Full / Sencillo (total, último mes)</h2>");
        sb.Append(AbrirTabla("Expedición", "Viajes", "% del total", "$/viaje"));
        sb.Append(FilaTabla("Comodato", FormatoN0(a.Comodato), FormatoPct(a.PctComodato), FormatoDinero(a.VentaPorViajeComodato)));
        sb.Append(FilaTabla("Full", FormatoN0(a.Full), "", FormatoDinero(a.VentaPorViajeFull)));
        sb.Append(FilaTabla("Sencillo", FormatoN0(a.Sencillo), "", FormatoDinero(a.VentaPorViajeSencillo)));
        sb.Append(FilaTabla("Total", FormatoN0(a.Total), a.DeltaPuntosPorcentuales is null ? "" : $"Δ {FormatoPct(a.DeltaPuntosPorcentuales)} pp vs mes anterior", ""));
        sb.Append(CerrarTabla());
        // Nota: sigue siendo solo el total a nivel raíz -- el desglose de Asignación por
        // Cliente/Zona/Matriz (la otra tabla de ResumenArbolComparativo.razor) queda fuera de
        // esta etapa; Comodato en sí sigue PENDIENTE DE FUENTE DE NEGOCIO (P0/P1, sin cambio aquí).
        sb.Append("<p style=\"font-size:9pt;color:#75787E;\">Total a nivel raíz. El desglose por Cliente/Zona/Matriz está disponible en pantalla. Comodato sigue pendiente de fuente de negocio confirmada.</p>");
    }

    private static void EscribirFrecuencia(StringBuilder sb, NodoComparativo raiz)
    {
        var filas = ResumenEjecutivoCalculator.ConstruirTablaFrecuencia(raiz);

        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Frecuencia por agencia</h2>");
        if (filas.Count == 0)
        {
            sb.Append("<p>Sin datos.</p>");
            return;
        }

        sb.Append(AbrirTabla("Nivel", "Viajes anterior", "Viajes último", "Δ Viajes", "%", "Venta último", "Señal"));
        foreach (var f in filas)
        {
            sb.Append(FilaTabla(
                Sangria(f.Nivel) + f.Label,
                FormatoN0(f.ViajesAnterior), FormatoN0(f.ViajesUltimo),
                FormatoDeltaAbs(f.ViajesUltimo - f.ViajesAnterior), FormatoPct(f.DeltaPorcentaje),
                FormatoDinero(f.VentaUltimo), f.Alerta ? "⚠ Revisar" : ""));
        }

        var deltaTot = Delta(raiz.Anterior.Viajes, raiz.Ultimo.Viajes);
        sb.Append($"<tr style=\"background:{ColorTotalBg};font-weight:bold;\">");
        sb.Append(
            Celda("TOTAL") + Celda(FormatoN0(raiz.Anterior.Viajes)) + Celda(FormatoN0(raiz.Ultimo.Viajes)) +
            Celda(FormatoDeltaAbs(raiz.Ultimo.Viajes - raiz.Anterior.Viajes)) + Celda(FormatoPct(deltaTot)) +
            Celda(FormatoDinero(raiz.Ultimo.Venta)) + Celda(""));
        sb.Append("</tr>");
        sb.Append(CerrarTabla());
        sb.Append("<p style=\"font-size:9pt;color:#75787E;\">Alerta: Δ Viajes ≤ -15% con base del mes anterior ≥ 20 viajes (misma regla que el semáforo).</p>");
    }

    private static void EscribirDestinosCayendo(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Destinos que estamos dejando de dar</h2>");
        if (resumen.DestinosCayendo is not { TotalConCaida: > 0 } destinos)
        {
            sb.Append("<p>Ningún destino con caída de venta en el periodo consultado.</p>");
            return;
        }

        sb.Append($"<p style=\"font-size:9pt;\">{destinos.TotalConCaida} destino(s) con caída · impacto total {FormatoDinero(destinos.ImpactoTotal)} · se muestran las {destinos.Top25.Count} mayores caídas</p>");
        sb.Append(AbrirTabla("Destino", "Matriz", "Venta anterior", "Venta actual", "Δ Venta", "Se dejó de dar"));
        foreach (var d in destinos.Top25)
            sb.Append(FilaTabla(d.Destino, d.Matriz, FormatoDinero(d.VentaAnterior), FormatoDinero(d.VentaActual), FormatoDinero(d.DeltaVenta), d.SeDejoDeDar ? "Sí" : "—"));
        sb.Append(CerrarTabla());
    }

    private static void EscribirAgenciasDesaparecidas(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Agencias que ya no aparecen</h2>");
        if (resumen.AgenciasDesaparecidas.TotalDesaparecidas == 0)
        {
            sb.Append("<p>Ninguna en el periodo consultado.</p>");
            return;
        }

        sb.Append($"<p style=\"font-size:9pt;\">{resumen.AgenciasDesaparecidas.TotalDesaparecidas} agencia(s)/destino(s) · venta acumulada {FormatoDinero(resumen.AgenciasDesaparecidas.VentaAcumuladaTotal)} · se muestran las {resumen.AgenciasDesaparecidas.Top30.Count} de mayor venta</p>");
        sb.Append(AbrirTabla("Destino", "Matriz", "Último mes activo", "Meses activa", "Venta acumulada"));
        foreach (var ag in resumen.AgenciasDesaparecidas.Top30)
            sb.Append(FilaTabla(ag.Destino, ag.Matriz, ag.UltimoMesActivo.Etiqueta, FormatoN0(ag.MesesActiva), FormatoDinero(ag.VentaAcumulada)));
        sb.Append(CerrarTabla());
    }

    private static void EscribirOperadores(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Operadores (todos los meses)</h2>");

        var filas = new List<(string Sucursal, string Operador, decimal Viajes, decimal Kms, decimal Venta)>();
        foreach (var (sucursal, porOperador) in resumen.Operadores.PorSucursalOperadorMes)
        {
            foreach (var (operador, porMes) in porOperador)
            {
                var viajes = porMes.Values.Sum(t => t.Viajes);
                var kms = porMes.Values.Sum(t => t.Kms);
                var venta = porMes.Values.Sum(t => t.Venta);
                if (viajes > 0 || kms > 0 || venta > 0)
                    filas.Add((sucursal, operador, viajes, kms, venta));
            }
        }

        if (filas.Count == 0)
        {
            sb.Append("<p>Sin operadores con viajes en el periodo.</p>");
            return;
        }

        sb.Append(AbrirTabla("Sucursal", "Operador", "Viajes", "KM", "Venta", "$/KM"));
        foreach (var f in filas.OrderByDescending(f => f.Venta))
            sb.Append(FilaTabla(f.Sucursal, f.Operador, FormatoN0(f.Viajes), FormatoN0(f.Kms), FormatoDinero(f.Venta), FormatoDinero(f.Kms > 0 ? f.Venta / f.Kms : 0)));
        sb.Append(CerrarTabla());
    }

    private static void EscribirRotacion(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Rotación de operadores</h2>");
        if (resumen.Rotacion.PorSucursal.Count == 0)
        {
            sb.Append("<p>Sin datos suficientes (se necesitan al menos 2 meses).</p>");
            return;
        }

        sb.Append(AbrirTabla("Sucursal", "Activos", "Altas", "Bajas", "Δ% Viajes", "Lectura", "Venta de bajas"));
        foreach (var f in resumen.Rotacion.PorSucursal)
            sb.Append(FilaTabla(f.Sucursal, FormatoN0(f.Activos), FormatoN0(f.Altas), FormatoN0(f.Bajas), FormatoPct(f.DeltaViajesPorcentaje), f.Lectura, FormatoDinero(f.VentaBajas)));

        var t = resumen.Rotacion.Total;
        sb.Append($"<tr style=\"background:{ColorTotalBg};font-weight:bold;\">");
        sb.Append(Celda(t.Sucursal) + Celda(FormatoN0(t.Activos)) + Celda(FormatoN0(t.Altas)) + Celda(FormatoN0(t.Bajas)) + Celda(FormatoPct(t.DeltaViajesPorcentaje)) + Celda(t.Lectura) + Celda(FormatoDinero(t.VentaBajas)));
        sb.Append("</tr>");
        sb.Append(CerrarTabla());
    }

    private static string AbrirTabla(params string[] encabezados)
    {
        var sb = new StringBuilder("<table style=\"border-collapse:collapse;width:100%;margin-bottom:14px;\"><thead><tr>");
        foreach (var h in encabezados)
            sb.Append($"<th style=\"background:{ColorHeaderBg};color:{ColorHeaderTxt};padding:5px 8px;text-align:left;font-size:9pt;\">{h}</th>");
        sb.Append("</tr></thead><tbody>");
        return sb.ToString();
    }

    private static string FilaTabla(params string[] valores)
    {
        var sb = new StringBuilder("<tr>");
        foreach (var v in valores)
            sb.Append(Celda(v));
        sb.Append("</tr>");
        return sb.ToString();
    }

    private static string Celda(string v) => $"<td style=\"padding:5px 8px;border-bottom:1px solid #E6E4DE;font-size:9pt;\">{WebUtility.HtmlEncode(v)}</td>";

    private static string CerrarTabla() => "</tbody></table>";

    private static decimal? Delta(decimal anterior, decimal ultimo) => anterior > 0 ? (ultimo - anterior) / anterior * 100 : null;

    private static string FormatoDeltaAbs(decimal delta) => (delta >= 0 ? "+" : "") + FormatoN0(delta);

    private static string FormatoN0(decimal v) => Math.Round(v).ToString("N0", Cultura);
    private static string FormatoDinero(decimal v) => v.ToString("C0", Cultura);
    private static string FormatoPct(decimal? v) => v is null ? "—" : ResumenEjecutivoCalculator.FormatoPorcentaje(v.Value);
}