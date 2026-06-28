namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// Computes exact total cost for a given PortfolioAllocation.
///
/// Formula matches AdvancedAnalysis step-by-step:
///   1. Distribute market energy across TOU bands by ratio
///   2. Residual grid = max(Q_b - Market_b, 0) per band
///   3. EnergyAfter  = Σ Residual_b × tariff_b
///   4. Article16After = max((GreenSubject - GreenKwh) × article16Diff, 0)
///   5. CostWith = EnergyAfter + Article16After + ExchangeBill + GreenBill + BilateralBill
///
/// Credit (Model A) is zero under energy-balance constraint (Grid ≥ 0),
/// because Market ≤ TotalConsumption → no band over-purchase → Credit = 0.
/// </summary>
public static class CostEvaluator
{
    public static CostBreakdown Evaluate(PortfolioOptimizationInput inp, PortfolioAllocation alloc)
    {
        decimal article16Diff = Math.Max(inp.GreenLawRate - inp.TariffMid, 0m);

        // ── Distribute market energy across TOU bands ────────────────────────
        decimal totalMarket = alloc.TotalMarketKwh;
        decimal mktPeak     = totalMarket * inp.PeakRatio;
        decimal mktMid      = totalMarket * inp.MidRatio;
        decimal mktLow      = totalMarket * inp.LowRatio;

        // ── Residual from grid ───────────────────────────────────────────────
        decimal remPeak = Math.Max(inp.PeakKwh - mktPeak, 0m);
        decimal remMid  = Math.Max(inp.MidKwh  - mktMid,  0m);
        decimal remLow  = Math.Max(inp.LowKwh  - mktLow,  0m);

        // ── Energy costs ─────────────────────────────────────────────────────
        decimal energyBefore = inp.PeakKwh * inp.TariffPeak
                             + inp.MidKwh  * inp.TariffMid
                             + inp.LowKwh  * inp.TariffLow;

        decimal energyAfter  = remPeak * inp.TariffPeak
                             + remMid  * inp.TariffMid
                             + remLow  * inp.TariffLow;

        // ── Article 16 ───────────────────────────────────────────────────────
        decimal article16Before = inp.GreenSubjectKwh * Math.Max(article16Diff, 0m);
        decimal article16After  = Math.Max(
            (inp.GreenSubjectKwh - alloc.GreenKwh) * article16Diff, 0m);

        // ── Market bills ─────────────────────────────────────────────────────
        decimal exchangeBill  = alloc.ExchangeKwh  * inp.ExchangeRate;
        decimal greenBill     = alloc.GreenKwh     * inp.GreenRate;
        decimal bilateralBill = alloc.BilateralKwh * inp.BilateralRate;

        decimal costWithout = energyBefore + article16Before;
        decimal costWith    = energyAfter + article16After
                            + exchangeBill + greenBill + bilateralBill;

        return new CostBreakdown
        {
            EnergyBefore    = energyBefore,
            Article16Before = article16Before,
            CostWithout     = costWithout,
            EnergyAfter     = energyAfter,
            Article16After  = article16After,
            ExchangeBill    = exchangeBill,
            GreenBill       = greenBill,
            BilateralBill   = bilateralBill,
            CostWith        = costWith,
            NetSaving       = costWithout - costWith,
        };
    }

    /// <summary>Baseline cost — zero market purchases.</summary>
    public static CostBreakdown Baseline(PortfolioOptimizationInput inp) =>
        Evaluate(inp, new PortfolioAllocation
        {
            ExchangeKwh  = 0m,
            GreenKwh     = 0m,
            BilateralKwh = 0m,
            GridKwh      = inp.TotalKwh,
        });
}
