namespace Testing.Application.GetAllViajes;

public static class SlidesPresentacionCalculator
{
    public const string PendienteVenta = ResumenEjecutivoCalculator.PendienteVenta;

    public static PresentacionResumenDto Construir(ResumenEjecutivoDto resumen)
    {
        if (resumen.NivelZemog is null)
            throw new InvalidOperationException("La presentación requiere al menos un mes cerrado (resumen.NivelZemog).");

        var nivel = resumen.NivelZemog;
        var ultimo = nivel.MesUltimo;
        var anterior = nivel.MesAnterior;

        return new PresentacionResumenDto(
            EstadoOperacion: new SlideEstadoOperacionDto(TextoCorte(resumen)),
            TresCifras: new SlideTresCifrasDto(
                Periodo: $"{nivel.PrimerMesDelAnio.Etiqueta}–{ultimo.Etiqueta}",
                ViajesYtd: resumen.ArbolComparativo?.Anual.Viajes ?? nivel.TotalesUltimo.Viajes,
                KmsYtd: resumen.ArbolComparativo?.Anual.Kms ?? nivel.TotalesUltimo.Kms,
                VentaPendienteMotivo: PendienteVenta),
            AnioVistazo: new SlideAnioVistazoDto(PendienteMotivo: PendienteVenta + " (gráfica de venta mensual, 100% Venta, sin sustituto)"),
            ComparativoMensual: new SlideComparativoMensualDto(
                MesActual: ultimo.Etiqueta,
                MesAnterior: anterior?.Etiqueta ?? "sin mes anterior",
                VentaPendienteMotivo: PendienteVenta,
                ViajesActual: nivel.TotalesUltimo.Viajes,
                ViajesAnterior: nivel.TotalesAnterior?.Viajes ?? 0,
                DeltaViajesPorcentaje: nivel.DeltaViajesPctVsAnterior,
                KmsActual: nivel.TotalesUltimo.Kms,
                KmsAnterior: nivel.TotalesAnterior?.Kms ?? 0,
                DeltaKmsPorcentaje: nivel.DeltaKmsPctVsAnterior),
            PorCliente: new SlidePorClienteDto(
                resumen.PorCliente
                    .Select(c => new FilaClientePresentacionDto(c.Cliente, PendienteVenta, c.Bloque.TotalesUltimo.Viajes, c.Bloque.DeltaViajesPctVsAnterior))
                    .ToList()),
            MezclaExpedicion: ConstruirMezclaExpedicion(resumen),
            Fugas: new SlideFugasDto(
                CaidaDestinoPendienteMotivo: "Bloque 8.5 requiere Venta, ver §54.122",
                AgenciasDesaparecidas: resumen.AgenciasDesaparecidas.Count,
                VentaAcumuladaPendienteMotivo: PendienteVenta),
            QueSigue: new SlideQueSigueDto());
    }

    private static string TextoCorte(ResumenEjecutivoDto resumen)
    {
        var rango = resumen.MesesCerrados.Count == 1
            ? resumen.MesesCerrados[0].Etiqueta
            : $"{resumen.MesesCerrados[0].Etiqueta} – {resumen.MesesCerrados[^1].Etiqueta}";

        var nota = resumen.MesAbiertoExcluido
            ? $" · {resumen.EtiquetaMesAbiertoExcluido} excluido (mes en curso, sin cerrar)"
            : "";

        return $"Meses cerrados: {rango}{nota}";
    }

    private static SlideMezclaExpedicionDto ConstruirMezclaExpedicion(ResumenEjecutivoDto resumen)
    {
        if (resumen.ArbolComparativo is null)
            return new SlideMezclaExpedicionDto(0, 0, 0, 0, 0, null, PendienteVenta);

        var a = ResumenEjecutivoCalculator.CalcularAsignacion(resumen.ArbolComparativo);
        return new SlideMezclaExpedicionDto(
            a.Comodato, a.Full, a.Sencillo, a.Total, a.PctComodato, a.DeltaPuntosPorcentuales,
            NotaCierrePendienteMotivo: PendienteVenta + " ($/viaje por tipo de expedición)");
    }
}