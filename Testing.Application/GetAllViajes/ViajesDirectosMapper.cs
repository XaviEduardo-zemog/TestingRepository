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
        // ---- No hay fila SP real detrás de esta fuente: los campos sin equivalente en CIS
        // quedan en su valor por defecto (null) a propósito -- no se inventan. estatus_viaje NO
        // se puebla con EstatusAsignacion (no son equivalentes, ver docs/PROTOTIPO_FUENTE_CIS.md
        // sección Estatus). armado queda null: su regla sigue sin fuente confirmada y esta etapa
        // tiene explícitamente prohibido inferirla de EjesEquipos/Ruta/Expedicion/remolques.
        no_viaje = cis.NoViaje,
        id_unidad = cis.Unidad,
        id_remolque1 = cis.Remolque1,
        id_dolly = cis.Dolly,
        id_remolque2 = cis.Remolque2,
        ruta = cis.Ruta,
        expedicion = cis.Expedicion, // passthrough crudo -- el fallback a Ruta se resuelve on-demand vía CamposDerivadosViajes.ObtenerExpedicion, igual que ObtenerTarifa lee "ruta" on-demand
        no_remision = cis.Folio, // compatibilidad: ya no hay "matching", Folio es la llave propia de la fila
        id_operador1 = cis.IdOperador1,
        operador1 = cis.Operador1,
        id_operador2 = cis.IdOperador2,
        operador2 = cis.Operador2,
        factura = cis.Factura,
        armado = null, // pendiente -- ver restricciones de esta etapa, no inventar regla

        // _base: shim temporal de compatibilidad para OperadoresRotacionCalculator.cs, que hoy lee
        // v._base directamente. Usa el mismo valor que Matriz (Sucursales.Nomenclatura) -- ver
        // docs/DISENO_CONSULTA_DIRECTA_CIS.md §5. NO es la solución definitiva.
        _base = cis.Nomenclatura,

        // Enriquecimiento CIS: en la Fuente B toda fila "existe" por definición -- no hay concepto
        // de NoEncontrado/Duplicado (exclusivo del matching de Folio de la Fuente A). Se deja
        // Encontrado para que el resto del pipeline (que filtra por cis_estado == Encontrado) no
        // necesite un tercer estado.
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
        cis_trayecto = cis.Trayecto,

        // KM: Etapa B confirmó Kms == kms_viaje al 100% en la muestra real
        // (docs/VALIDACION_SP_VS_CIS.md §12) -- mapeo directo, sin factor de conversión.
        kms_viaje = cis.Kms,

        // Peajes: CANDIDATOS DIAGNÓSTICOS, NO VALIDADOS. Etapa B midió 15.3% de diferencia contra
        // los peajes reales del SP (docs/VALIDACION_SP_VS_CIS.md §14). Se mapean para que el KPI
        // de Peaje no quede engañosamente en cero bajo Fuente B, pero cualquier cifra de Peaje
        // debe leerse como preliminar hasta reconciliar -- ver docs/PROTOTIPO_FUENTE_CIS.md.
        peaje_electronico = cis.MontoPeajeIave,
        peaje_efectivo = cis.MontoPeajeEfectivo,

        // fecha_ingreso queda null a propósito: esta etapa NO intenta reproducir fecha_ingreso del
        // SP. La fecha de negocio de esta fuente es cis_fecha_calendario (arriba), consumida por
        // CamposDerivadosViajes.ObtenerFechaNegocio a través de cis_llave_utilizada == LlaveCisDirecto.
    };
}