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
            cis_fuente_enriquecimiento = estado == EstadoEnriquecimientoCis.Encontrado ? "CIS_DB" : null,
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

    /// <summary>
    /// Prompt 2 (2026-09-11, integración trafico_guia) -- 2ª fuente de enriquecimiento (fallback),
    /// usada ÚNICAMENTE para filas que ya fallaron en CIS_DB (NoEncontrado/Duplicado). NO
    /// modifica ni reemplaza <see cref="Enriquecer"/> (la ruta CIS sigue intacta, sin cambios).
    ///
    /// Condición de éxito: <paramref name="guia"/> (trafico_guia, Factura+NoViaje verificados)
    /// debe existir -- sin Venta real no hay estado EncontradoFallback, tal como pide el
    /// requisito de no dejar entrar silenciosamente a las métricas una fila sin una fuente válida
    /// de Venta. Sucursales/RutasZam se aplican igual estén o no disponibles (afectan la
    /// jerarquía/ruta, nunca el estado final).
    ///
    /// Prioridad de fuentes (fallback): Cliente/Zona/Matriz vía Sucursales(_base); Origen/Destino
    /// vía RutasZam(código) -- null si el código fue ambiguo o no existe, NUNCA se adivina;
    /// Venta vía trafico_guia.Subtotal (nunca subtotal_factura); Kilómetros: se conserva
    /// sp.kms_viaje como ya lo consume ContribucionViajeProyectada.Kms -- RutasZam.Kms NO
    /// reemplaza esto (no hay evidencia de que la lógica existente lo requiera); Estados de
    /// origen/destino y Ejes: sin fuente real disponible, se dejan null, nunca inventados;
    /// Movimiento (cis_trayecto): SP.direccion se usa como fallback SOLO cuando su valor es
    /// exactamente "Ida" o "Regreso" (verificado con datos reales: 186/188 filas pendientes de
    /// todo el rango 2026 traen ese valor limpio; los 2 casos restantes -- "Tramo" -- quedan
    /// null a propósito, nunca se fuerzan a Ida/Regreso).
    /// </summary>
    public static ViajesDto EnriquecerConFallback(
        SpViajesDto sp,
        DatosSucursalFallback? sucursal,
        DatosRutaFallback? ruta,
        DatosTraficoGuiaFallback? guia,
        string? motivoNoEncontradoOriginal) => new()
        {
            // ---- Campos exactos del SP, sin transformación (idéntico a Enriquecer) ----
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

            // ---- Enriquecimiento: 2ª fuente (fallback) ----
            cis_estado = guia is not null ? EstadoEnriquecimientoCis.EncontradoFallback : EstadoEnriquecimientoCis.NoEncontrado,
            cis_llave_utilizada = guia is not null ? "Factura+NoViaje" : null,
            cis_motivo_no_coincidencia = guia is not null ? null : motivoNoEncontradoOriginal,
            cis_fuente_enriquecimiento = guia is not null ? "Fallback:Sucursales+RutasZam+TraficoGuia" : null,
            cis_cliente = sucursal?.Cliente,
            cis_zona = sucursal?.Zona,
            cis_matriz = sucursal?.Matriz,
            cis_sucursal = sucursal?.Sucursal,
            cis_id_sucursal = sucursal?.IdSucursal,
            cis_folio = null, // no aplica -- no hay Folio de CIS_DB en este camino
            cis_folio_complemento = null,
            cis_origen = ruta?.Origen,
            cis_destino = ruta?.Destino,
            cis_estado_origen = null, // sin fuente real -- nunca se inventa
            cis_estado_destino = null, // sin fuente real -- nunca se inventa
            cis_total_venta = guia?.Subtotal,
            cis_ejes_equipos = null, // sin fuente real -- nunca se inventa
            cis_fecha_calendario = guia is not null ? DateOnly.FromDateTime(guia.FechaGuia) : null,
            cis_trayecto = ResolverTrayectoFallback(sp.direccion),
            cis_operacion = null, // sin fuente real
        };

    // SP.direccion como fallback de Movimiento -- SOLO cuando es exactamente "Ida"/"Regreso"
    // (verificado con datos reales antes de implementar esto). Cualquier otro valor (ej.
    // "Tramo") se deja null a propósito -- nunca se fuerza a Ida/Regreso por adivinanza.
    private static string? ResolverTrayectoFallback(string? direccionSp)
    {
        if (string.IsNullOrWhiteSpace(direccionSp))
            return null;

        return direccionSp.Trim().ToUpperInvariant() switch
        {
            "IDA" => "Ida",
            "REGRESO" => "Regreso",
            _ => null,
        };
    }
}