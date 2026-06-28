namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// Full output of the constrained portfolio optimizer.
/// Contains: optimal mix, cost breakdown, constraint status, and explainability reasoning.
/// </summary>
public class PortfolioOptimizationResult
{
    // ── Optimal energy mix (kWh) ─────────────────────────────────────────────
    public OptimalMix OptimalMix { get; init; } = new();

    // ── Cost breakdown (ریال) ────────────────────────────────────────────────
    public decimal TotalCost        { get; init; }   // CostWith at optimal
    public decimal BaselineCost     { get; init; }   // CostWithout (no market purchases)
    public decimal Saving           { get; init; }   // BaselineCost - TotalCost
    public decimal SavingPercent    { get; init; }   // %

    // ── Cost components (ریال) ───────────────────────────────────────────────
    public decimal ResidualGridCost { get; init; }   // EnergyAfter
    public decimal ExchangeBill     { get; init; }
    public decimal GreenBill        { get; init; }
    public decimal BilateralBill    { get; init; }
    public decimal Article16After   { get; init; }   // remaining Article16 penalty
    public decimal Article16Saved   { get; init; }   // Article16Before - Article16After

    // ── Reference rates used (ریال/kWh) ─────────────────────────────────────
    public decimal WeightedGridTariff { get; init; }  // WT — effective grid rate per kWh
    public decimal Article16Benefit   { get; init; }  // A16 — avoidance benefit per green kWh

    // ── Constraint status ────────────────────────────────────────────────────
    public List<string> ConstraintHits { get; init; } = [];  // which limits were hit

    // ── Explainability layer ─────────────────────────────────────────────────
    public List<ChannelDecision> Reasoning   { get; init; } = [];
    public List<ConstraintStep>  SolverTrace { get; init; } = [];  // full solver steps
}

/// <summary>
/// The recommended energy mix with quantities and per-channel cost.
/// </summary>
public class OptimalMix
{
    public decimal ExchangeKwh  { get; init; }
    public decimal GreenKwh     { get; init; }
    public decimal BilateralKwh { get; init; }
    public decimal GridKwh      { get; init; }

    public decimal ExchangeCost  { get; init; }
    public decimal GreenCost     { get; init; }
    public decimal BilateralCost { get; init; }
    public decimal GridCost      { get; init; }

    public decimal TotalMarketKwh => ExchangeKwh + GreenKwh + BilateralKwh;
}

/// <summary>
/// One channel's allocation decision with human-readable explanation.
/// </summary>
public class ChannelDecision
{
    /// <summary>"exchange" | "green" | "bilateral" | "grid"</summary>
    public string  Channel      { get; init; } = "";
    public string  ChannelName  { get; init; } = "";   // فارسی

    public bool    IsActive     { get; init; }
    public decimal KwhAllocated { get; init; }
    public decimal CostRial     { get; init; }

    /// <summary>Why this channel was chosen or rejected.</summary>
    public string  Message      { get; init; } = "";

    /// <summary>Rate comparison used to make the decision (ریال/kWh).</summary>
    public decimal? ChannelRate    { get; init; }
    public decimal? GridSavingRate { get; init; }   // what grid would have cost per kWh
}
