using System;
using System.Collections.Generic;

namespace Testing.Infrastructure.Persistence.Zam;

public partial class TraficoCombustible
{
    public int IdArea { get; set; }

    public int IdCombustible { get; set; }

    public string NoConsecutivo { get; set; } = null!;

    public int NoViaje { get; set; }

    public int IdProveedor { get; set; }

    public decimal CantidadComb { get; set; }

    public string TipoComb { get; set; } = null!;

    public decimal MontoComb { get; set; }

    public string StatusDocto { get; set; } = null!;

    public string? IdIngreso { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public decimal? CantSaldocomb { get; set; }

    public DateTime? FechaContabilizado { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public decimal? Iva { get; set; }

    public string? Observaciones { get; set; }

    public int? TipoCarga { get; set; }

    public DateTime? FechaDoc { get; set; }

    public string? NoPoliza { get; set; }

    public int? IdCombfact { get; set; }

    public int? Almacen { get; set; }

    public decimal FactorIva { get; set; }

    public int IdIva { get; set; }

    public string? IdUnidad { get; set; }

    public int? LocalForaneo { get; set; }

    public int IdOperador { get; set; }

    public int? IdPago { get; set; }

    public int? NoLiquidacion { get; set; }

    public int? AreaLiq { get; set; }

    public string? NumFact { get; set; }

    public decimal MontoIeps { get; set; }

    public string? IdModifico { get; set; }

    public string? IdAutorizo { get; set; }

    public DateTime? FechaModifico { get; set; }

    public DateTime? FechaAutorizo { get; set; }

    public int? KmsFinales { get; set; }

    public int RebasaPresupuesto { get; set; }

    public int TipoAutorizacion { get; set; }

    public int IdCompania { get; set; }

    public int Impreso { get; set; }

    public int Foraneo { get; set; }

    public int IdAsignacion { get; set; }

    public decimal MontoPrecio { get; set; }

    public int KmsActuales { get; set; }

    public int? IdAreaviaje { get; set; }

    public int? IdPlazaorigen { get; set; }

    public string? IdCancelo { get; set; }

    public DateTime? FechaCancelo { get; set; }

    public int? KmsActualesvale { get; set; }

    public int? KmsRecorridosvale { get; set; }

    public int? KmsHorometro { get; set; }

    public decimal? RendReal { get; set; }

    public int? ReinicioOdometro { get; set; }

    public int? KmsIni { get; set; }

    public int? KmsFin { get; set; }

    public decimal? LtsCarga { get; set; }

    public decimal? DifLts { get; set; }

    public decimal? RendBase { get; set; }

    public decimal? RendComputadora { get; set; }

    public decimal? Bonificacion { get; set; }

    public decimal? Descto { get; set; }

    public decimal LtsForaneos { get; set; }

    public decimal LtsTolerancia { get; set; }

    public decimal? LtsReales { get; set; }

    public decimal DescuentoLts { get; set; }

    public decimal BonificacionLts { get; set; }

    public string? TipoValeConvenio { get; set; }

    public string? IdIngresoValext { get; set; }

    public decimal? PrecioXLto { get; set; }

    public decimal? LitrosAutorizados { get; set; }

    public int? KmsUnidad { get; set; }

    public int Tlleno { get; set; }

    public decimal CantCargado { get; set; }

    public string? OdometroCarga { get; set; }

    public DateTime? FechaCarga { get; set; }

    public int StatusCarga { get; set; }

    public int IdGasolinera { get; set; }

    public int Recarga { get; set; }

    public string? FolioEnergex { get; set; }

    public decimal? CantidadCombHist { get; set; }

    public int? IdMultiEmpresa { get; set; }

    public int? IdConciliacion { get; set; }

    public string? NoFactura { get; set; }

    public decimal? CantidadComb2 { get; set; }

    public decimal? MontoComb2 { get; set; }

    public decimal? MontoPrecio2 { get; set; }

    public int? IdAreaCaptura { get; set; }

    public int? IdAreaPreliq { get; set; }

    public int? NoPreliq { get; set; }

    public decimal? KmRecEcm { get; set; }

    public decimal? LtConsEcm { get; set; }

    public decimal? TiempoViaje { get; set; }

    public decimal? TiempoManejando { get; set; }

    public decimal? LitrosManejando { get; set; }

    public decimal? FactorCarga { get; set; }

    public decimal? Coast { get; set; }

    public decimal? RpmMax { get; set; }

    public decimal? ConteoFrenos { get; set; }

    public decimal? VelProm { get; set; }

    public decimal? LtRelanti { get; set; }

    public decimal? CntrlCruceroKm { get; set; }

    public decimal? TopGearKm { get; set; }

    public decimal? GearDownKm { get; set; }

    public decimal PrecioFactura { get; set; }

    public decimal IvaFactura { get; set; }

    public decimal MontoIepsFactura { get; set; }

    public decimal TotalCombustibleFactura { get; set; }

    public decimal SubtotalCombustibleFactura { get; set; }

    public int? TieneComision { get; set; }

    public int? Odometro { get; set; }

    public decimal? RendRealvale { get; set; }

    public decimal? DieselReseteo { get; set; }

    public decimal? DifLtsvale { get; set; }

    public decimal? RendManejo { get; set; }

    public decimal? PresionPedal { get; set; }

    public decimal? PorcManejoBaja { get; set; }

    public decimal? LtsBaja { get; set; }

    public decimal? VelMaxima { get; set; }

    public int? HrsBajaH { get; set; }

    public int? HrsBajaM { get; set; }

    public decimal Pto { get; set; }

    public string EcmPath { get; set; } = null!;
}
