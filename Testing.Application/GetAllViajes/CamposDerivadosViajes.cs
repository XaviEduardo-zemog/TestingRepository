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

/// <summary>
/// Campos derivados de ViajesDto que no requieren TotalVenta. Vive en Application (no en
/// Presentation) para que ConsultaViajes.razor no tenga Split/regex directo en Razor, y para
/// poder probarlos con pruebas unitarias reales (ver Testing.Application.Tests).
/// </summary>
public static class CamposDerivadosViajes
{
    private static readonly Regex PatronCodigoEntreParentesis = new(
        @"^(?<texto>.+?)\s*\((?<codigo>[^()]*)\)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PatronCodigoInicial = new(
        @"^(?<codigo>\S+)\s+(?<resto>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static RutaParseada ParsearRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return RutaParseada.NoReconocida;

        var s = ruta.Trim();
        var movimiento = ObtenerMovimiento(s);

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

    public static string? ObtenerMovimiento(ViajesDto viaje) => ObtenerMovimiento(viaje.ruta);

    private static string? ObtenerMovimiento(string? ruta)
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
}