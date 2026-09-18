using System.Globalization;

namespace Testing.Application.GetAllViajes;

public static class CamposDerivadosViajes
{
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

    /// <summary>ZemogViajesEnZamAnual.EjesEquipos, como texto para filtro UI. Null si no tiene valor -- no se inventa.</summary>
    public static string? ObtenerEjes(ViajesDto viaje) => viaje.cis_ejes_equipos?.ToString(CultureInfo.InvariantCulture);

    private static readonly string[] FormatosFecha = ["d/M/yyyy h:mm tt"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

    public static DateTime? ObtenerFechaNegocio(ViajesDto viaje)
    {
        if (viaje.cis_llave_utilizada == LlaveCisDirecto && viaje.cis_fecha_calendario is { } fechaCalendario)
            return fechaCalendario.ToDateTime(TimeOnly.MinValue);

        if (string.IsNullOrWhiteSpace(viaje.fecha_ingreso))
            return null;

        if (DateTime.TryParseExact(viaje.fecha_ingreso, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;

        if (DateTime.TryParse(viaje.fecha_ingreso, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;

        return null;
    }

    public static string? ObtenerExpedicion(ViajesDto viaje)
    {
        var valor = viaje.expedicion?.Trim();
        return string.IsNullOrEmpty(valor) || valor == "-" ? null : valor;
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
            return "Viaje";

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
        if (viaje.cis_ejes_equipos is { } ejes)
        {
            return ejes switch
            {
                5 => "Sencillo",
                6 => "Comodato",
                9 => "Full",
                _ => null,
            };
        }

        if (string.IsNullOrWhiteSpace(viaje.armado))
            return null;

        return viaje.armado.Trim().ToUpperInvariant() switch
        {
            "FULL" => "Full",
            "SENCILLO" => "Sencillo",
            _ => null,
        };
    }

    public static string? NormalizarArmadoCrudo(ViajesDto viaje)
    {
        if (viaje.cis_ejes_equipos is { } ejes)
            return ejes.ToString(CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(viaje.armado) ? null : viaje.armado.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Replica el bloque "VIAJE REDONDO" de viajes_v14.html (procesarArchivo, comentario original:
    /// "cada regreso hereda Destino/Edo.Destino de la ida previa de la MISMA unidad, orden por
    /// CitaCarga. Los regresos sin ida previa conservan su destino. Totales intactos."). Ordena
    /// todos los viajes por (id_unidad, cis_cita_carga) y, para cada tramo que NO es Ida, si existe
    /// un tramo Ida previo con la misma clave Unidad+Sucursal, le copia cis_destino/cis_estado_destino
    /// de ese Ida. Un Regreso sin Ida previa en el rango conserva su propio destino -- igual que el
    /// HTML. Muta la lista recibida in-place; no cambia Viajes/KM/Venta/Peaje/Movimiento ni ningún
    /// otro campo. Debe llamarse UNA vez, justo después de mapear los datos de CIS y antes de
    /// cualquier filtro/agrupación/UI. Ver docs/BUG_DESTINO_VIAJE_REDONDO.md.
    /// </summary>
    public static void AplicarViajeRedondo(IReadOnlyList<ViajesDto> viajes)
    {
        var ordenados = viajes
            .Select((viaje, indice) => (Viaje: viaje, Indice: indice))
            .OrderBy(x => x.Viaje.id_unidad, StringComparer.Ordinal)
            .ThenBy(x => x.Viaje.cis_cita_carga)
            .ThenBy(x => x.Indice) // desempate estable si CitaCarga coincide o falta en ambos
            .ToList();

        var ultimoIdaPorClave = new Dictionary<string, ViajesDto>();

        foreach (var (viaje, _) in ordenados)
        {
            var clave = $"{viaje.id_unidad}|{viaje.cis_sucursal}";

            if (ObtenerMovimiento(viaje) == "Ida")
            {
                ultimoIdaPorClave[clave] = viaje;
            }
            else if (ultimoIdaPorClave.TryGetValue(clave, out var idaPrevia))
            {
                viaje.cis_destino = idaPrevia.cis_destino;
                viaje.cis_estado_destino = idaPrevia.cis_estado_destino;
            }
        }
    }
}