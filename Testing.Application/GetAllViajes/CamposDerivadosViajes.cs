using System.Globalization;

namespace Testing.Application.GetAllViajes;

public static class CamposDerivadosViajes
{
    /// <summary>
    /// Valor de ViajesDto.cis_llave_utilizada que identifica una fila construida por
    /// ViajesDirectosMapper (Fuente B: CIS_DB directo, prototipo paralelo -- ver
    /// docs/PROTOTIPO_FUENTE_CIS.md), a diferencia de "Directo"/"LimpiarFolio"/"Slash" que usa la
    /// Fuente A (SP + matching de Folio). Se define aquí para que ObtenerFechaNegocio pueda usarla
    /// sin crear una dependencia cíclica con ViajesDirectosMapper.
    /// </summary>
    public const string LlaveCisDirecto = "CisDirecto";

    public static string? NormalizarCliente(string? nombreCorto)
    {
        if (string.IsNullOrWhiteSpace(nombreCorto))
            return nombreCorto;

        return nombreCorto.Trim().ToUpperInvariant() switch
        {
            "ME" or "MD" => "Modelo",
            var _ => nombreCorto.Trim(),
        };
    }

    public static int PrioridadCliente(string? cliente) => cliente switch
    {
        "Arca" => 0,
        "Modelo" => 1,
        _ => 2,
    };

    /// <summary>Cliente visual, ya normalizado (ME/MD → "Modelo"). Fuente: Sucursales.NombreCorto, vía el Folio ya resuelto en CIS_DB.</summary>
    public static string? ObtenerCliente(ViajesDto viaje) => NormalizarCliente(viaje.cis_cliente);

    /// <summary>Valor crudo de Sucursales.NombreCorto ("ME"/"MD"/"Arca"), SIN normalizar -- solo para diagnóstico, nunca para agrupar/filtrar/mostrar en la jerarquía.</summary>
    public static string? ObtenerClienteOriginal(ViajesDto viaje) => viaje.cis_cliente;

    /// <summary>Sucursales.Region.</summary>
    public static string? ObtenerZona(ViajesDto viaje) => viaje.cis_zona;

    /// <summary>Sucursales.Nomenclatura. Antes era "v._base" repetido inline en ConsultaViajes.razor, ResumenEjecutivoCalculator.cs y OperadoresRotacionCalculator.cs -- no existía como método.</summary>
    public static string? ObtenerMatriz(ViajesDto viaje) => viaje.cis_matriz;

    public static string? ObtenerSucursal(ViajesDto viaje) => viaje.cis_sucursal;

    /// <summary>ZemogViajesEnZamAnual.Destino. Antes se derivaba parseando "ruta" -- ya no.</summary>
    public static string? ObtenerDestino(ViajesDto viaje) => viaje.cis_destino;

    /// <summary>ZemogViajesEnZamAnual.Origen. No existía como método -- el campo crudo "ruta" nunca se separaba en Origen antes de esta fase.</summary>
    public static string? ObtenerOrigen(ViajesDto viaje) => viaje.cis_origen;

    /// <summary>ZemogViajesEnZamAnual.EstadoOrigen.</summary>
    public static string? ObtenerEstadoOrigen(ViajesDto viaje) => viaje.cis_estado_origen;

    /// <summary>ZemogViajesEnZamAnual.EstadoDestino. Antes el filtro "Edo. Destino" quedaba deshabilitado a propósito -- ya tiene fuente real.</summary>
    public static string? ObtenerEstadoDestino(ViajesDto viaje) => viaje.cis_estado_destino;

    public static string? ObtenerMovimiento(ViajesDto viaje)
    {
        if (string.IsNullOrWhiteSpace(viaje.cis_trayecto))
            return null;

        return viaje.cis_trayecto.Trim().ToUpperInvariant() switch
        {
            "IDA" => "Ida",
            "REGRESO" => "Regreso",
            var otro => otro, // valor no esperado -- se conserva tal cual, NUNCA se normaliza a "Ida"/"Regreso" por accidente
        };
    }

    private static readonly string[] FormatosFecha = ["d/M/yyyy h:mm tt"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    public static DateTime? ObtenerFechaNegocio(ViajesDto viaje)
    {
        // Fuente B (prototipo CIS directo): usa FechaCalendario como fecha de negocio -- decisión
        // de negocio explícita (docs/DISENO_CONSULTA_DIRECTA_CIS.md §3), NO un intento de
        // reproducir fecha_ingreso del SP. Se distingue por cis_llave_utilizada y no por la mera
        // presencia de cis_fecha_calendario, porque ese campo TAMBIÉN viene poblado hoy en la
        // Fuente A (SP + enriquecimiento CIS por Folio) -- ramificar solo por presencia habría
        // cambiado silenciosamente el comportamiento de fecha de TODA la Fuente A existente.
        if (viaje.cis_llave_utilizada == LlaveCisDirecto && viaje.cis_fecha_calendario is { } fechaCalendario)
            return fechaCalendario.ToDateTime(TimeOnly.MinValue);

        // Fuente A (SP): comportamiento sin cambios respecto al existente antes de este prototipo.
        if (string.IsNullOrWhiteSpace(viaje.fecha_ingreso))
            return null;

        if (DateTime.TryParseExact(viaje.fecha_ingreso, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;

        if (DateTime.TryParse(viaje.fecha_ingreso, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;

        return null;
    }

    /// <summary>
    /// Expedición reutilizable para ambas fuentes (Fuente A: SP; Fuente B: CIS directo, ver
    /// docs/PROTOTIPO_FUENTE_CIS.md). Regla: 1) si viaje.expedicion tiene valor tras Trim(),
    /// devolverlo tal cual; 2) si es null/vacío/"-", analizar viaje.ruta con la MISMA posición que
    /// ObtenerTarifa (primer espacio, 2 caracteres siguientes) pero mapeado a Comodato/Propio; 3)
    /// en cualquier otro caso, null -- nunca "Viaje" (a diferencia de ObtenerTarifa, que sí usa
    /// "Viaje" como default). Expedición y Tipo de tarifa son conceptos distintos aunque compartan
    /// la misma fuente de fallback (Ruta) -- no fusionar esta lógica con ObtenerTarifa.
    /// </summary>
    public static string? ObtenerExpedicion(ViajesDto viaje)
    {
        var directo = viaje.expedicion?.Trim();
        if (!string.IsNullOrEmpty(directo) && directo != "-")
            return directo;

        var s = viaje.ruta?.Trim();
        if (string.IsNullOrEmpty(s))
            return null;

        var i = s.IndexOf(' ');
        if (i < 0)
            return null;

        var restante = s.Length - (i + 1);
        var token = restante <= 0 ? "" : s.Substring(i + 1, Math.Min(2, restante));

        return token switch
        {
            "C." => "Comodato",
            "P." => "Propio",
            _ => null,
        };
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