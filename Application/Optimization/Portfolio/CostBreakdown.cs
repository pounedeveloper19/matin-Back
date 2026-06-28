namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// Full cost breakdown for a given PortfolioAllocation.
/// Mirrors AdvancedAnalysis formula exactly — single source of truth via CostEvaluator.
/// </summary>
public record CostBreakdown
{
    // ── Before Matin (baseline — no market purchases) ────────────────────────
    public decimal EnergyBefore    { get; init; }   // Σ Q_b × tariff_b
    public decimal Article16Before { get; init; }   // GreenSubject × max(greenLawRate - tariffMid, 0)
    public decimal CostWithout     { get; init; }   // = EnergyBefore + Article16Before

    // ── After Matin (with optimal market mix) ────────────────────────────────
    public decimal EnergyAfter     { get; init; }   // Σ Residual_b × tariff_b
    public decimal Article16After  { get; init; }   // max(GreenSubject - GreenKwh, 0) × article16Diff
    public decimal ExchangeBill    { get; init; }   // ExchangeKwh × ExchangeRate
    public decimal GreenBill       { get; init; }   // GreenKwh × GreenRate
    public decimal BilateralBill   { get; init; }   // BilateralKwh × BilateralRate
    public decimal CostWith        { get; init; }   // = EnergyAfter + Article16After + bills

    // ── Results ──────────────────────────────────────────────────────────────
    public decimal NetSaving     { get; init; }   // = CostWithout - CostWith
    public decimal SavingPercent => CostWithout > 0
        ? Math.Round(NetSaving / CostWithout * 100m, 2) : 0m;
}
