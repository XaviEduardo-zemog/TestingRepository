using System;
using System.Collections.Generic;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class RutasZam
{
    public string Ruta { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public decimal Kms { get; set; }

    public string Origen { get; set; } = null!;

    public string Destino { get; set; } = null!;
}
