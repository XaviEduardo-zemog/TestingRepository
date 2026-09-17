namespace Testing.Application.GetAllViajes;

/// <summary>
/// Cálculo de los meses presentes en los datos y del bloque de totales/tendencia por nivel (Zemog
/// o por Cliente) -- extraído de ResumenEjecutivoCalculator en la Etapa Clean H
/// (docs/ETAPA_CLEAN_H_DIVIDIR_RESUMEN_CALCULATOR.md). Mismo namespace que
/// ResumenEjecutivoCalculator (Testing.Application.GetAllViajes) a propósito: solo cambia la
/// ubicación física del archivo, no la resolución de tipos en ningún consumidor. internal porque
/// solo ResumenEjecutivoCalculator (mismo ensamblado) lo llama -- ningún .razor, Word ni test
/// invoca estos métodos directamente.
/// </summary>
internal static class ResumenBloqueNivelCalculator
{
    private static readonly string[] NombresMes =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    // ---------- Meses presentes en los datos (SIN exclusión -- ver nota de clase original) ----------

    internal static List<MesCerrado> CalcularMeses(IReadOnlyList<ViajesDto> viajes, Func<ViajesDto, DateTime?> fechaDe) =>
        viajes
            .Select(fechaDe)
            .Where(f => f is not null)
            .Select(f => (Anio: f!.Value.Year, Mes: f.Value.Month))
            .Distinct()
            .OrderBy(m => m.Anio).ThenBy(m => m.Mes)
            .Select(m => new MesCerrado(m.Anio, m.Mes, EtiquetaMes(m.Anio, m.Mes)))
            .ToList();

    private static string EtiquetaMes(int anio, int mes) => $"{NombresMes[mes - 1]} {anio}";

    internal static bool EstaEnMeses(ViajesDto v, IReadOnlyList<MesCerrado> meses, Func<ViajesDto, DateTime?> fechaDe)
    {
        var fecha = fechaDe(v);
        return fecha is not null && meses.Any(m => m.Anio == fecha.Value.Year && m.Mes == fecha.Value.Month);
    }

    // ---------- Bloques 8.2/8.3 — Nivel general / Por Cliente (replica RE_bloqueNivel) ----------

    internal static BloqueNivelDto CalcularBloqueNivel(string titulo, IReadOnlyList<ViajesDto> viajes, IReadOnlyList<MesCerrado> meses, CorteMensual? corte, Func<ViajesDto, DateTime?> fechaDe)
    {
        var mesPorClave = meses.ToDictionary(m => (m.Anio, m.Mes));
        var totalesPorMes = meses.ToDictionary(m => m, _ => TotalesPeriodo.Vacio);

        foreach (var v in viajes)
        {
            var fecha = fechaDe(v);
            if (fecha is null || !mesPorClave.TryGetValue((fecha.Value.Year, fecha.Value.Month), out var claveMes))
                continue;

            var mes = meses.First(m => m.Anio == claveMes.Anio && m.Mes == claveMes.Mes);
            totalesPorMes[mes] = TotalesPeriodo.Sumar(totalesPorMes[mes], TotalesPeriodo.De(v, corte));
        }

        var ultimo = meses[^1];
        var anterior = meses.Count > 1 ? meses[^2] : (MesCerrado?)null;
        var primerMes = meses[0];

        return new BloqueNivelDto(
            titulo, anterior, anterior is null ? null : totalesPorMes[anterior.Value],
            ultimo, totalesPorMes[ultimo], primerMes, totalesPorMes[primerMes],
            meses.Select(m => (m, totalesPorMes[m])).ToList());
    }
}