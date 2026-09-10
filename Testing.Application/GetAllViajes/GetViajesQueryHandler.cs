using MediatR;
using Testing.Application.Abstractions.Data;
using Testing.Domain.Common;

namespace Testing.Application.GetAllViajes;

public sealed class GetViajesQueryHandler(IApplicationDbContext dbContext, ICisViajeEnrichmentRepository cisRepository)
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

        var filasSp = await dbContext.QueryAsync<SpViajesDto>(SpConsultaViajes, parametros, cancellationToken);

        // Prompt 2, puntos 1-2: dos exclusiones explícitas e independientes (no una sola regla
        // combinada) -- una fila puede caer en cualquiera de las dos, o en ambas.
        var filasActivas = filasSp.Where(sp => !EsNoRemisionNulo(sp.no_remision) && !EsEstatusCancelado(sp.estatus_viaje)).ToList();

        // Punto 3: normalización como texto -- Trim() en todos los candidatos, ninguna
        // conversión numérica en ningún punto (no_remision/Folio son y siempre serán string).
        var candidatosPorFila = filasActivas.ToDictionary(sp => sp, sp => ConstruirCandidatos(sp.no_remision!));

        // Punto 7: UNA sola consulta en lote con TODOS los candidatos de TODAS las filas --
        // nunca una consulta por fila.
        var todosLosCandidatos = candidatosPorFila.Values.SelectMany(c => c.Todos).Distinct().ToList();
        var enriquecimiento = todosLosCandidatos.Count == 0
            ? CisEnrichmentBatchResult.Vacio
            : await cisRepository.ObtenerPorFoliosAsync(todosLosCandidatos, cancellationToken);

        var viajes = filasActivas
            .Select(sp => Enriquecer(sp, candidatosPorFila[sp], enriquecimiento))
            // Punto 5: NoEncontrado (y Duplicado) se excluyen de la tabla principal y no
            // contribuyen a métricas ni totales -- solo Encontrado permanece.
            .Where(v => v.cis_estado == EstadoEnriquecimientoCis.Encontrado)
            .ToList();

        return Result.Success<IReadOnlyList<ViajesDto>>(viajes);
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

    private static ViajesDto Enriquecer(SpViajesDto sp, CandidatosFolio candidatos, CisEnrichmentBatchResult enriquecimiento)
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

    private static object? ToDbValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}