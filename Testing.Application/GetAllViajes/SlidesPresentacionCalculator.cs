namespace Testing.Application.GetAllViajes;

public static class SlidesPresentacionCalculator
{
    /// <summary>
    /// Replica PR_corteInfo(): si corte.DiaCorte &lt; 28, excluye del dataset las filas del mes de
    /// corte (se reporta como "avance", no se usa en ninguna comparativa de Presentación) y
    /// devuelve su etiqueta para anotarla en el slide 1. Si no hay avance, devuelve los viajes
    /// tal cual y null.
    /// </summary>
    public static (IReadOnlyList<ViajesDto> Viajes, string? EtiquetaMesAvance) PrepararParaPresentacion(IReadOnlyList<ViajesDto> viajesCargados, CorteMensual? corte)
    {
        if (corte is null || corte.DiaCorte >= 28)
            return (viajesCargados, null);

        var filtrados = viajesCargados.Where(v =>
        {
            var f = CamposDerivadosViajes.ObtenerFechaNegocio(v);
            return f is null || f.Value.Year != corte.Anio || f.Value.Month != corte.Mes;
        }).ToList();

        var etiqueta = $"{NombresMes[corte.Mes - 1]} {corte.Anio}";
        return (filtrados, etiqueta);
    }

    private static readonly string[] NombresMes =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    public static PresentacionResumenDto Construir(ResumenEjecutivoDto resumen, string? etiquetaMesAvance)
    {
        if (resumen.NivelZemog is null)
            throw new InvalidOperationException("La presentación requiere al menos un mes con datos (resumen.NivelZemog).");

        var nivel = resumen.NivelZemog;
        var ultimo = nivel.MesUltimo;
        var anterior = nivel.MesAnterior;

        return new PresentacionResumenDto(
            EstadoOperacion: new SlideEstadoOperacionDto(TextoCorte(resumen, etiquetaMesAvance)),
            TresCifras: new SlideTresCifrasDto(
                Periodo: $"{nivel.PrimerMesDelAnio.Etiqueta}–{ultimo.Etiqueta}",
                VentaYtd: resumen.ArbolComparativo?.Anual.Venta ?? nivel.TotalesUltimo.Venta,
                ViajesYtd: resumen.ArbolComparativo?.Anual.Viajes ?? nivel.TotalesUltimo.Viajes,
                KmsYtd: resumen.ArbolComparativo?.Anual.Kms ?? nivel.TotalesUltimo.Kms,
                MejorMes: nivel.MejorMesDelAnio is { } mm ? (mm.Mes.Etiqueta, mm.Venta) : null,
                PeorMes: nivel.PeorMesDelAnio is { } pm ? (pm.Mes.Etiqueta, pm.Venta) : null),
            AnioVistazo: new SlideAnioVistazoDto(
                nivel.Tendencia.Select(t => (t.Mes.Etiqueta, t.Totales.Venta)).ToList()),
            ComparativoMensual: new SlideComparativoMensualDto(
                MesActual: ultimo.Etiqueta,
                MesAnterior: anterior?.Etiqueta ?? "sin mes anterior",
                VentaActual: nivel.TotalesUltimo.Venta,
                VentaAnterior: nivel.TotalesAnterior?.Venta ?? 0,
                DeltaVentaPorcentaje: nivel.DeltaVentaPctVsAnterior,
                ViajesActual: nivel.TotalesUltimo.Viajes,
                ViajesAnterior: nivel.TotalesAnterior?.Viajes ?? 0,
                DeltaViajesPorcentaje: nivel.DeltaViajesPctVsAnterior,
                KmsActual: nivel.TotalesUltimo.Kms,
                KmsAnterior: nivel.TotalesAnterior?.Kms ?? 0,
                DeltaKmsPorcentaje: nivel.DeltaKmsPctVsAnterior),
            PorCliente: new SlidePorClienteDto(
                resumen.PorCliente
                    .Select(c => new FilaClientePresentacionDto(c.Cliente, c.Bloque.TotalesUltimo.Venta, c.Bloque.DeltaVentaPctVsAnterior, c.Bloque.TotalesUltimo.Viajes, c.Bloque.DeltaViajesPctVsAnterior))
                    .ToList()),
            MezclaExpedicion: ConstruirMezclaExpedicion(resumen),
            Fugas: new SlideFugasDto(
                // VentaPerdida se guarda en POSITIVO (magnitud de la pérdida) -- DeltaVenta del DTO es negativo (actual-anterior).
                PeorDestinoCayendo: resumen.DestinosCayendo is { TotalConCaida: > 0 } d ? (d.Top25[0].Destino, -d.Top25[0].DeltaVenta) : null,
                AgenciasDesaparecidas: resumen.AgenciasDesaparecidas.TotalDesaparecidas,
                VentaAcumuladaPerdida: resumen.AgenciasDesaparecidas.VentaAcumuladaTotal),
            QueSigue: new SlideQueSigueDto());
    }

    // Mismo texto que ResumenEjecutivo.razor.Subtitulo(), más la anotación de avance (que el
    // Resumen Ejecutivo normal ya no necesita, porque ya no excluye nada).
    private static string TextoCorte(ResumenEjecutivoDto resumen, string? etiquetaMesAvance)
    {
        var rango = resumen.MesesCerrados.Count == 1
            ? resumen.MesesCerrados[0].Etiqueta
            : $"{resumen.MesesCerrados[0].Etiqueta} – {resumen.MesesCerrados[^1].Etiqueta}";

        var nota = etiquetaMesAvance is null ? "" : $" · {etiquetaMesAvance} se reporta como avance";

        return $"Meses: {rango}{nota}";
    }

    private static SlideMezclaExpedicionDto ConstruirMezclaExpedicion(ResumenEjecutivoDto resumen)
    {
        if (resumen.ArbolComparativo is null)
            return new SlideMezclaExpedicionDto(0, 0, 0, 0, 0, null, 0, 0);

        var a = ResumenEjecutivoCalculator.CalcularAsignacion(resumen.ArbolComparativo);
        return new SlideMezclaExpedicionDto(
            a.Comodato, a.Full, a.Sencillo, a.Total, a.PctComodato, a.DeltaPuntosPorcentuales,
            VentaPorViajeFull: a.VentaPorViajeFull,
            VentaPorViajeSencillo: a.VentaPorViajeSencillo);
    }
}