namespace MatinPower.Server.Application.Optimization.Portfolio;

/// <summary>
/// Constrained energy portfolio optimizer.
///
/// ═══════════════════════════════════════════════════════════
/// MATHEMATICAL FOUNDATION
/// ═══════════════════════════════════════════════════════════
///
/// Objective: Minimize TotalCost(E, G, B)
///
///   TotalCost = EnergyAfter + Article16After
///             + E·E_r + G·G_r + B·B_r
///
/// After substituting Grid = Total - E - G - B and distributing
/// market energy across TOU bands by ratio, EnergyAfter becomes:
///
///   EnergyAfter = EnergyBefore - (E+G+B)·WT
///
///   where WT = (Q_peak·r_peak + Q_mid·r_mid + Q_low·r_low) / Q_total
///              (weighted average tariff per kWh)
///
/// So the objective reduces to:
///
///   Minimize: EnergyBefore                      ← constant
///           - (E+G+B)·WT                        ← cost avoidance
///           + E·E_r + G·G_r + B·B_r             ← market purchase cost
///           + Article16After(G)                  ← piecewise in G
///
/// ═══════════════════════════════════════════════════════════
/// MARGINAL BENEFIT per kWh (= how much cost is reduced)
/// ═══════════════════════════════════════════════════════════
///
///   Exchange:            benefit = WT - E_r
///   Green (G ≤ Subject): benefit = WT + A16 - G_r   ← Article16 avoidance bonus
///   Green (G > Subject): benefit = WT - G_r
///   Bilateral:           benefit = WT - B_r
///
///   where A16 = max(greenLawRate - tariffMid, 0)
///
/// ═══════════════════════════════════════════════════════════
/// PROOF OF OPTIMALITY
/// ═══════════════════════════════════════════════════════════
///
/// The objective is SEPARABLE (sum of independent per-channel functions)
/// with a shared BUDGET constraint (E + G + B ≤ Total - Reserve).
/// This is exactly the Fractional Knapsack problem.
/// Greedy-by-benefit-density is provably optimal for fractional knapsack
/// (Theorem: Dantzig 1957, generalized LP duality).
///
/// Green has two "items" (segments) because its marginal benefit
/// changes at G = GreenSubject (Article16 avoidance ends there).
///
/// ═══════════════════════════════════════════════════════════
/// CONSTRAINTS
/// ═══════════════════════════════════════════════════════════
///   E + G + B + Grid  = TotalKwh          (energy balance)
///   0 ≤ E             ≤ MaxExchangeKwh
///   0 ≤ G             ≤ GreenAvailability
///   0 ≤ B             ≤ MaxBilateralKwh
///   Grid              ≥ OperationalReserve
/// </summary>
public static class PortfolioSolver
{
    private record BenefitSlot(
        string  Channel,
        decimal Capacity,
        decimal BenefitPerKwh,
        bool    IsGreen1         // true = green below GreenSubject (Article16 bonus applies)
    );

