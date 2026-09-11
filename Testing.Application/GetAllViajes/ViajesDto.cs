using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Application.GetAllViajes;

/// <summary>
/// Resultado de intentar enriquecer un viaje del SP con datos de CIS_DB, vía
/// ICisViajeEnrichmentRepository + ViajesEnriquecidoMapper. Siempre queda establecido.
/// </summary>
public enum EstadoEnriquecimientoCis
{
    /// <summary>no_remision venía null/vacío en el SP -- el viaje está cancelado (no se generó
    /// remisión). No se buscó nada en CIS para esta fila; no es un error.</summary>
    NoAplica,

    /// <summary>Se encontró exactamente una fila en CIS_DB para uno de los 3 candidatos de
    /// Folio -- los campos cis_* quedan poblados y cis_llave_utilizada indica cuál candidato
    /// coincidió.</summary>
    Encontrado,

    /// <summary>Ninguno de los 3 candidatos de Folio (original, LimpiarFolio, candidato con "/")
    /// existe en CIS_DB. Los campos cis_* quedan en null a propósito -- no se inventa un valor,
    /// no se usa NoViaje ni ningún otro dato para adivinar.</summary>
    NoEncontrado,

    /// <summary>2 o más filas de CIS_DB comparten el mismo Folio candidato -- caso real pero
    /// raro (confirmado con datos: 29 casos históricos, 0 en 2026). Nunca se resuelve con
    /// First()/Single(); se reporta y se excluye.</summary>
    Duplicado,

    /// <summary>
    /// Prompt 2 (2026-09-11, integración trafico_guia) -- la fila NO se encontró en CIS_DB
    /// (llegó aquí siendo NoEncontrado o Duplicado), pero SÍ se resolvió por la 2ª fuente de
    /// enriquecimiento (Sucursales por _base + RutasZam por código de ruta + trafico_guia por
    /// Factura+NoViaje). Se distingue deliberadamente de <see cref="Encontrado"/> -- NUNCA se
    /// marca una fila resuelta por esta vía como si viniera directamente de CIS_DB. La condición
    /// para este estado es específicamente haber encontrado Venta real en trafico_guia (Factura
    /// + NoViaje verificados); Sucursales/RutasZam solas, sin Venta, no bastan -- ver
    /// GetViajesQueryHandler y ViajesEnriquecidoMapper.EnriquecerConFallback.
    /// </summary>
    EncontradoFallback,
}

public sealed class ViajesDto
{
    // ---------- Contrato original del SP (sin cambios) ----------

    [Column("base")]
    public string? _base { get; set; }

    public int? no_viaje { get; set; }

    public string? estatus_viaje { get; set; }

    public string? id_unidad { get; set; }

    public string? id_remolque1 { get; set; }

    public string? id_dolly { get; set; }

    public string? id_remolque2 { get; set; }

    public string? expedicion { get; set; }

    public string? ruta { get; set; }

    public string? circuito { get; set; }

    public string? direccion { get; set; }

    public decimal? kms_viaje { get; set; }

    public string? no_remision { get; set; }

    public decimal? comision { get; set; }

    public decimal? anticipos { get; set; }

    public int? id_operador1 { get; set; }

    public string? operador1 { get; set; }

    public int? id_operador2 { get; set; }

    public string? operador2 { get; set; }

    public decimal? peaje_electronico { get; set; }

    public decimal? peaje_efectivo { get; set; }

    public string? fecha_cita { get; set; }

    public string? fecha_ingreso { get; set; }

    public string? fecha_real_viaje { get; set; }

    public string? fecha_real_fin_viaje { get; set; }

    public string? armado { get; set; }

    public int? no_liquidacion { get; set; }

    public string? fecha_liquidacion { get; set; }

    public string? factura { get; set; }

    public decimal? diesel_cargado { get; set; }

    public decimal? costo_diesel { get; set; }

    public decimal? subtotal_factura { get; set; }

    public string? cargado_vacio { get; set; }

    public string? tipo_operacion { get; set; }

    // ---------- Enriquecimiento CIS_DB ----------
    // Llave usada para buscar: no_remision (arriba, tal como vino del SP) contra 3 candidatos
    // de ZemogViajesEnZamAnual.Folio -- ver GetViajesQueryHandler.LimpiarFolio/CandidatoSlash.
    // Todos los campos cis_* quedan en null si cis_estado no es Encontrado.

    /// <summary>Nunca null: siempre queda establecido por ViajesEnriquecidoMapper, en los 4 valores posibles.</summary>
    public EstadoEnriquecimientoCis cis_estado { get; set; } = EstadoEnriquecimientoCis.NoAplica;

    /// <summary>Cuál de los 3 candidatos coincidió: "Directo", "LimpiarFolio" o "Slash". Null si cis_estado no es Encontrado.</summary>
    public string? cis_llave_utilizada { get; set; }

    /// <summary>Explica por qué NO se encontró/aplicó -- null únicamente cuando cis_estado es Encontrado.</summary>
    public string? cis_motivo_no_coincidencia { get; set; }

    /// <summary>
    /// Prompt 2 (integración trafico_guia) -- campo de auditoría explícito que distingue de dónde
    /// viene el enriquecimiento final: "CIS_DB" cuando cis_estado es Encontrado (vía Folio),
    /// "Fallback:Sucursales+RutasZam+TraficoGuia" cuando cis_estado es EncontradoFallback, o null
    /// si no se encontró por ningún medio. Los campos cis_* de abajo representan el
    /// enriquecimiento FINAL (cualquiera de las 2 fuentes) -- este campo es exclusivamente para
    /// diagnóstico/auditoría, no para lógica de negocio.
    /// </summary>
    public string? cis_fuente_enriquecimiento { get; set; }

    /// <summary>Sucursales.NombreCorto (vía FK ZemogViajesEnZamAnual.IdSucursal).</summary>
    public string? cis_cliente { get; set; }

    /// <summary>Sucursales.Region.</summary>
    public string? cis_zona { get; set; }

    /// <summary>Sucursales.Nomenclatura.</summary>
    public string? cis_matriz { get; set; }

    /// <summary>ZemogViajesEnZamAnual.Sucursal (texto propio del viaje; NO es lo mismo que Nomenclatura/Matriz -- ver Prompt 1 §8, decisión pendiente).</summary>
    public string? cis_sucursal { get; set; }

    /// <summary>ZemogViajesEnZamAnual.IdSucursal.</summary>
    public int? cis_id_sucursal { get; set; }

    /// <summary>ZemogViajesEnZamAnual.Folio -- el Folio real de CIS_DB que coincidió (no necesariamente igual a no_remision tal cual).</summary>
    public string? cis_folio { get; set; }

    /// <summary>ZemogViajesEnZamAnual.FolioComplemento.</summary>
    public string? cis_folio_complemento { get; set; }

    public string? cis_origen { get; set; }

    public string? cis_destino { get; set; }

    public string? cis_estado_origen { get; set; }

    public string? cis_estado_destino { get; set; }

    public decimal? cis_total_venta { get; set; }

    public int? cis_ejes_equipos { get; set; }

    public DateOnly? cis_fecha_calendario { get; set; }

    public string? cis_trayecto { get; set; }

    /// <summary>ZemogViajesEnZamAnual.Operacion.</summary>
    public string? cis_operacion { get; set; }
}