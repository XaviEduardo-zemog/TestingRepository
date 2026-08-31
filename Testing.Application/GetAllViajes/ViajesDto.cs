using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Application.GetAllViajes;

public sealed class ViajesDto
{
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
}
