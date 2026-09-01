using System.Globalization;
using System.Text;

namespace Testing.Application.GetAllViajes;

public static class ResumenEjecutivoWordExporter
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    private const string ColorAcento = "#8A6604";
    private const string ColorHeaderBg = "#16181C";
    private const string ColorHeaderTxt = "#FFFFFF";
    private const string ColorTotalBg = "#F1EFE9";

    public static string Generar(ResumenEjecutivoDto resumen)
    {
        var sb = new StringBuilder();

        sb.Append("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.Append("<head><meta charset=\"utf-8\"><title>Resumen Ejecutivo Zemog</title></head><body style=\"font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#191B1E;\">");

        sb.Append($"<h1 style=\"color:{ColorAcento};font-size:18pt;\">Resumen Ejecutivo — Zemog</h1>");
        sb.Append($"<p style=\"color:#5E6167;font-size:10pt;\">{Subtitulo(resumen)}</p>");

        EscribirSemaforo(sb, resumen);
        if (resumen.NivelZemog is not null)
            EscribirBloqueNivel(sb, "Zemog · Nivel general", resumen.NivelZemog);
        foreach (var c in resumen.PorCliente)
            EscribirBloqueNivel(sb, $"Por Cliente — {c.Cliente}", c.Bloque);
        if (resumen.ArbolComparativo is not null)
            EscribirAsignacion(sb, resumen.ArbolComparativo);
        EscribirAgenciasDesaparecidas(sb, resumen);
        EscribirOperadores(sb, resumen);
        EscribirRotacion(sb, resumen);

        sb.Append($"<p style=\"color:#75787E;font-size:9pt;margin-top:24px;\">Generado automáticamente desde Viajes Zemog el {DateTime.Now:dd/MM/yyyy HH:mm}. Las líneas marcadas \"Pendiente\" requieren una fuente de Venta que todavía no se ha confirmado (ver Fase 5).</p>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Subtitulo(ResumenEjecutivoDto resumen)
    {
        if (resumen.MesesCerrados.Count == 0)
            return "Sin meses cerrados disponibles";

        var rango = resumen.MesesCerrados.Count == 1
            ? resumen.MesesCerrados[0].Etiqueta
            : $"{resumen.MesesCerrados[0].Etiqueta} – {resumen.MesesCerrados[^1].Etiqueta}";

        var nota = resumen.MesAbiertoExcluido ? $" · {resumen.EtiquetaMesAbiertoExcluido} excluido (mes en curso, sin cerrar)" : "";
        return $"Meses cerrados: {rango}{nota}";
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
            sb.Append($"<li>{a.Texto}</li>");
        sb.Append("</ul>");
    }

    private static void EscribirBloqueNivel(StringBuilder sb, string titulo, BloqueNivelDto bloque)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">{titulo}</h2>");
        sb.Append(AbrirTabla("Mes", "Viajes", "KM", "Δ% Viajes", "Δ% KM"));

        foreach (var (mes, totales) in bloque.Tendencia)
        {
            var esUltimo = mes.Anio == bloque.MesUltimo.Anio && mes.Mes == bloque.MesUltimo.Mes;
            var deltaV = esUltimo ? FormatoPct(bloque.DeltaViajesPctVsAnterior) : "";
            var deltaK = esUltimo ? FormatoPct(bloque.DeltaKmsPctVsAnterior) : "";
            sb.Append(FilaTabla(mes.Etiqueta, FormatoN0(totales.Viajes), FormatoN0(totales.Kms), deltaV, deltaK));
        }

        sb.Append(CerrarTabla());
    }

    private static void EscribirAsignacion(StringBuilder sb, NodoComparativo raiz)
    {
        var a = ResumenEjecutivoCalculator.CalcularAsignacion(raiz);
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Asignación Comodato / Full / Sencillo (último mes)</h2>");
        sb.Append(AbrirTabla("Expedición", "Viajes", "% del total"));
        sb.Append(FilaTabla("Comodato", FormatoN0(a.Comodato), FormatoPct(a.PctComodato)));
        sb.Append(FilaTabla("Full", FormatoN0(a.Full), ""));
        sb.Append(FilaTabla("Sencillo", FormatoN0(a.Sencillo), ""));
        sb.Append(FilaTabla("Total", FormatoN0(a.Total), a.DeltaPuntosPorcentuales is null ? "" : $"Δ {FormatoPct(a.DeltaPuntosPorcentuales)} pp vs mes anterior"));
        sb.Append(CerrarTabla());
    }

    private static void EscribirAgenciasDesaparecidas(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Agencias que ya no aparecen</h2>");
        if (resumen.AgenciasDesaparecidas.Count == 0)
        {
            sb.Append("<p>Ninguna en el periodo consultado.</p>");
            return;
        }

        sb.Append(AbrirTabla("Destino", "Matriz", "Último mes activo", "Meses activa"));
        foreach (var ag in resumen.AgenciasDesaparecidas)
            sb.Append(FilaTabla(ag.Destino, ag.Matriz, ag.UltimoMesActivo.Etiqueta, FormatoN0(ag.MesesActiva)));
        sb.Append(CerrarTabla());
    }

    private static void EscribirOperadores(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Operadores (todos los meses cerrados)</h2>");

        var filas = new List<(string Sucursal, string Operador, int Viajes, decimal Kms)>();
        foreach (var (sucursal, porOperador) in resumen.Operadores.PorSucursalOperadorMes)
        {
            foreach (var (operador, porMes) in porOperador)
            {
                var viajes = porMes.Values.Sum(t => t.Viajes);
                var kms = porMes.Values.Sum(t => t.Kms);
                if (viajes > 0 || kms > 0)
                    filas.Add((sucursal, operador, viajes, kms));
            }
        }

        if (filas.Count == 0)
        {
            sb.Append("<p>Sin operadores con viajes en el periodo.</p>");
            return;
        }

        sb.Append(AbrirTabla("Sucursal", "Operador", "Viajes", "KM", "KM/Viaje"));
        foreach (var f in filas.OrderByDescending(f => f.Viajes))
            sb.Append(FilaTabla(f.Sucursal, f.Operador, FormatoN0(f.Viajes), FormatoN0(f.Kms), FormatoN0(f.Viajes > 0 ? f.Kms / f.Viajes : 0)));
        sb.Append(CerrarTabla());
        sb.Append("<p style=\"font-size:9pt;color:#75787E;\">Venta y $/KM: Pendiente — " + ResumenEjecutivoCalculator.PendienteVenta + "</p>");
    }

    private static void EscribirRotacion(StringBuilder sb, ResumenEjecutivoDto resumen)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">Rotación de operadores</h2>");
        if (resumen.Rotacion.PorSucursal.Count == 0)
        {
            sb.Append("<p>Sin datos suficientes (se necesitan al menos 2 meses cerrados).</p>");
            return;
        }

        sb.Append(AbrirTabla("Sucursal", "Activos", "Altas", "Bajas", "Δ% Viajes", "Lectura"));
        foreach (var f in resumen.Rotacion.PorSucursal)
            sb.Append(FilaTabla(f.Sucursal, FormatoN0(f.Activos), FormatoN0(f.Altas), FormatoN0(f.Bajas), FormatoPct(f.DeltaViajesPorcentaje), f.Lectura));

        var t = resumen.Rotacion.Total;
        sb.Append($"<tr style=\"background:{ColorTotalBg};font-weight:bold;\">");
        sb.Append(Celda(t.Sucursal) + Celda(FormatoN0(t.Activos)) + Celda(FormatoN0(t.Altas)) + Celda(FormatoN0(t.Bajas)) + Celda(FormatoPct(t.DeltaViajesPorcentaje)) + Celda(t.Lectura));
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

    private static string Celda(string v) => $"<td style=\"padding:5px 8px;border-bottom:1px solid #E6E4DE;font-size:9pt;\">{v}</td>";

    private static string CerrarTabla() => "</tbody></table>";

    private static string FormatoN0(decimal v) => Math.Round(v).ToString("N0", Cultura);
    private static string FormatoN0(int v) => v.ToString("N0", Cultura);
    private static string FormatoPct(decimal? v) => v is null ? "—" : ResumenEjecutivoCalculator.FormatoPorcentaje(v.Value);
}