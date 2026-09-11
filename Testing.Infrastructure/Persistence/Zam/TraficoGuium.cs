using System;
using System.Collections.Generic;

namespace Testing.Infrastructure.Persistence.Zam;

public partial class TraficoGuium
{
    public int IdArea { get; set; }

    public int NoGuia { get; set; }

    public string TipoPago { get; set; } = null!;

    public string StatusGuia { get; set; } = null!;

    public string TipoCobro { get; set; } = null!;

    public int ClasificacionDoc { get; set; }

    public DateTime FechaGuia { get; set; }

    public DateTime? FechaConfirmacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public DateTime? FechaPago { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public decimal? ValorUnitario { get; set; }

    public decimal? ConvenidoTonelada { get; set; }

    public decimal ConvOperador { get; set; }

    public decimal ConvPermisionario { get; set; }

    public int? IdAreaFacturacion { get; set; }

    public int IdCliente { get; set; }

    public int IdPersonal { get; set; }

    public int NoViaje { get; set; }

    public int IdRemitente { get; set; }

    public int IdDestinatario { get; set; }

    public int? IdCondujo { get; set; }

    public int? IdOperador2 { get; set; }

    public int KmsGuia { get; set; }

    public decimal Flete { get; set; }

    public decimal Seguro { get; set; }

    public decimal Maniobras { get; set; }

    public decimal Autopistas { get; set; }

    public decimal Otros { get; set; }

    public decimal Subtotal { get; set; }

    public decimal IvaGuia { get; set; }

    public decimal? CobroViajeKms { get; set; }

    public decimal MontoTipoCambio { get; set; }

    public int TipoDoc { get; set; }

    public int IdOrigen { get; set; }

    public int IdDestino { get; set; }

    public int IdFraccion { get; set; }

    public int PlazaEmision { get; set; }

    public int ConducirDe { get; set; }

    public int ConducirA { get; set; }

    public int IdTipoMoneda { get; set; }

    public string NumGuia { get; set; } = null!;

    public string? NoCarta { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? IdRemolque1 { get; set; }

    public DateTime? FechaContabilizado { get; set; }

    public string? IdUnidad { get; set; }

    public string? RecogerEn { get; set; }

    public string? EntregarEn { get; set; }

    public string? ValorDeclarado { get; set; }

    public string? ObservacionesGuia { get; set; }

    public string? NoRemision { get; set; }

    public string? MotivoCancelacion { get; set; }

    public string? ObsCobranza { get; set; }

    public string? IdRemolque2 { get; set; }

    public string Prestamo { get; set; } = null!;

    public string? NumGuiaAsignado { get; set; }

    public int? NoDeposito { get; set; }

    public string? IdIngreso { get; set; }

    public string? Unidadplaca { get; set; }

    public string? Unidadtipo { get; set; }

    public string? Rem1placa { get; set; }

    public string? Rem1tipo { get; set; }

    public string? Rem2placa { get; set; }

    public string? Rem2tipo { get; set; }

    public string? Personalnombre { get; set; }

    public string? TipoFacturacion { get; set; }

    public int? NoTransferenciaCobranza { get; set; }

    public decimal MontoNcredito { get; set; }

    public decimal MontoNcargo { get; set; }

    public decimal MontoPago { get; set; }

    public decimal FactorIva { get; set; }

    public string? NumGuiacancel { get; set; }

    public int CantMovguia { get; set; }

    public int IdSerieguia { get; set; }

    public decimal MontoIvancredito { get; set; }

    public decimal MontoIvancargo { get; set; }

    public decimal MontoComisiontercero { get; set; }

    public string? IdLinearem1 { get; set; }

    public int? IdPago { get; set; }

    public int TipoOrigen { get; set; }

    public string? NoPoliza { get; set; }

    public decimal MontoRetencion { get; set; }

    public string? StatusPago { get; set; }

    public decimal MontoRetenciontercero { get; set; }

    public string? ControlPago { get; set; }

    public string? SustituyeDocumento { get; set; }

    public string? Campo1 { get; set; }

    public string? Campo2 { get; set; }

    public string? IdLinearem2 { get; set; }

    public string? Campo3 { get; set; }

    public string? Campo4 { get; set; }

    public int? IdTercero { get; set; }

    public int? IdSector { get; set; }

    public int? IdSubsector { get; set; }

    public string? Campo5 { get; set; }

    public string? Campo6 { get; set; }

    public string? Campo7 { get; set; }

    public string? TipoMovimiento { get; set; }

    public int? TipoViaje { get; set; }

    public int? TipoPago02 { get; set; }

    public decimal MontoIvaflete { get; set; }

    public int? IdAreapagoperm { get; set; }

    public int Tipocambioconvenio { get; set; }

    public decimal DescFlete { get; set; }

    public decimal FleteBruto { get; set; }

    public int IdIva { get; set; }

    public int IdRetencion { get; set; }

    public int LocalForaneo { get; set; }

    public int IdMonedaPermisionario { get; set; }

    public decimal TipoCambioPermisionario { get; set; }

    public decimal ConvPermisionarioorigen { get; set; }

    public int? IdConvenio { get; set; }

    public int? IdAreaconvenio { get; set; }

    public decimal? MontoTipoCambioDlls { get; set; }

    public string? TipoServ { get; set; }

    public int? IdTipoOperacion { get; set; }

    public int? IdVendedor { get; set; }

    public int IdSeguro { get; set; }

    public string? NoKit { get; set; }

    public decimal MontoIvadescto { get; set; }

    public decimal MontoRetdescto { get; set; }

    public decimal MontoDescto { get; set; }

    public int? TipoDetalle { get; set; }

    public string? ValeCarga { get; set; }

    public int IdSerieguia2 { get; set; }

    public int? SucursalPosicion { get; set; }

    public string ClavePorteador { get; set; } = null!;

    public int PeriodoFacturacion { get; set; }

    public string NumeroRelacion { get; set; } = null!;

    public string TipoProducto { get; set; } = null!;

    public string CedulaPago { get; set; } = null!;

    public string NumeroContrato { get; set; } = null!;

    public string RegistroEstatal { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public string Medida { get; set; } = null!;

    public string Campo8 { get; set; } = null!;

    public string Campo9 { get; set; } = null!;

    public int KmsCpac { get; set; }

    public int TipoCpac { get; set; }

    public decimal FactorCpac { get; set; }

    public int? NoTransferencia { get; set; }

    public decimal? TipoCambioOperador { get; set; }

    public DateTime? FechaDocumentador { get; set; }

    public DateTime? FechaFacturacion { get; set; }

    public int OrdenFact { get; set; }

    public int? IdFactura { get; set; }

    public decimal? MontoFaltante { get; set; }

    public string? NumGuiaIncentivo { get; set; }

    public int? Incentivo { get; set; }

    public int IdCompania { get; set; }

    public int Facturado { get; set; }

    public string IdModifico { get; set; } = null!;

    public DateTime? FechaModifico { get; set; }

    public int IdContrato { get; set; }

    public DateTime? FCarfinProg { get; set; }

    public string? NumVapor { get; set; }

    public string? Bl { get; set; }

    public string? NumOrden { get; set; }

    public int? KmsConvenio { get; set; }

    public string? Zona { get; set; }

    public decimal Ieps { get; set; }

    public decimal SubtotalIeps { get; set; }

    public string? Pedimento { get; set; }

    public int? PagoParcialidades { get; set; }

    public int? IdTipoFactura { get; set; }

    public int? Castigo { get; set; }

    public int? BanderaFletefracion { get; set; }

    public string? ContratoNo { get; set; }

    public DateTime? ContratoFechaIni { get; set; }

    public DateTime? ContratoFechaFin { get; set; }

    public string? ContratoEstimacion { get; set; }

    public decimal? Cpac { get; set; }

    public decimal? Cruces { get; set; }

    public decimal? MontoAplicadoCxp { get; set; }

    public int IdAduanal { get; set; }

    public int? IdDepositoFactoraje { get; set; }

    public int? IdMultiEmpresa { get; set; }

    public int? IdAreaViaje { get; set; }

    public string? SustituidoPor { get; set; }
}
