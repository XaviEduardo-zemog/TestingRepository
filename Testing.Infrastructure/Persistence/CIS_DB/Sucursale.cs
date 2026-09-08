using System;
using System.Collections.Generic;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class Sucursale
{
    public int IdSucursal { get; set; }

    public string Nombre { get; set; } = null!;

    public string Nomenclatura { get; set; } = null!;

    public int? IdOperacion { get; set; }

    public int? Clave { get; set; }

    public int? IdEspecialidad { get; set; }

    public int? EstatusSucursal { get; set; }

    public string? NombreCorto { get; set; }

    public string? Region { get; set; }

    public string? IdAreaZam { get; set; }

    public int? Orden { get; set; }

    public int? NewOrden { get; set; }

    public virtual ICollection<ZemogViajesEnZamAnual> ZemogViajesEnZamAnuals { get; set; } = new List<ZemogViajesEnZamAnual>();
}
