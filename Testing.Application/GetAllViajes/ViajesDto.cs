using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Application.GetAllViajes;

/// <summary>
/// Resultado de intentar enriquecer un ViajesDto con datos de CIS_DB, vía
/// ICisViajeEnrichmentRepository + ViajesEnriquecidoMapper. Siempre queda establecido.
/// </summary>
public enum EstadoEnriquecimientoCis
{
    /// <summary>no_remision venía null en el SP -- confirmado por el usuario: ocurre exactamente
    /// cuando estatus_viaje está cancelado (el viaje no se tomó, nunca se generó una remisión).
    /// No se buscó nada en CIS para esta fila; no es un error.</summary>
    NoAplica,

    /// <summary>Se encontró exactamente una fila en CIS_DB para este Folio -- los campos cis_*
    /// quedan poblados.</summary>
    Encontrado,

    /// <summary>no_remision no era null, pero ninguna fila de ZemogViajesEnZamAnual tiene ese
    /// Folio. Los campos cis_* quedan en null a propósito -- no se inventa un valor.</summary>
    NoEncontrado,

    /// <summary>Más de una fila de ZemogViajesEnZamAnual comparte el mismo Folio -- caso
    /// inesperado (la unicidad de Folio es una regla de negocio confirmada por el usuario, no
    /// forzada por un índice único en la base).</summary>
    Duplicado,
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
    // Llave usada para obtenerlos: no_remision (arriba) <-> ZemogViajesEnZamAnual.Folio,
    // confirmada por el usuario. Todos null si cis_estado no es Encontrado.

    /// <summary>Nunca null: siempre queda establecido por ViajesEnriquecidoMapper, en los 4 valores posibles.</summary>
    public EstadoEnriquecimientoCis cis_estado { get; set; } = EstadoEnriquecimientoCis.NoAplica;

    /// <summary>Sucursales.NombreCorto (vía FK ZemogViajesEnZamAnual.IdSucursal).</summary>
    public string? cis_cliente { get; set; }

    /// <summary>Sucursales.Region.</summary>
    public string? cis_zona { get; set; }

    /// <summary>Sucursales.Nomenclatura.</summary>
    public string? cis_matriz { get; set; }

    /// <summary>ZemogViajesEnZamAnual.Sucursal (texto; viene de la tabla de viajes, no de Sucursales).</summary>
    public string? cis_sucursal { get; set; }

    /// <summary>ZemogViajesEnZamAnual.IdSucursal.</summary>
    public int? cis_id_sucursal { get; set; }

    public string? cis_destino { get; set; }

    public string? cis_estado_destino { get; set; }

    public decimal? cis_total_venta { get; set; }

    public int? cis_ejes_equipos { get; set; }

    public DateOnly? cis_fecha_calendario { get; set; }

    public string? cis_trayecto { get; set; }
}