namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// The energy mix solution produced by PortfolioSolver.
/// Satisfies: Exchange + Green + Bilateral + Grid = TotalConsumption
///            Grid >= OperationalReserve
/// </summary>
public record PortfolioAllocation
{
    public decimal ExchangeKwh  { get; init; }
    public decimal GreenKwh     { get; init; }
    public decimal BilateralKwh { get; init; }
    public decimal GridKwh      { get; init; }

    public decimal TotalMarketKwh => ExchangeKwh + GreenKwh + BilateralKwh;
    public decimal TotalKwh       => TotalMarketKwh + GridKwh;
}
