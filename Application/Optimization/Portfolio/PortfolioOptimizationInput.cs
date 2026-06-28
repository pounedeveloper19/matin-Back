namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// All data the portfolio solver needs — pure math, no DB references.
/// Built by the controller after resolving tariff, market, and TOU data.
/// </summary>
public class PortfolioOptimizationInput
{
    // ── Consumption (kWh) ────────────────────────────────────────────────────
    public decimal TotalKwh { get; init; }
    public decimal PeakKwh  { get; init; }
    public decimal MidKwh   { get; init; }
    public decimal LowKwh   { get; init; }

    // ── TOU ratios (ratio = hours / touTotal, NOT /24) ───────────────────────
    public decimal PeakRatio { get; init; }
    public decimal MidRatio  { get; init; }
    public decimal LowRatio  { get; init; }

    // ── Customer tariff rates (ریال/kWh) ────────────────────────────────────
    public decimal TariffPeak { get; init; }   // tariffMid × 2.0
    public decimal TariffMid  { get; init; }   // base rate from DB
    public decimal TariffLow  { get; init; }   // tariffMid × 0.5

    // ── Market purchase prices (ریال/kWh) — decision inputs ─────────────────
    public decimal ExchangeRate  { get; init; }
    public decimal GreenRate     { get; init; }
    public decimal BilateralRate { get; init; }

    // ── Article 16 (قانون جهش تولید) ────────────────────────────────────────
    public decimal GreenSubjectKwh { get; init; }   // totalKwh × article16Percent
    public decimal GreenLawRate    { get; init; }   // IndustrialTariffBase from DB

    // ── Constraints (hard upper/lower bounds) ────────────────────────────────
    /// Maximum kWh purchasable from exchange in this period
    public decimal MaxExchangeKwh        { get; init; } = 0m;
    /// Maximum green energy available in market
    public decimal GreenAvailabilityKwh  { get; init; } = 0m;
    /// Maximum bilateral contract volume available
    public decimal MaxBilateralKwh       { get; init; } = 0m;
    /// Minimum kWh that MUST remain on grid (operational reserve)
    public decimal OperationalReserveKwh { get; init; } = 0m;
}
