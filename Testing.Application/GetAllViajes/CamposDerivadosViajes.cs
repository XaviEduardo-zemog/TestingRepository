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
    // Formato confirmado por el usuario (chat, 2026-08-28) sobre datos reales:
    //   "<código> Origen (CodOrigen) - Destino (CodDestino) - I|R"
    //   Ejemplo ida:     800059 Victor Rosales (CCZ) - Torreon (DCMNorte) - I
    //   Ejemplo regreso: 059800 Torreon (DCMNorte) - Victor Rosales (CCZ) - R
    // Es distinto del formato que tarifaDeRuta() del HTML asume (prefijo "C."/"P." tras el
    // primer espacio) — por eso NO se implementa Tarifa en esta fase (ver ObtenerTarifa).
    private static readonly Regex PatronRuta = new(
        @"^(?<codigo>\S+)\s+(?<origen>.+?)\s*\((?<codorigen>[^()]*)\)\s*-\s*(?<destino>.+?)\s*\((?<coddestino>[^()]*)\)\s*-\s*(?<mov>[IR])\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Parsea "ruta" con el formato confirmado. Heurística: si el texto no calza con el
    /// patrón exacto, devuelve RutaParseada.NoReconocida en vez de adivinar — no se ha
    /// validado este patrón contra el universo completo de valores reales de Ruta.
    /// </summary>
    public static RutaParseada ParsearRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return RutaParseada.NoReconocida;

        var match = PatronRuta.Match(ruta.Trim());
        if (!match.Success)
            return RutaParseada.NoReconocida;

        var movimiento = match.Groups["mov"].Value.ToUpperInvariant() switch
        {
            "I" => "Ida",
            "R" => "Regreso",
            _ => null,
        };

        return new RutaParseada(
            Reconocida: true,
            CodigoRuta: match.Groups["codigo"].Value,
            Origen: NuloSiVacio(match.Groups["origen"].Value),
            CodigoOrigen: NuloSiVacio(match.Groups["codorigen"].Value),
            Destino: NuloSiVacio(match.Groups["destino"].Value),
            CodigoDestino: NuloSiVacio(match.Groups["coddestino"].Value),
            Movimiento: movimiento);
    }

    public static string? ObtenerDestino(ViajesDto viaje) => ParsearRuta(viaje.ruta).Destino;

    public static string? ObtenerMovimiento(ViajesDto viaje) => ParsearRuta(viaje.ruta).Movimiento;

    /// <summary>
    /// El código entre paréntesis junto al destino (ej. "DCMNorte"). Deliberadamente NO se
    /// llama "EstadoDestino" — esa equivalencia no está confirmada (Fase 4 punto 4).
    /// </summary>
    public static string? ObtenerCodigoDestino(ViajesDto viaje) => ParsearRuta(viaje.ruta).CodigoDestino;

    private static string? NuloSiVacio(string valor)
    {
        var recortado = valor.Trim();
        return recortado.Length > 0 ? recortado : null;
    }

    /// <summary>
    /// Deriva Cliente/Zona de "tipo_operacion" (regla confirmada por el usuario, chat
    /// 2026-08-28, con solo 2-3 valores de ejemplo vistos: "Arca", "Modelo metro", "Modelo
    /// occidente"): primer token = Cliente, resto = Zona. tipo_operacion CONFIRMADO por el
    /// usuario (2026-08-31) como ya devuelto por sp_ConsultaViajesZemog y agregado a
    /// ViajesDto — ver Fase 4 punto 1 / §54.68. El listado completo de valores reales de
    /// tipo_operacion sigue sin confirmarse — validar contra datos reales antes de confiar
    /// al 100% en producción.
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
    /// Fecha de negocio usada para Año/Mes/Semana/Día: fecha_ingreso, la misma columna que ya
    /// usaba ConsultaViajes.razor antes de esta fase (heredado del código original, no
    /// re-confirmado contra el SP). El SP recibe @tipo_fecha = NULL siempre hoy
    /// (ConsultaViajesFilterModel.TipoFecha nunca se expone en la UI desde la Fase 1) — no se
    /// sabe con certeza qué columna de fecha usa el SP internamente cuando tipo_fecha es NULL,
    /// ni si coincide con fecha_ingreso. Riesgo documentado, no resuelto — ver Fase 4 punto 6.
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
    /// NO implementado a propósito: tarifaDeRuta() del HTML original asume un formato de Ruta
    /// con prefijo "C."/"P." tras el primer espacio, que NO coincide con el formato real
    /// confirmado (ver PatronRuta arriba, y §54.20 del Artifact). Se deja como stub
    /// documentado — no se omite en silencio — hasta validar contra más datos reales. No
    /// confundir con "expedicion", que es un campo real y distinto ya disponible en
    /// ViajesDto.expedicion.
    /// </summary>
    public static string? ObtenerTarifa(ViajesDto viaje) => null;
}