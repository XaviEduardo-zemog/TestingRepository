namespace Testing.Application.GetAllViajes;

public sealed record SlideEstadoOperacionDto(string TextoCorte)
{
    public int PasosRevelado => 1;
}

/// <summary>Slide 2. Mejor/peor mes son 100% Venta — VentaPendienteMotivo cubre ambos (venta acumulada y mejor/peor mes).</summary>
public sealed record SlideTresCifrasDto(
    string Periodo,
    int ViajesYtd,
    decimal KmsYtd,
    string VentaPendienteMotivo)
{
    public int PasosRevelado => 3; // Venta acumulada / Viajes / KM, uno a la vez -- igual que 3 <div class="pkpi">
}

/// <summary>Slide 3. 100% Venta (gráfica de venta mensual) — no hay ninguna cifra no-Venta que mostrar en su lugar.</summary>
public sealed record SlideAnioVistazoDto(string PendienteMotivo)
{
    public int PasosRevelado => 1;
}

/// <summary>Slide 4. Venta+Δ% pendiente; Viajes/KM+Δ% funcionales (mismos datos que ResumenBloqueNivel, Bloque 8.2).</summary>
public sealed record SlideComparativoMensualDto(
    string MesActual,
    string MesAnterior,
    string VentaPendienteMotivo,
    int ViajesActual,
    int ViajesAnterior,
    decimal? DeltaViajesPorcentaje,
    decimal KmsActual,
    decimal KmsAnterior,
    decimal? DeltaKmsPorcentaje)
{
    public int PasosRevelado => 3; // Venta / Viajes / KM
}

public sealed record FilaClientePresentacionDto(string Cliente, string VentaPendienteMotivo, int Viajes, decimal? DeltaViajesPorcentaje);

/// <summary>Slide 5. Por cliente: Venta+Δ% pendiente, Viajes+Δ% funcional (mismos datos que ResumenPorCliente, Bloque 8.3).</summary>
public sealed record SlidePorClienteDto(IReadOnlyList<FilaClientePresentacionDto> Filas)
{
    // Texto fijo del HTML (L1706), no depende de Venta -- se muestra siempre.
    public const string NotaFija = "La señal: quien vende más con menos viajes está cambiando su mezcla — siguiente lámina.";

    public int PasosRevelado => Filas.Count + 1; // una fila a la vez + la nota final
}

public sealed record SlideMezclaExpedicionDto(
    int Comodato,
    int Full,
    int Sencillo,
    int Total,
    decimal PctComodato,
    decimal? DeltaPuntosPorcentuales,
    string NotaCierrePendienteMotivo)
{
    public int PasosRevelado => 2; // mezcla por conteo / nota de cierre (Pendiente)
}

public sealed record SlideFugasDto(
    string CaidaDestinoPendienteMotivo,
    int AgenciasDesaparecidas,
    string VentaAcumuladaPendienteMotivo)
{
    public const string NotaFija = "Ese volumen no desapareció del mercado: si no lo movemos nosotros, lo mueve otro transportista.";

    public int PasosRevelado => 3; // destinos (Pendiente) / agencias (funcional) / nota final
}

/// <summary>Slide 8. 100% texto estático del HTML (L1718-1722, estos 4 párrafos son literales, no se resumen ni se reformulan).</summary>
public sealed record SlideQueSigueDto
{
    public static readonly string[] Parrafos =
    [
        "1 · Revisar con el cliente, agencia por agencia, las 10 de mayor venta perdida — primeras 2 semanas.",
        "2 · Mesa sobre la mezcla: recuperar full o repreciar sencillo.",
        "3 · Este monitoreo en cada corte mensual: agencia que entra a la lista, agencia que se atiende en la semana.",
    ];

    public const string Nota = "El detalle por sucursal, destino y operador está en la herramienta para cualquier pregunta.";

    public int PasosRevelado => Parrafos.Length + 1;
}

/// <summary>Los 8 slides, en el orden exacto de PR_build. TotalSlides existe para no repetir "8" en la UI.</summary>
public sealed record PresentacionResumenDto(
    SlideEstadoOperacionDto EstadoOperacion,
    SlideTresCifrasDto TresCifras,
    SlideAnioVistazoDto AnioVistazo,
    SlideComparativoMensualDto ComparativoMensual,
    SlidePorClienteDto PorCliente,
    SlideMezclaExpedicionDto MezclaExpedicion,
    SlideFugasDto Fugas,
    SlideQueSigueDto QueSigue)
{
    public const int TotalSlides = 8;
}