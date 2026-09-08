using System;
using System.Collections.Generic;

namespace Testing.Infrastructure.Persistence.CIS_DB;

public partial class ZemogViajesEnZamAnual
{
    public int Identificador { get; set; }

    public int NoViaje { get; set; }

    public string Sucursal { get; set; } = null!;

    public string Unidad { get; set; } = null!;

    public DateTime CitaCarga { get; set; }

    public string Remolque1 { get; set; } = null!;

    public string? Remolque2 { get; set; }

    public string? Dolly { get; set; }

    public string Ruta { get; set; } = null!;

    public string CodigoRuta { get; set; } = null!;

    public string Origen { get; set; } = null!;

    public string Destino { get; set; } = null!;

    public string EstadoOrigen { get; set; } = null!;

    public string EstadoDestino { get; set; } = null!;

    public string Folio { get; set; } = null!;

    public string? FolioComplemento { get; set; }

    public string CartaPorte { get; set; } = null!;

    public int Kms { get; set; }

    public int NoGuia { get; set; }

    public decimal TotalVenta { get; set; }

    public string Expedicion { get; set; } = null!;

    public bool Adicional { get; set; }

    public int IdOperador1 { get; set; }

    public string Operador1 { get; set; } = null!;

    public decimal TotalViaticos { get; set; }

    public string ConceptosDispension { get; set; } = null!;

    public string Trayecto { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public int? IdOperador2 { get; set; }

    public string? Operador2 { get; set; }

    public string? Factura { get; set; }

    public decimal MontoPeajeIave { get; set; }

    public decimal MontoPeajeEfectivo { get; set; }

    public DateTime? FechaConfirmacionViaticos { get; set; }

    public DateTime? FechaLiquidacion { get; set; }

    public bool RutaJdv { get; set; }

    public bool RutaMov { get; set; }

    public string EstatusAsignacion { get; set; } = null!;

    public string Operacion { get; set; } = null!;

    public int IdArea { get; set; }

    public DateOnly FechaCalendario { get; set; }

    public int IdSucursal { get; set; }

    public bool? RutaCircuito { get; set; }

    public int? TotalCasetas { get; set; }

    public string? Casetas { get; set; }

    public int? EjesEquipos { get; set; }

    public decimal? ComisionRuta { get; set; }

    public decimal? Rendimiento { get; set; }

    public DateTime? FechaTimbrado { get; set; }

    public decimal? CostoLitroDiesel { get; set; }

    public decimal? ListosDieselViaje { get; set; }

    public decimal? ComisionOp1 { get; set; }

    public decimal? ComisionOp2 { get; set; }

    public decimal? CompensacionOp1 { get; set; }

    public decimal? CompensacionOp2 { get; set; }

    public string? Tipo { get; set; }

    public virtual Sucursale IdSucursalNavigation { get; set; } = null!;
}
