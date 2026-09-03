using System.Globalization;
using System.Text.RegularExpressions;

namespace Testing.Application.GetAllViajes;

/// <summary>
/// Resultado de parsear el campo "ruta". CodigoDestino se deja separado de "EstadoDestino"
/// a propósito: no se ha confirmado que sean el mismo concepto — ver §54.20 del Artifact
/// y Fase 4 punto 4.
/// </summary>
public sealed record RutaParseada(
    bool Reconocida,
    string? CodigoRuta,
    string? Origen,
    string? CodigoOrigen,
    string? Destino,
    string? CodigoDestino,
    string? Movimiento)
{
    public static readonly RutaParseada NoReconocida = new(false, null, null, null, null, null, null);
}

/// <summary>
/// Campos derivados de ViajesDto que no requieren TotalVenta. Vive en Application (no en
/// Presentation) para que ConsultaViajes.razor no tenga Split/regex directo en Razor, y para
/// poder probarlos con pruebas unitarias reales (ver Testing.Application.Tests).
/// </summary>
public static class CamposDerivadosViajes
{
    // AUDITORÍA de Destino (esta fase, "ajuste fino"): se detectaron 2 formatos reales distintos
    // de "ruta", confirmados por el usuario en dos ocasiones separadas:
    //   Con paréntesis (Fase 4, chat 2026-08-28):
    //     "800059 Victor Rosales (CCZ) - Torreon (DCMNorte) - I"
    //   Sin paréntesis, con prefijo de tarifa (ejemplos de Tarifa de esta fase):
    //     "33083301 C. Chihuahua - Juarez Chh. - I"
    // AMBOS comparten la misma estructura de 3 partes separadas por " - ":
    //   <código[+prefijo tarifa opcional] Origen> - <Destino[+código opcional]> - <I|R>
    // Se parsea dividiendo por " - " (en vez del regex monolítico anterior, que exigía
    // paréntesis en AMBOS lados y fallaba con el formato sin paréntesis) -- el Destino es
    // siempre el segmento del MEDIO, con o sin "(codigo)" al final. Esto reemplaza el parser
    // anterior (que solo reconocía el formato con paréntesis) por uno tolerante a ambos formatos
    // conocidos -- sigue sin validarse contra el universo COMPLETO de valores reales de "ruta"
    // (solo se conocen 4 ejemplos, 2 por formato), ver auditoría de Destino en el Artifact.
    private static readonly Regex PatronCodigoEntreParentesis = new(
        @"^(?<texto>.+?)\s*\((?<codigo>[^()]*)\)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PatronCodigoInicial = new(
        @"^(?<codigo>\S+)\s+(?<resto>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Parsea "ruta" dividiendo por " - " en 3 partes (origen, destino, dirección) -- tolerante a
    /// ambos formatos confirmados (con o sin paréntesis de código junto a origen/destino). Si el
    /// texto no tiene al menos 3 partes separadas por " - ", devuelve RutaParseada.NoReconocida
    /// para Origen/Destino (Movimiento se resuelve aparte, ver ObtenerMovimiento -- no depende de
    /// este parseo).
    /// </summary>
    public static RutaParseada ParsearRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return RutaParseada.NoReconocida;

        var s = ruta.Trim();
        var movimiento = ObtenerMovimientoDesdeRuta(s);

        var partes = s.Split(" - ", StringSplitOptions.None);
        if (partes.Length < 3)
            return RutaParseada.NoReconocida with { Movimiento = movimiento }; // Origen/Destino no se pudieron extraer, pero Movimiento (si se reconoció) se conserva -- no depende de este parseo.

        // Si hay más de 3 partes (un nombre de lugar que por casualidad contuviera " - "), se
        // toma la primera como origen, la última como dirección, y todo lo de en medio se une de
        // vuelta como destino -- heurística conservadora, no se ha visto un caso así en los
        // ejemplos confirmados.
        var origenCrudo = partes[0];
        var destinoCrudo = string.Join(" - ", partes.Skip(1).Take(partes.Length - 2)).Trim();

        var codigoInicial = PatronCodigoInicial.Match(origenCrudo);
        var codigoRuta = codigoInicial.Success ? codigoInicial.Groups["codigo"].Value : null;
        var origenSinCodigo = codigoInicial.Success ? codigoInicial.Groups["resto"].Value : origenCrudo;

        var (origen, codigoOrigen) = SepararCodigoEntreParentesis(origenSinCodigo);
        var (destino, codigoDestino) = SepararCodigoEntreParentesis(destinoCrudo);

        return new RutaParseada(true, codigoRuta, origen, codigoOrigen, destino, codigoDestino, movimiento);
    }

    private static (string? Texto, string? Codigo) SepararCodigoEntreParentesis(string valor)
    {
        var match = PatronCodigoEntreParentesis.Match(valor);
        return match.Success
            ? (NuloSiVacio(match.Groups["texto"].Value), NuloSiVacio(match.Groups["codigo"].Value))
            : (NuloSiVacio(valor), null);
    }

    public static string? ObtenerDestino(ViajesDto viaje) => ParsearRuta(viaje.ruta).Destino;

    public static string? ObtenerMovimiento(ViajesDto viaje)
    {
        if (string.IsNullOrWhiteSpace(viaje.direccion))
            return null;

        return viaje.direccion.Trim().ToUpperInvariant() switch
        {
            "IDA" => "Ida",
            "REGRESO" => "Regreso",
            var otro => otro, // "TRAMO" u otro valor no esperado -- se conserva tal cual, NUNCA se normaliza a "Ida"/"Regreso" por accidente
        };
    }

    private static string? ObtenerMovimientoDesdeRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return null;

        var s = ruta.TrimEnd();
        if (s.EndsWith("- I", StringComparison.OrdinalIgnoreCase) || s.EndsWith("-I", StringComparison.OrdinalIgnoreCase))
            return "Ida";
        if (s.EndsWith("- R", StringComparison.OrdinalIgnoreCase) || s.EndsWith("-R", StringComparison.OrdinalIgnoreCase))
            return "Regreso";

        return null;
    }

    /// <summary>
    /// El código entre paréntesis junto al destino (ej. "DCMNorte"), si lo hay -- null si el
    /// formato de esta fila no trae paréntesis. Deliberadamente NO se llama "EstadoDestino" —
    /// esa equivalencia no está confirmada (Fase 4 punto 4).
    /// </summary>
    public static string? ObtenerCodigoDestino(ViajesDto viaje) => ParsearRuta(viaje.ruta).CodigoDestino;

    private static string? NuloSiVacio(string valor)
    {
        var recortado = valor.Trim();
        return recortado.Length > 0 ? recortado : null;
    }

    /// <summary>
    /// Deriva Cliente/Zona de "tipo_operacion" (regla confirmada por el usuario, chat
    /// 2026-08-28): primer token = Cliente, resto = Zona. tipo_operacion confirmado como ya
    /// devuelto por sp_ConsultaViajesZemog — ver Fase 4 punto 1 / §54.68.
    /// </summary>
    public static (string? Cliente, string? Zona) ParsearClienteZona(string? tipoOperacion)
    {
        if (string.IsNullOrWhiteSpace(tipoOperacion))
            return (null, null);

        var partes = tipoOperacion.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        return partes.Length switch
        {
            0 => (null, null),
            1 => (partes[0], null),
            _ => (partes[0], NuloSiVacio(partes[1])),
        };
    }

    public static string? ObtenerCliente(ViajesDto viaje) => ParsearClienteZona(viaje.tipo_operacion).Cliente;

    public static string? ObtenerZona(ViajesDto viaje) => ParsearClienteZona(viaje.tipo_operacion).Zona;

    private static readonly string[] FormatosFecha = ["d/M/yyyy h:mm tt"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    /// <summary>
    /// Fecha de negocio usada para Año/Mes/Semana/Día: fecha_ingreso. Riesgo documentado, no
    /// resuelto — ver Fase 4 punto 6.
    /// </summary>
    public static DateTime? ObtenerFechaNegocio(ViajesDto viaje)
    {
        if (string.IsNullOrWhiteSpace(viaje.fecha_ingreso))
            return null;

        if (DateTime.TryParseExact(viaje.fecha_ingreso, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;

        if (DateTime.TryParse(viaje.fecha_ingreso, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;

        return null;
    }

    public static string? ObtenerAnio(ViajesDto viaje) => ObtenerFechaNegocio(viaje)?.Year.ToString();

    public static string? ObtenerMesClave(ViajesDto viaje) => ObtenerFechaNegocio(viaje)?.ToString("yyyy-MM");

    public static string? ObtenerMesEtiqueta(ViajesDto viaje)
    {
        var fecha = ObtenerFechaNegocio(viaje);
        return fecha is null ? null : Cultura.TextInfo.ToTitleCase(fecha.Value.ToString("MMM yyyy", Cultura));
    }

    public static string? ObtenerSemana(ViajesDto viaje)
    {
        var fecha = ObtenerFechaNegocio(viaje);
        return fecha is null ? null : ISOWeek.GetWeekOfYear(fecha.Value).ToString();
    }

    /// <summary>
    /// Tipo de tarifa desde "ruta" — replica tarifaDeRuta() de viajes_v14.html: recorta espacios,
    /// vacío -> "(sin tarifa)"; busca el PRIMER espacio, sin espacio -> "Viaje"; toma exactamente
    /// los 2 caracteres inmediatamente después de ese espacio y compara sensible a mayúsculas
    /// contra "C." (Comodato) o "P." (Propio); cualquier otro valor -> "Viaje". Regla de
    /// POSICIÓN, no de búsqueda de letras en cualquier parte de la cadena.
    /// </summary>
    public static string ObtenerTarifa(ViajesDto viaje)
    {
        var s = viaje.ruta?.Trim();
        if (string.IsNullOrEmpty(s))
            return "(sin tarifa)";

        var i = s.IndexOf(' ');
        if (i < 0)
            return "Viaje";

        var restante = s.Length - (i + 1);
        var token = restante <= 0 ? "" : s.Substring(i + 1, Math.Min(2, restante));

        return token switch
        {
            "C." => "Comodato",
            "P." => "Propio",
            _ => "Viaje",
        };
    }

    public static string? ClasificarArmado(ViajesDto viaje)
    {
        if (string.IsNullOrWhiteSpace(viaje.armado))
            return null;

        return viaje.armado.Trim().ToUpperInvariant() switch
        {
            "FULL" => "Full",
            "SENCILLO" => "Sencillo",
            _ => null,
        };
    }

    public static string? NormalizarArmadoCrudo(ViajesDto viaje) =>
        string.IsNullOrWhiteSpace(viaje.armado) ? null : viaje.armado.Trim().ToUpperInvariant();
}