    public static SolverOutput Solve(PortfolioOptimizationInput inp)
    {
        var steps       = new List<ConstraintStep>();
        var violations  = new List<string>();

        // ── Guard: no consumption ────────────────────────────────────────────
        if (inp.TotalKwh <= 0m)
            return SolverOutput.Infeasible("مصرف کل صفر است");

        // ── Guard: reserve exceeds total ─────────────────────────────────────
        if (inp.OperationalReserveKwh > inp.TotalKwh)
            return SolverOutput.Infeasible(
                $"رزرو عملیاتی ({inp.OperationalReserveKwh:N0} kWh) از کل مصرف ({inp.TotalKwh:N0} kWh) بیشتر است");

        // ── Step 1: Weighted average tariff (WT) ─────────────────────────────
        // WT = effective cost saved per kWh replaced from grid
        decimal WT = inp.TotalKwh > 0m
            ? (inp.PeakKwh * inp.TariffPeak + inp.MidKwh * inp.TariffMid + inp.LowKwh * inp.TariffLow)
              / inp.TotalKwh
            : inp.TariffMid;

        decimal A16 = Math.Max(inp.GreenLawRate - inp.TariffMid, 0m);

        steps.Add(new ConstraintStep
        {
            Name        = "weighted_tariff",
            Description = $"نرخ موثر شبکه (WT): {WT:N0} ریال/kWh — "
                        + $"ماده ۱۶ صرفه: {A16:N0} ریال/kWh",
            Value       = WT,
        });

        // ── Step 2: Market budget (how much can come from market) ────────────
        decimal budget = inp.TotalKwh - inp.OperationalReserveKwh;

        steps.Add(new ConstraintStep
        {
            Name        = "market_budget",
            Description = $"بودجه بازار: {budget:N0} kWh "
                        + $"(کل {inp.TotalKwh:N0} − رزرو {inp.OperationalReserveKwh:N0})",
            Value       = budget,
        });

        // ── Step 3: Build benefit slots ──────────────────────────────────────
        var slots = new List<BenefitSlot>();

        // Green Slot 1: up to GreenSubject (Article16 avoidance bonus included)
        if (inp.GreenSubjectKwh > 0m && inp.GreenRate > 0m && inp.GreenAvailabilityKwh > 0m)
        {
            decimal cap  = Math.Min(inp.GreenSubjectKwh, inp.GreenAvailabilityKwh);
            decimal ben  = WT + A16 - inp.GreenRate;
            slots.Add(new BenefitSlot("green", cap, ben, IsGreen1: true));
        }

        // Exchange
        if (inp.ExchangeRate > 0m && inp.MaxExchangeKwh > 0m)
        {
            decimal cap = inp.MaxExchangeKwh;
            decimal ben = WT - inp.ExchangeRate;
            slots.Add(new BenefitSlot("exchange", cap, ben, IsGreen1: false));
        }

        // Green Slot 2: beyond GreenSubject (no Article16 bonus, pure grid avoidance)
        if (inp.GreenAvailabilityKwh > inp.GreenSubjectKwh && inp.GreenRate > 0m)
        {
            decimal cap  = inp.GreenAvailabilityKwh - Math.Max(inp.GreenSubjectKwh, 0m);
            decimal ben  = WT - inp.GreenRate;
            slots.Add(new BenefitSlot("green", cap, ben, IsGreen1: false));
        }

        // Bilateral
        if (inp.BilateralRate > 0m && inp.MaxBilateralKwh > 0m)
        {
            decimal cap = inp.MaxBilateralKwh;
            decimal ben = WT - inp.BilateralRate;
            slots.Add(new BenefitSlot("bilateral", cap, ben, IsGreen1: false));
        }

        // Log all slots before filtering
        foreach (var s in slots)
        {
            steps.Add(new ConstraintStep
            {
                Name        = $"slot_{s.Channel}{(s.IsGreen1 ? "_a16" : "")}",
                Description = $"{ChannelFa(s.Channel)}{(s.IsGreen1 ? " (مشمول ماده ۱۶)" : "")}: "
                            + $"ظرفیت {s.Capacity:N0} kWh، مزیت {s.BenefitPerKwh:N0} ریال/kWh — "
                            + (s.BenefitPerKwh > 0m ? "✓ سودمند" : "✗ گران‌تر از شبکه"),
                Value       = s.BenefitPerKwh,
                IsActive    = s.BenefitPerKwh > 0m,
            });
        }

        // ── Step 4: Sort by benefit descending, discard non-beneficial ───────
        var activeSlots = slots
            .Where(s => s.BenefitPerKwh > 0m && s.Capacity > 0m)
            .OrderByDescending(s => s.BenefitPerKwh)
            .ToList();

        // ── Step 5: Greedy fill (Fractional Knapsack) ────────────────────────
        decimal exchangeKwh  = 0m;
        decimal greenKwh     = 0m;
        decimal bilateralKwh = 0m;
        decimal remaining    = budget;

        foreach (var slot in activeSlots)
        {
            if (remaining <= 0m) break;

            decimal take    = Math.Min(slot.Capacity, remaining);
            bool    capped  = take < slot.Capacity - 0.001m; // budget limit hit

            switch (slot.Channel)
            {
                case "exchange":  exchangeKwh  += take; break;
                case "green":     greenKwh     += take; break;
                case "bilateral": bilateralKwh += take; break;
            }
            remaining -= take;

            if (capped) violations.Add($"budget_cap_{slot.Channel}");

            steps.Add(new ConstraintStep
            {
                Name        = $"allocate_{slot.Channel}",
                Description = $"← تخصیص {take:N0} kWh به {ChannelFa(slot.Channel)}"
                            + (capped ? " [محدودیت بودجه]" : ""),
                Value       = take,
                IsActive    = true,
                ConstraintHit = capped ? "budget" : null,
            });
        }

        // Skipped slots (negative benefit)
        foreach (var s in slots.Except(activeSlots))
        {
            steps.Add(new ConstraintStep
            {
                Name        = $"skip_{s.Channel}",
                Description = $"✗ {ChannelFa(s.Channel)} نادیده گرفته شد — "
                            + $"نرخ ({(s.Channel == "exchange" ? inp.ExchangeRate : s.Channel == "green" ? inp.GreenRate : inp.BilateralRate):N0}) "
                            + $"> صرفه ({WT + (s.IsGreen1 ? A16 : 0m):N0} ریال/kWh)",
                Value       = 0m,
                IsActive    = false,
                ConstraintHit = "not_beneficial",
            });
        }

        decimal gridKwh = Math.Max(
            inp.TotalKwh - exchangeKwh - greenKwh - bilateralKwh,
            inp.OperationalReserveKwh);

        steps.Add(new ConstraintStep
        {
            Name        = "grid_residual",
            Description = $"شبکه توزیع: {gridKwh:N0} kWh "
                        + (gridKwh <= inp.OperationalReserveKwh + 0.5m && inp.OperationalReserveKwh > 0m
                            ? "(رزرو عملیاتی)" : "(باقیمانده)"),
            Value       = gridKwh,
            IsActive    = true,
        });

        return new SolverOutput
        {
            Allocation = new PortfolioAllocation
            {
                ExchangeKwh  = exchangeKwh,
                GreenKwh     = greenKwh,
                BilateralKwh = bilateralKwh,
                GridKwh      = gridKwh,
            },
            WeightedTariff   = WT,
            Article16Benefit = A16,
            Steps            = steps,
            ConstraintHits   = violations,
            IsFeasible       = true,
        };
    }

    private static string ChannelFa(string ch) => ch switch
    {
        "exchange"  => "بورس برق",
        "green"     => "برق سبز",
        "bilateral" => "قرارداد دوجانبه",
        _           => ch,
    };
}

// ── Solver output types ──────────────────────────────────────────────────────

public class SolverOutput
{
    public bool               IsFeasible      { get; init; }
    public string?            InfeasibleReason { get; init; }
    public PortfolioAllocation? Allocation    { get; init; }
    public decimal            WeightedTariff  { get; init; }
    public decimal            Article16Benefit { get; init; }
    public List<ConstraintStep> Steps         { get; init; } = [];
    public List<string>       ConstraintHits  { get; init; } = [];

    public static SolverOutput Infeasible(string reason) => new()
    {
        IsFeasible       = false,
        InfeasibleReason = reason,
    };
}

public class ConstraintStep
{
    public string  Name         { get; init; } = "";
    public string  Description  { get; init; } = "";
    public decimal Value        { get; init; }
    public bool    IsActive     { get; init; }
    public string? ConstraintHit { get; init; }
}
