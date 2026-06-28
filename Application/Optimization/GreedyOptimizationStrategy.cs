namespace MatinPower.Server.Application.Optimization;

/// <summary>
/// Phase 3A — Greedy Optimization Strategy (MVP)
///
/// Algorithm (4 steps, no constraint-solving):
///   1. Effective Grid Price = weighted average of maxWhole rates across TOU bands
///   2. Exchange viable?  ExchangePrice &lt; EffectiveGridPrice
///   3. Green viable?     GreenPrice &lt; Article16AvoidancePerKwh (= greenLawRate - tariffMid)
///   4. Allocation: green=required, exchange=fill remaining if viable, grid=rest
///
/// Explainability: every decision produces a reasoning entry (Persian message + numbers).
/// This is intentionally NOT a constraint solver or LP — explainability > mathematical optimality.
/// Phase 3B will add LinearProgrammingStrategy for enterprise constraints.
/// </summary>
public class GreedyOptimizationStrategy : IOptimizationStrategy
{
    public OptimizationResult Optimize(EnergyOptimizationInput input)
    {
        var reasoning    = new List<OptimizationReason>();
        var constraints  = new List<string>();

        if (input.TotalKwh <= 0)
            return new OptimizationResult { Reasoning = reasoning, Constraints = ["مصرف کل صفر است"] };

        // ── Step 1: Effective Grid Price (weighted average across TOU) ─────────
        decimal ratioSum = input.PeakRatio + input.MidRatio + input.LowRatio;
        if (ratioSum <= 0) ratioSum = 1m;  // guard against bad data

        decimal gridCostBaseline =
            input.PeakKwh * input.EffectiveGridRatePeak +
            input.MidKwh  * input.EffectiveGridRateMid  +
            input.LowKwh  * input.EffectiveGridRateLow;

        decimal effectiveGridPerKwh = gridCostBaseline / input.TotalKwh;

        // ── Step 2: Exchange viability ─────────────────────────────────────────
        bool exchangeViable = input.ExchangePrice > 0
                           && input.ExchangePrice < effectiveGridPerKwh;

        reasoning.Add(new OptimizationReason
        {
            EnergyType      = "exchange",
            Message         = exchangeViable
                ? $"بورس ({input.ExchangePrice:N0} ریال/kWh) ارزان‌تر از شبکه ({effectiveGridPerKwh:N0} ریال/kWh) است — خرید توصیه می‌شود"
                : input.ExchangePrice > 0
                    ? $"بورس ({input.ExchangePrice:N0} ریال/kWh) گران‌تر از شبکه ({effectiveGridPerKwh:N0} ریال/kWh) است — خرید توصیه نمی‌شود"
                    : "نرخ بورس وارد نشده است",
            IsActive        = exchangeViable,
            EffectiveCost   = input.ExchangePrice > 0 ? input.ExchangePrice : null,
            AlternativeCost = effectiveGridPerKwh,
        });

        // ── Step 3: Green (Article 16) viability ──────────────────────────────
        decimal article16AvoidancePerKwh = Math.Max(input.GreenLawRate - input.TariffMidRate, 0m);
        decimal article16BaselinePenalty = input.GreenSubjectKwh * article16AvoidancePerKwh;

        bool greenViable = input.GreenSubjectKwh > 0
                        && input.GreenPrice > 0
                        && article16AvoidancePerKwh > 0
                        && input.GreenPrice < article16AvoidancePerKwh;

        if (input.GreenSubjectKwh <= 0)
        {
            reasoning.Add(new OptimizationReason
            {
                EnergyType = "green",
                Message    = "مشمول قانون جهش تولید (ماده ۱۶) نیست — خرید برق سبز اجباری نیست",
                IsActive   = false,
            });
        }
        else if (article16AvoidancePerKwh <= 0)
        {
            reasoning.Add(new OptimizationReason
            {
                EnergyType    = "green",
                Message       = "نرخ قانون جهش از تعرفه پایه کمتر است — جریمه ماده ۱۶ وجود ندارد",
                IsActive      = false,
                EffectiveCost = input.GreenPrice,
            });
        }
        else
        {
            reasoning.Add(new OptimizationReason
            {
                EnergyType      = "green",
                Message         = greenViable
                    ? $"برق سبز ({input.GreenPrice:N0} ریال/kWh) از جریمه ماده ۱۶ ({article16AvoidancePerKwh:N0} ریال/kWh صرفه) ارزان‌تر است — خرید توصیه می‌شود"
                    : $"نرخ برق سبز ({input.GreenPrice:N0} ریال/kWh) از صرفه‌جویی ماده ۱۶ ({article16AvoidancePerKwh:N0} ریال/kWh) بالاتر است — خرید توصیه نمی‌شود",
                IsActive        = greenViable,
                EffectiveCost   = input.GreenPrice,
                AlternativeCost = article16AvoidancePerKwh,
            });
        }

        // ── Step 4: Allocation ─────────────────────────────────────────────────
        // Green: exactly greenSubjectKwh if viable (article16 required — not more)
        decimal greenKwh = greenViable ? input.GreenSubjectKwh : 0m;

        // Exchange: fill remaining if viable, cap at ExchangeMarketCapacity
        decimal remainingAfterGreen = Math.Max(input.TotalKwh - greenKwh, 0m);
        decimal exchangeKwh = 0m;
        if (exchangeViable)
        {
            exchangeKwh = remainingAfterGreen;
            if (input.ExchangeMarketCapacity.HasValue)
            {
                decimal cap = Math.Max(input.ExchangeMarketCapacity.Value - greenKwh, 0m);
                if (exchangeKwh > cap)
                {
                    exchangeKwh = cap;
                    constraints.Add($"ظرفیت بازار محدود است — حداکثر {cap:N0} kWh از بورس");
                }
            }
        }

        // Grid: remainder
        decimal gridKwh = Math.Max(input.TotalKwh - greenKwh - exchangeKwh, 0m);

        if (gridKwh > 0)
        {
            reasoning.Add(new OptimizationReason
            {
                EnergyType    = "grid",
                Message       = $"مصرف باقی‌مانده ({gridKwh:N0} kWh) از شبکه توزیع تامین می‌شود",
                IsActive      = true,
                EffectiveCost = effectiveGridPerKwh,
            });
        }

        // ── Estimate Saving ────────────────────────────────────────────────────
        // Baseline: everything from grid + full article16 penalty
        decimal baselineCost = gridCostBaseline + article16BaselinePenalty;

        // Distribute market energy (exchange + green) across TOU by ratios
        // Then subtract from actual consumption to get remaining grid per band.
        // This ensures: when market=0, gridCost=baseline (saving=0).
        decimal totalMarketKwh = greenKwh + exchangeKwh;
        decimal mktPeakKwh = totalMarketKwh * (input.PeakRatio / ratioSum);
        decimal mktMidKwh  = totalMarketKwh * (input.MidRatio  / ratioSum);
        decimal mktLowKwh  = totalMarketKwh * (input.LowRatio  / ratioSum);

        decimal gridPeakKwh = Math.Max(input.PeakKwh - mktPeakKwh, 0m);
        decimal gridMidKwh  = Math.Max(input.MidKwh  - mktMidKwh,  0m);
        decimal gridLowKwh  = Math.Max(input.LowKwh  - mktLowKwh,  0m);

        decimal gridEnergyCost    = gridPeakKwh * input.EffectiveGridRatePeak
                                  + gridMidKwh  * input.EffectiveGridRateMid
                                  + gridLowKwh  * input.EffectiveGridRateLow;
        decimal exchangeCost      = exchangeKwh * (input.ExchangePrice > 0 ? input.ExchangePrice : 0m);
        decimal greenCost         = greenKwh    * (input.GreenPrice    > 0 ? input.GreenPrice    : 0m);
        decimal remainingArticle16 = Math.Max((input.GreenSubjectKwh - greenKwh) * article16AvoidancePerKwh, 0m);

        decimal recommendedCost   = gridEnergyCost + exchangeCost + greenCost + remainingArticle16;
        decimal estimatedSaving   = Math.Max(baselineCost - recommendedCost, 0m);
        decimal savingPercent     = baselineCost > 0
            ? Math.Round(estimatedSaving / baselineCost * 100m, 2)
            : 0m;

        return new OptimizationResult
        {
            GridKwh                = gridKwh,
            ExchangeKwh            = exchangeKwh,
            GreenKwh               = greenKwh,
            BilateralKwh           = 0m,
            EstimatedSavingAmount  = estimatedSaving,
            EstimatedSavingPercent = savingPercent,
            Reasoning              = reasoning,
            Constraints            = constraints,
        };
    }
}
