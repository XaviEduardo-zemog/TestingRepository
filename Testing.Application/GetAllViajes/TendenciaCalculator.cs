namespace Testing.Application.GetAllViajes;

public static class TendenciaCalculator
{
    public static IReadOnlyList<decimal> Calcular(IReadOnlyList<decimal> valores)
    {
        var n = valores.Count;

        if (n < 2)
            return valores.ToList();

        if (n < 4)
            return CalcularLineal(valores);

        return CalcularCuadratica(valores);
    }

    private static IReadOnlyList<decimal> CalcularLineal(IReadOnlyList<decimal> valores)
    {
        var n = valores.Count;
        double sx = 0, sy = 0, sxy = 0, sx2 = 0;

        for (var i = 0; i < n; i++)
        {
            double x = i, y = (double)valores[i];
            sx += x;
            sy += y;
            sxy += x * y;
            sx2 += x * x;
        }

        var denominador = n * sx2 - sx * sx;
        if (denominador == 0)
            return valores.ToList(); // todos los x iguales (no debería pasar con índices 0..n-1), evita división por 0

        var b = (n * sxy - sx * sy) / denominador;
        var a = (sy - b * sx) / n;

        var resultado = new List<decimal>(n);
        for (var i = 0; i < n; i++)
            resultado.Add((decimal)(a + b * i));

        return resultado;
    }

    private static IReadOnlyList<decimal> CalcularCuadratica(IReadOnlyList<decimal> valores)
    {
        var n = valores.Count;
        double s0 = n, s1 = 0, s2 = 0, s3 = 0, s4 = 0, t0 = 0, t1 = 0, t2 = 0;

        for (var i = 0; i < n; i++)
        {
            double x = i, y = (double)valores[i];
            double x2 = x * x, x3 = x2 * x, x4 = x2 * x2;
            s1 += x;
            s2 += x2;
            s3 += x3;
            s4 += x4;
            t0 += y;
            t1 += x * y;
            t2 += x2 * y;
        }

        // Matriz aumentada 3x4: [s0 s1 s2 | t0] [s1 s2 s3 | t1] [s2 s3 s4 | t2]
        var m = new double[3, 4]
        {
            { s0, s1, s2, t0 },
            { s1, s2, s3, t1 },
            { s2, s3, s4, t2 },
        };

        var coef = ResolverGauss(m);
        if (coef is null)
            return CalcularLineal(valores); // matriz singular (caso degenerado) -- cae a lineal en vez de fallar

        var (A, B, C) = (coef[0], coef[1], coef[2]);

        var resultado = new List<decimal>(n);
        for (var i = 0; i < n; i++)
            resultado.Add((decimal)(A + B * i + C * i * i));

        return resultado;
    }

    private static double[]? ResolverGauss(double[,] m)
    {
        const int filas = 3, columnas = 4;

        for (var col = 0; col < filas; col++)
        {
            var pivote = col;
            for (var fila = col + 1; fila < filas; fila++)
                if (Math.Abs(m[fila, col]) > Math.Abs(m[pivote, col]))
                    pivote = fila;

            if (Math.Abs(m[pivote, col]) < 1e-12)
                return null;

            if (pivote != col)
                for (var k = 0; k < columnas; k++)
                    (m[col, k], m[pivote, k]) = (m[pivote, k], m[col, k]);

            for (var fila = 0; fila < filas; fila++)
            {
                if (fila == col) continue;
                var factor = m[fila, col] / m[col, col];
                for (var k = col; k < columnas; k++)
                    m[fila, k] -= factor * m[col, k];
            }
        }

        return [m[0, 3] / m[0, 0], m[1, 3] / m[1, 1], m[2, 3] / m[2, 2]];
    }
}