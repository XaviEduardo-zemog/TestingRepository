namespace Testing.Application.GetAllViajes;

/// <summary>
/// Los datos crudos del SP (SpViajesDto) con el enriquecimiento
/// de CIS_DB (DatosCisViaje?) para producir el ViajesDto final que consume el resto del sistema.
/// </summary>
public static class ViajesEnriquecidoMapper
{
    public static ViajesDto Enriquecer(
        SpViajesDto sp,
        DatosCisViaje? cis,
        EstadoEnriquecimientoCis estado,
        string? llaveUtilizada,
        string? motivoNoCoincidencia) => new()
        {
            // ---- Campos exactos del SP, sin transformación ----
            _base = sp._base,
            no_viaje = sp.no_viaje,
            estatus_viaje = sp.estatus_viaje,
            id_unidad = sp.id_unidad,
            id_remolque1 = sp.id_remolque1,
            id_dolly = sp.id_dolly,
            id_remolque2 = sp.id_remolque2,
            expedicion = sp.expedicion,
            ruta = sp.ruta,
            circuito = sp.circuito,
            direccion = sp.direccion,
            kms_viaje = sp.kms_viaje,
            no_remision = sp.no_remision,
            comision = sp.comision,
            anticipos = sp.anticipos,
            id_operador1 = sp.id_operador1,
            operador1 = sp.operador1,
            id_operador2 = sp.id_operador2,
            operador2 = sp.operador2,
            peaje_electronico = sp.peaje_electronico,
            peaje_efectivo = sp.peaje_efectivo,
            fecha_cita = sp.fecha_cita,
            fecha_ingreso = sp.fecha_ingreso,
            fecha_real_viaje = sp.fecha_real_viaje,
            fecha_real_fin_viaje = sp.fecha_real_fin_viaje,
            armado = sp.armado,
            no_liquidacion = sp.no_liquidacion,
            fecha_liquidacion = sp.fecha_liquidacion,
            factura = sp.factura,
            diesel_cargado = sp.diesel_cargado,
            costo_diesel = sp.costo_diesel,
            subtotal_factura = sp.subtotal_factura,
            cargado_vacio = sp.cargado_vacio,
            tipo_operacion = sp.tipo_operacion,

            // ---- Enriquecimiento CIS -- todos null salvo que estado sea Encontrado ----
            cis_estado = estado,
            cis_llave_utilizada = llaveUtilizada,
            cis_motivo_no_coincidencia = motivoNoCoincidencia,
            cis_cliente = cis?.NombreCorto,
            cis_zona = cis?.Region,
            cis_matriz = cis?.Nomenclatura,
            cis_sucursal = cis?.Sucursal,
            cis_id_sucursal = cis?.IdSucursal,
            cis_folio = cis?.Folio,
            cis_folio_complemento = cis?.FolioComplemento,
            cis_origen = cis?.Origen,
            cis_destino = cis?.Destino,
            cis_estado_origen = cis?.EstadoOrigen,
            cis_estado_destino = cis?.EstadoDestino,
            cis_total_venta = cis?.TotalVenta,
            cis_ejes_equipos = cis?.EjesEquipos,
            cis_fecha_calendario = cis?.FechaCalendario,
            cis_trayecto = cis?.Trayecto,
            cis_operacion = cis?.Operacion,
        };
}