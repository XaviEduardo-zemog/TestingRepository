using System.Globalization;
using System.Text;

namespace Testing.Application.GetAllViajes;

/// <summary>
/// Genera el Word del Resumen Ejecutivo con el mismo truco que viajes_v14.html
/// (RE_exportWord): HTML con namespaces xmlns:o/xmlns:w de Word, servido con mimetype
/// application/msword. 100% servidor: arma el HTML directo desde el ResumenEjecutivoDto ya
/// cargado en el circuito de Blazor Server.
///
/// ALCANCE (documentado, no un olvido): incluye Semáforo, Nivel general, Por Cliente,
/// Asignación (resumen a nivel raíz -- la versión jerárquica completa vive en pantalla,
/// ResumenArbolComparativo.razor), Destinos cayendo (Top 25), Agencias desaparecidas,
/// Operadores y Rotación. NO incluye el árbol comparativo detallado Cliente›Zona›Matriz
/// (Bloque 8.4/8.7) -- fuera de proporción para esta fase, extensión futura acotada.
/// </summary>
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
        EscribirDestinosCayendo(sb, resumen);
        EscribirAgenciasDesaparecidas(sb, resumen);
        EscribirOperadores(sb, resumen);
        EscribirRotacion(sb, resumen);

        sb.Append($"<p style=\"color:#75787E;font-size:9pt;margin-top:24px;\">Generado automáticamente desde Viajes Zemog el {DateTime.Now:dd/MM/yyyy HH:mm}. TotalVenta = subtotal_factura (fuente temporal, ver §54.85).</p>");

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
            sb.Append($"<li>{a.Texto}</li>");
        sb.Append("</ul>");
    }

    private static void EscribirBloqueNivel(StringBuilder sb, string titulo, BloqueNivelDto bloque)
    {
        sb.Append($"<h2 style=\"color:{ColorAcento};font-size:13pt;\">{titulo}</h2>");
        sb.Append(AbrirTabla("Mes", "Viajes", "KM", "Venta", "$/KM"));

        foreach (var (mes, totales) in bloque.Tendencia)
            sb.Append(FilaTabla(mes.Etiqueta, FormatoN0(totales.Viajes), FormatoN0(totales.Kms), FormatoDinero(totales.Venta), FormatoDinero(totales.PorKm)));

        sb.Append(CerrarTabla());

        if (bloque.PeorMesDelAnio is not null)
            sb.Append($"<p style=\"font-size:9pt;\">Peor mes (por venta): <b>{bloque.PeorMesDelAnio.Value.Mes.Etiqueta}</b> ({FormatoDinero(bloque.PeorMesDelAnio.Value.Venta)}). Mejor mes: <b>{bloque.MejorMesDelAnio!.Value.Mes.Etiqueta}</b> ({FormatoDinero(bloque.MejorMesDelAnio.Value.Venta)}).</p>");
    }

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
        sb.Append("<p style=\"font-size:9pt;color:#75787E;\">Total a nivel raíz. El desglose por Cliente/Zona/Matriz/Sucursal está disponible en pantalla.</p>");
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
        if (resumen.AgenciasDesaparecidas.Count == 0)
        {
            sb.Append("<p>Ninguna en el periodo consultado.</p>");
            return;
        }

        sb.Append(AbrirTabla("Destino", "Matriz", "Último mes activo", "Meses activa", "Venta acumulada"));
        foreach (var ag in resumen.AgenciasDesaparecidas)
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

    private static string Celda(string v) => $"<td style=\"padding:5px 8px;border-bottom:1px solid #E6E4DE;font-size:9pt;\">{v}</td>";

    private static string CerrarTabla() => "</tbody></table>";

    private static string FormatoN0(decimal v) => Math.Round(v).ToString("N0", Cultura);
    private static string FormatoDinero(decimal v) => v.ToString("C0", Cultura);
    private static string FormatoPct(decimal? v) => v is null ? "—" : ResumenEjecutivoCalculator.FormatoPorcentaje(v.Value);
}