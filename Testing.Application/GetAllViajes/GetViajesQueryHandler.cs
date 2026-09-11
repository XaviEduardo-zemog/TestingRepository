using MediatR;
using Testing.Application.Abstractions.Data;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryHandler(
    IApplicationDbContext dbContext,
    ICisViajeEnrichmentRepository cisRepository,
    ISegundaFuenteEnrichmentRepository segundaFuenteRepository)
    : IRequestHandler<GetViajesQuery, Result<IReadOnlyList<ViajesDto>>>
{
    private const string FormatoFecha = "yyyy-MM-dd";

    private const string SpConsultaViajes =
        "EXEC [Operaciones].[sp_ConsultaViajesZemog] " +
        "@fecha_inicio, @fecha_fin, @tipo_fecha, @areas, @id_unidad, " +
        "@estados, @id_ruta, @id_operador, @no_remision";

    private const string LlaveDirecto = "Directo";
    private const string LlaveLimpiarFolio = "LimpiarFolio";
    private const string LlaveSlash = "Slash";

    private const string MotivoNoEncontrado =
        "Ninguno de los 3 candidatos de Folio (original, LimpiarFolio, candidato con \"/\") coincidió con CIS_DB.";

    private const string MotivoDuplicado =
        "Uno de los candidatos de Folio coincide con 2 o más filas de CIS_DB -- no se puede resolver sin ambigüedad.";

    public async Task<Result<IReadOnlyList<ViajesDto>>> Handle(GetViajesQuery request, CancellationToken cancellationToken)
    {
        QueryParameter[] parametros =
        [
            new("@fecha_inicio", request.FechaInicio.ToString(FormatoFecha)),
            new("@fecha_fin", request.FechaFin.ToString(FormatoFecha)),
            new("@tipo_fecha", ToDbValue(request.TipoFecha)),
            new("@areas", ToDbValue(request.Areas)),
            new("@id_unidad", ToDbValue(request.IdUnidad)),
            new("@estados", ToDbValue(request.Estados)),
            new("@id_ruta", ToDbValue(request.IdRuta)),
            new("@id_operador", ToDbValue(request.IdOperador)),
            new("@no_remision", ToDbValue(request.NoRemision)),
        ];

        // Paso 1: SP, exactamente como antes -- sin cambios.
        var filasSp = await dbContext.QueryAsync<SpViajesDto>(SpConsultaViajes, parametros, cancellationToken);

        // Paso 2: dos exclusiones explícitas e independientes -- sin cambio.
        var filasActivas = filasSp.Where(sp => !EsNoRemisionNulo(sp.no_remision) && !EsEstatusCancelado(sp.estatus_viaje)).ToList();

        // Paso 3: enriquecimiento CIS_DB por Folio -- comportamiento EXACTO existente, sin
        // ningún cambio (misma construcción de candidatos, misma consulta en lote, mismo
        // Enriquecer -- solo se renombró a EnriquecerPorCis para reflejar que es específicamente
        // la ruta CIS, ahora que existe una 2ª ruta de resolución).
        var candidatosPorFila = filasActivas.ToDictionary(sp => sp, sp => ConstruirCandidatos(sp.no_remision!));
        var todosLosCandidatos = candidatosPorFila.Values.SelectMany(c => c.Todos).Distinct().ToList();
        var enriquecimientoCis = todosLosCandidatos.Count == 0
            ? CisEnrichmentBatchResult.Vacio
            : await cisRepository.ObtenerPorFoliosAsync(todosLosCandidatos, cancellationToken);

        var viajesCis = filasActivas
            .Select(sp => (Sp: sp, Viaje: EnriquecerPorCis(sp, candidatosPorFila[sp], enriquecimientoCis)))
            .ToList();

        // Paso 4: separar temporalmente los pendientes (NoEncontrado/Duplicado) -- estos son los
        // ÚNICOS candidatos a la 2ª fuente. Los ya Encontrado por CIS_DB nunca se tocan, nunca se
        // vuelven a consultar, nunca compiten con la 2ª fuente.
        var resueltosPorCis = viajesCis
            .Where(x => x.Viaje.cis_estado == EstadoEnriquecimientoCis.Encontrado)
            .Select(x => x.Viaje)
            .ToList();

        var pendientes = viajesCis
            .Where(x => x.Viaje.cis_estado != EstadoEnriquecimientoCis.Encontrado)
            .ToList();

        var resueltosPorFallback = new List<ViajesDto>();

        if (pendientes.Count > 0)
        {
            // Pasos 5-8: consultas en lote de la 2ª fuente (Sucursales por _base, RutasZam por
            // código de ruta, trafico_guia por Factura) -- SOLO con los valores distintos de las
            // filas pendientes, nunca una consulta por fila.
            var basesDistintas = pendientes
                .Select(p => p.Sp._base)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b!.Trim())
                .Distinct()
                .ToList();

            var codigosDistintos = pendientes
                .Select(p => PrimerTokenRuta(p.Sp.ruta))
                .Where(c => c is not null)
                .Select(c => c!)
                .Distinct()
                .ToList();

            var facturasDistintas = pendientes
                .Select(p => p.Sp.factura)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Trim())
                .Distinct()
                .ToList();

            var segundaFuente = basesDistintas.Count == 0 && codigosDistintos.Count == 0 && facturasDistintas.Count == 0
                ? SegundaFuenteBatchResult.Vacio
                : await segundaFuenteRepository.ObtenerAsync(basesDistintas, codigosDistintos, facturasDistintas, cancellationToken);

            foreach (var (sp, viajeOriginal) in pendientes)
            {
                var sucursal = ObtenerSucursalFallback(sp, segundaFuente);
                var ruta = ObtenerRutaFallback(sp, segundaFuente);
                var guia = ObtenerGuiaFallback(sp, segundaFuente);

                resueltosPorFallback.Add(ViajesEnriquecidoMapper.EnriquecerConFallback(
                    sp, sucursal, ruta, guia, viajeOriginal.cis_motivo_no_coincidencia));
            }
        }

        // Paso 13: el filtro final permite Encontrado (CIS) y EncontradoFallback (2ª fuente) --
        // cualquier fila que no logre ninguno de los dos conserva su cis_estado/cis_motivo_no_coincidencia
        // diagnóstico pero NUNCA entra a esta lista (nunca entra silenciosamente a filtros/jerarquías/métricas).
        var viajesFinal = resueltosPorCis
            .Concat(resueltosPorFallback.Where(v => v.cis_estado == EstadoEnriquecimientoCis.EncontradoFallback))
            .ToList();

        return Result.Success<IReadOnlyList<ViajesDto>>(viajesFinal);
    }

    // internal (no private): permite que Testing.Application.Tests pruebe el matching de Folio
    // directo, sin necesidad de simular el pipeline completo del handler para cada caso.
    internal static bool EsNoRemisionNulo(string? noRemision) => string.IsNullOrWhiteSpace(noRemision);

    // Cubre "Cancelado" y "Cancelada" -- ambas variantes fueron confirmadas por el usuario como
    // el mismo estatus de cancelación en esta misma sesión, no solo "Cancelado" tal como se lee
    // literal en el enunciado del Prompt 2.
    internal static bool EsEstatusCancelado(string? estatusViaje)
    {
        if (string.IsNullOrWhiteSpace(estatusViaje))
            return false;

        var valor = estatusViaje.Trim();
        return valor.Equals("Cancelado", StringComparison.OrdinalIgnoreCase)
            || valor.Equals("Cancelada", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed record CandidatosFolio(string Directo, string? LimpiarFolio, string? Slash)
    {
        public IEnumerable<string> Todos
        {
            get
            {
                yield return Directo;
                if (LimpiarFolio is not null) yield return LimpiarFolio;
                if (Slash is not null) yield return Slash;
            }
        }
    }

    // Construye los 3 candidatos de Prompt 2, punto 4 -- siempre en este orden, nunca se
    // reordenan ni se prueban variantes adicionales (ej. NoViaje).
    internal static CandidatosFolio ConstruirCandidatos(string noRemisionCruda)
    {
        var directo = noRemisionCruda.Trim();

        var limpio = LimpiarFolio(directo);
        var limpiarFolio = limpio != directo ? limpio : null;

        string? slash = null;
        if (directo.Contains(','))
        {
            var candidato = CandidatoSlash(directo);
            if (candidato != directo && candidato != limpiarFolio)
                slash = candidato;
        }

        return new CandidatosFolio(directo, limpiarFolio, slash);
    }

    // Paso 2: conserva lo anterior a la primera coma, y de eso, lo anterior al primer guion
    // medio. Un solo resultado, sin probar variantes.
    internal static string LimpiarFolio(string valor)
    {
        var v = valor;

        var indiceComa = v.IndexOf(',');
        if (indiceComa >= 0)
            v = v[..indiceComa];

        var indiceGuion = v.IndexOf('-');
        if (indiceGuion >= 0)
            v = v[..indiceGuion];

        return v.Trim();
    }

    // Paso 3: separa por coma, limpia cada parte (quita sufijo -C/-R/R final), elimina partes
    // repetidas conservando el orden, une con "/". CIS_DB guarda algunos Folios compuestos
    // exactamente así (confirmado con datos reales: 30 casos en el rango 2026-01-01/2026-09-09).
    internal static string CandidatoSlash(string valor)
    {
        var partes = valor.Split(',');
        var limpias = new List<string>();

        foreach (var parte in partes)
        {
            var limpia = QuitarSufijo(parte.Trim());
            if (limpia.Length > 0 && !limpias.Contains(limpia))
                limpias.Add(limpia);
        }

        return string.Join('/', limpias);
    }

    internal static string QuitarSufijo(string valor)
    {
        if (valor.EndsWith("-C", StringComparison.OrdinalIgnoreCase) || valor.EndsWith("-R", StringComparison.OrdinalIgnoreCase))
            return valor[..^2];

        // Sufijo "R" sin guion (ej. "8003743465R"), confirmado en datos reales -- solo si el
        // caracter anterior es un dígito, para no cortar un Folio alfanumérico legítimo.
        if (valor.Length > 1 && (valor[^1] is 'R' or 'r') && char.IsDigit(valor[^2]))
            return valor[..^1];

        return valor;
    }

    // Antes se llamaba "Enriquecer" -- renombrado a EnriquecerPorCis (Prompt 2, integración
    // trafico_guia) para distinguirlo de la 2ª fuente; el CUERPO no cambió ni una línea.
    private static ViajesDto EnriquecerPorCis(SpViajesDto sp, CandidatosFolio candidatos, CisEnrichmentBatchResult enriquecimiento)
    {
        var intentos = new List<(string Llave, string Candidato)> { (LlaveDirecto, candidatos.Directo) };
        if (candidatos.LimpiarFolio is { } limpiarFolio)
            intentos.Add((LlaveLimpiarFolio, limpiarFolio));
        if (candidatos.Slash is { } slash)
            intentos.Add((LlaveSlash, slash));

        foreach (var (llave, candidato) in intentos)
        {
            if (enriquecimiento.PorFolio.TryGetValue(candidato, out var cis))
                return ViajesEnriquecidoMapper.Enriquecer(sp, cis, EstadoEnriquecimientoCis.Encontrado, llave, null);
        }

        // Ninguno coincidió como único -- si alguno de los 3 candidatos coincide con 2+ filas de
        // CIS_DB, es Duplicado (no NoEncontrado); nunca se resuelve con First()/Single().
        if (intentos.Any(i => enriquecimiento.FoliosDuplicados.Contains(i.Candidato)))
            return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.Duplicado, null, MotivoDuplicado);

        return ViajesEnriquecidoMapper.Enriquecer(sp, null, EstadoEnriquecimientoCis.NoEncontrado, null, MotivoNoEncontrado);
    }

    // Paso 6: primer token de SP.ruta, antes del primer espacio. Ej. "32203931 P. Santa Rita -
    // Planta Guadiana - R" -> "32203931". internal (no private) para poder probarse directo.
    internal static string? PrimerTokenRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return null;

        var r = ruta.Trim();
        var indiceEspacio = r.IndexOf(' ');
        return indiceEspacio < 0 ? r : r[..indiceEspacio];
    }

    // Paso 5: Sucursales por _base -- null si _base viene vacío o no existe en el diccionario
    // (código no encontrado en Sucursales.Nomenclatura). Nunca se inventa un valor.
    private static DatosSucursalFallback? ObtenerSucursalFallback(SpViajesDto sp, SegundaFuenteBatchResult segundaFuente)
    {
        if (string.IsNullOrWhiteSpace(sp._base))
            return null;

        return segundaFuente.PorBase.GetValueOrDefault(sp._base.Trim());
    }

    // Paso 6: RutasZam por código -- los códigos ambiguos (2+ filas) ya fueron excluidos del
    // diccionario por el repositorio, así que un GetValueOrDefault normal ya respeta "nunca
    // First() sin validación": un código ambiguo simplemente no está en PorCodigoRuta.
    private static DatosRutaFallback? ObtenerRutaFallback(SpViajesDto sp, SegundaFuenteBatchResult segundaFuente)
    {
        var codigo = PrimerTokenRuta(sp.ruta);
        return codigo is null ? null : segundaFuente.PorCodigoRuta.GetValueOrDefault(codigo);
    }

    // Paso 7: llave lógica Factura + NoViaje -- NUNCA NoViaje solo. NumGuia (Factura) ya tiene
    // índice único real en trafico_guia, pero igual se valida aquí que el NoViaje que trae esa
    // fila coincida con el no_viaje del SP antes de aceptar el match; si no coincide, se trata
    // como no resuelto (nunca se asume, nunca se usa NoViaje como si fuera suficiente por sí solo).
    private static DatosTraficoGuiaFallback? ObtenerGuiaFallback(SpViajesDto sp, SegundaFuenteBatchResult segundaFuente)
    {
        if (string.IsNullOrWhiteSpace(sp.factura) || sp.no_viaje is null)
            return null;

        if (!segundaFuente.PorFactura.TryGetValue(sp.factura.Trim(), out var guia))
            return null;

        return guia.NoViaje == sp.no_viaje.Value ? guia : null;
    }

    private static object? ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}