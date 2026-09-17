namespace Testing.Application.GetAllViajes;

public sealed record SlideEstadoOperacionDto(string TextoCorte)
{
    public int PasosRevelado => 1;
}

/// <summary>Slide 2. ViajesYtd/KmsYtd/VentaYtd son decimal -- TotalesPeriodo.Viajes ya es decimal (proyectado con factorMes).</summary>
public sealed record SlideTresCifrasDto(
    string Periodo,
    decimal VentaYtd,
    decimal ViajesYtd,
    decimal KmsYtd,
    (string MesEtiqueta, decimal Venta)? MejorMes,
    (string MesEtiqueta, decimal Venta)? PeorMes)
{
    public int PasosRevelado => 3; // Venta / Viajes / KM, uno a la vez -- igual que 3 <div class="pkpi">
}

/// <summary>Slide 3. Venta mensual, datos reales (RE_tendencia calcula la línea de tendencia real, ver ResumenPresentacion.razor).</summary>
public sealed record SlideAnioVistazoDto(IReadOnlyList<(string MesEtiqueta, decimal Venta)> VentaPorMes)
{
    public int PasosRevelado => 1;
}

/// <summary>Slide 4. Mes actual (ya excluye el mes de avance si corresponde -- ver SlidesPresentacionCalculator.PrepararParaPresentacion) contra mes anterior.</summary>
public sealed record SlideComparativoMensualDto(
    string MesActual,
    string MesAnterior,
    decimal VentaActual,
    decimal VentaAnterior,
    decimal? DeltaVentaPorcentaje,
    decimal ViajesActual,
    decimal ViajesAnterior,
    decimal? DeltaViajesPorcentaje,
    decimal KmsActual,
    decimal KmsAnterior,
    decimal? DeltaKmsPorcentaje)
{
    public int PasosRevelado => 3; // Venta / Viajes / KM
}

public sealed record FilaClientePresentacionDto(string Cliente, decimal Venta, decimal? DeltaVentaPorcentaje, decimal Viajes, decimal? DeltaViajesPorcentaje);

public sealed record SlidePorClienteDto(IReadOnlyList<FilaClientePresentacionDto> Filas)
{
    public const string NotaFija = "La señal: quien vende más con menos viajes está cambiando su mezcla — siguiente lámina.";

    public int PasosRevelado => Filas.Count + 1;
}

public sealed record SlideMezclaExpedicionDto(
    decimal Comodato,
    decimal Full,
    decimal Sencillo,
    decimal Total,
    decimal? PctComodato,
    decimal? DeltaPuntosPorcentuales,
    decimal VentaPorViajeFull,
    decimal VentaPorViajeSencillo)
{
    public int PasosRevelado => 2;
}

public sealed record SlideFugasDto(
    (string Destino, decimal VentaPerdida)? PeorDestinoCayendo,
    int AgenciasDesaparecidas,
    decimal VentaAcumuladaPerdida)
{
    public const string NotaFija = "Ese volumen no desapareció del mercado: si no lo movemos nosotros, lo mueve otro transportista.";

    public int PasosRevelado => 3;
}

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

/// <summary>Los 8 slides, en el orden exacto de PR_build.</summary>
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