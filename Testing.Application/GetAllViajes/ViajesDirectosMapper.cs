namespace Testing.Application.GetAllViajes;

/// <summary>
/// Construye un <see cref="ViajesDto"/> directamente desde <see cref="DatosCisViajeDirecto"/> --
/// Fuente B del prototipo paralelo (CIS_DB sin pasar por el SP ni por el matching de Folio). NO
/// reutiliza <see cref="ViajesEnriquecidoMapper"/> porque esa clase asume una fila
/// <see cref="SpViajesDto"/> real y el resultado de un matching de Folio -- ninguno de los dos
/// existe en este flujo. Ver docs/PROTOTIPO_FUENTE_CIS.md para el detalle campo por campo y las
/// limitaciones (Armado, Peaje, Estatus).
/// </summary>
public static class ViajesDirectosMapper
{
    public static ViajesDto Mapear(DatosCisViajeDirecto cis) => new()
    {
        no_viaje = cis.NoViaje,
        id_unidad = cis.Unidad,
        id_remolque1 = cis.Remolque1,
        id_dolly = cis.Dolly,
        id_remolque2 = cis.Remolque2,
        ruta = cis.Ruta,
        expedicion = cis.Expedicion, 
        no_remision = cis.Folio, 
        id_operador1 = cis.IdOperador1,
        operador1 = cis.Operador1,
        id_operador2 = cis.IdOperador2,
        operador2 = cis.Operador2,
        factura = cis.Factura,
        armado = null, 

        _base = cis.Nomenclatura,

        cis_estado = EstadoEnriquecimientoCis.Encontrado,
        cis_llave_utilizada = CamposDerivadosViajes.LlaveCisDirecto,
        cis_motivo_no_coincidencia = null,

        cis_cliente = cis.NombreCorto,
        cis_zona = cis.Region,
        cis_matriz = cis.Nomenclatura,
        cis_sucursal = cis.Sucursal,
        cis_id_sucursal = cis.IdSucursal,
        cis_folio = cis.Folio,
        cis_folio_complemento = cis.FolioComplemento,
        cis_origen = cis.Origen,
        cis_destino = cis.Destino,
        cis_estado_origen = cis.EstadoOrigen,
        cis_estado_destino = cis.EstadoDestino,
        cis_total_venta = cis.TotalVenta,
        cis_ejes_equipos = cis.EjesEquipos,
        cis_fecha_calendario = cis.FechaCalendario,
        cis_cita_carga = cis.CitaCarga,
        cis_trayecto = cis.Trayecto,

        kms_viaje = cis.Kms,

        peaje_electronico = cis.MontoPeajeIave,
        peaje_efectivo = cis.MontoPeajeEfectivo,

    };
}