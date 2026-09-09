using System.Globalization;
using System.Text.RegularExpressions;

namespace Testing.Application.GetAllViajes;

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

public static class CamposDerivadosViajes
{
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

    public static string? ObtenerDestino(ViajesDto viaje) =>
        viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado && viaje.cis_destino is { Length: > 0 }
            ? viaje.cis_destino
            : ParsearRuta(viaje.ruta).Destino;

    public static string? ObtenerEstadoDestino(ViajesDto viaje) =>
        viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado ? viaje.cis_estado_destino : null;

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

    public static string? ObtenerCodigoDestino(ViajesDto viaje) => ParsearRuta(viaje.ruta).CodigoDestino;

    private static string? NuloSiVacio(string valor)
    {
        var recortado = valor.Trim();
        return recortado.Length > 0 ? recortado : null;
    }

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

    public static string? NormalizarCliente(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var recortado = valor.Trim();

        return recortado.ToUpperInvariant() switch
        {
            "MD" or "ME" or "MODELO" => "Modelo",
            "ARCA" => "Arca",
            _ => recortado,
        };
    }

    public static string? ObtenerCliente(ViajesDto viaje)
    {
        var crudo = viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado && viaje.cis_cliente is { Length: > 0 }
            ? viaje.cis_cliente
            : ParsearClienteZona(viaje.tipo_operacion).Cliente;

        return NormalizarCliente(crudo);
    }

    public static string? ObtenerZona(ViajesDto viaje) =>
        viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado && viaje.cis_zona is { Length: > 0 }
            ? viaje.cis_zona
            : null;

    public static string? ObtenerMatriz(ViajesDto viaje) =>
        viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado && viaje.cis_matriz is { Length: > 0 }
            ? viaje.cis_matriz
            : viaje._base;

    public static string? ObtenerSucursal(ViajesDto viaje) =>
        viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado && viaje.cis_sucursal is { Length: > 0 }
            ? viaje.cis_sucursal
            : viaje._base;

    public static bool EsFallbackCis(ViajesDto viaje) => viaje.cis_estado is EstadoEnriquecimientoCis.NoEncontrado or EstadoEnriquecimientoCis.Duplicado;

    private static readonly string[] FormatosFecha = ["d/M/yyyy h:mm tt"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

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