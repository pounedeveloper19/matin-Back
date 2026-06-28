using MatinPower.Infrastructure;
using MatinPower.Server.Application.Calculation;
using MatinPower.Server.Application.Optimization;
using MatinPower.Server.Application.Optimization.Portfolio;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    public class BillCalculationController : BaseController
    {
        private static readonly string[] JalaliMonths =
        {
            "", "فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور",
            "مهر","آبان","آذر","دی","بهمن","اسفند"
        };


        // ─── Helpers ──────────────────────────────────────────────────────────

        private static int DaysInJalaliMonth(int month) =>
            CalculationConstants.DaysInJalaliMonth(month);

        private (int peak, int mid, int low, bool found) GetTouHours(int month, int powerEntityId)
        {
            // Resolve type IDs dynamically from title — same logic as the admin frontend.
            // Do NOT use hardcoded int constants: the DB order (1/2/3) may not match
            // peak/mid/low in every installation.
            var types = Repository<EnumToutype>.GetListExtended((string)null, null, System.Web.Helpers.SortDirection.Ascending, null);
            int peakTypeId = types.FirstOrDefault(t => t.Title != null && (t.Title.Contains("اوج") || t.Title.Contains("پیک")))?.Id ?? -1;
            int midTypeId  = types.FirstOrDefault(t => t.Title != null &&  t.Title.Contains("میان"))?.Id ?? -1;
            int lowTypeId  = types.FirstOrDefault(t => t.Title != null && (t.Title.Contains("کم")   || t.Title.Contains("کمبار"))
                                                     && !t.Title.Contains("اوج"))?.Id ?? -1;

            var schedule = Repository<Touschedule>.GetListExtended(i =>
                i.MonthNumber == month &&
                (powerEntityId == 0 || i.PowerEntityId == powerEntityId));

            int peak = schedule.Where(s => s.ToutypeId == peakTypeId).Select(s => s.HourNumber).Distinct().Count();
            int mid  = schedule.Where(s => s.ToutypeId == midTypeId) .Select(s => s.HourNumber).Distinct().Count();
            int low  = schedule.Where(s => s.ToutypeId == lowTypeId) .Select(s => s.HourNumber).Distinct().Count();

            bool found = (peak + mid + low) > 0;
            if (!found)
            {
                peak = CalculationConstants.DefaultPeakHoursPerDay;
                mid  = CalculationConstants.DefaultMidHoursPerDay;
                low  = CalculationConstants.DefaultLowHoursPerDay;
            }

            return (peak, mid, low, found);
        }

        // ─── LEGACY: ManualAnalysis ───────────────────────────────────────────
        // Status: LEGACY — Feature Freeze. Bug-fix only. No new features.
        // Canonical engine: AdvancedAnalysis
        // Migration: customers using ManualAnalysis should be moved to AdvancedAnalysis flow.

        [HttpPost]
        [Route("[controller]/ManualAnalysis")]
        public ExecutionResult ManualAnalysis([FromBody] ManualBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            decimal contractCapacity = subscription.ContractCapacityKw ?? 0m;
            if (contractCapacity <= 0)
                return new ExecutionResult(ResultType.Danger, "خطا", "قدرت قراردادی اشتراک تنظیم نشده است.", 400);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            return RunExceptionProof(() =>
            {
                var contract = Repository<Contract>.GetLast(i => i.SubscriptionId == req.SubscriptionId);
                decimal contractRate = contract?.ContractRate ?? 0;

                var address = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
                int powerEntityId = address?.PowerEntityId ?? 0;

                var (peakHours, midHours, lowHours, _) = GetTouHours(req.Month, powerEntityId);
                int fridayPeakHours = 0;

                int     daysInMonth           = DaysInJalaliMonth(req.Month);
                decimal totalContracted       = contractCapacity * daysInMonth * 24m;
                decimal contractedPeak        = totalContracted * peakHours       / 24m;
                decimal contractedMid         = totalContracted * midHours        / 24m;
                decimal contractedLow         = totalContracted * lowHours        / 24m;
                decimal contractedFridayPeak  = totalContracted * fridayPeakHours / 24m;

                decimal mktPeak = marketRate.MarketPeak ?? 0;
                decimal mktMid  = marketRate.MarketMid  ?? 0;
                decimal mktLow  = marketRate.MarketLow  ?? 0;
                // پشتیبان: حداقل نرخ بازار (جایگزین فیلد حذف‌شده BackupRate)
                decimal backup  = mktLow;

                var bands       = new List<BillAnalysisBand>();
                decimal totalDiff   = 0;
                decimal totalCredit = 0;

                void CalcBand(string name, decimal actual, decimal contracted, decimal mktRate)
                {
                    decimal excess  = Math.Max(actual - contracted, 0);
                    decimal deficit = Math.Max(contracted - actual, 0);
                    decimal penalty = excess  * 1.3m * mktRate;
                    decimal credit  = deficit * 0.75m * backup;
                    totalDiff   += penalty;
                    totalCredit += credit;
                    bands.Add(new BillAnalysisBand
                    {
                        Name           = name,
                        ActualKwh      = actual,
                        ContractedKwh  = contracted,
                        ExcessKwh      = excess,
                        DeficitKwh     = deficit,
                        MarketRateRial = mktRate,
                        PenaltyRial    = penalty,
                        CreditRial     = credit,
                    });
                }

                CalcBand("اوج بار",       req.PeakKwh,       contractedPeak,      mktPeak);
                CalcBand("میان بار",      req.MidKwh,        contractedMid,       mktMid);
                CalcBand("کم بار",        req.LowKwh,        contractedLow,       mktLow);
                CalcBand("اوج بار جمعه", req.FridayPeakKwh, contractedFridayPeak, mktPeak);

                decimal totalCons = req.PeakKwh + req.MidKwh + req.LowKwh + req.FridayPeakKwh;
                decimal matinBill = contractRate * totalContracted;

                decimal withoutMatin = req.PeakKwh       * 1.3m * mktPeak
                                     + req.MidKwh        * 1.3m * mktMid
                                     + req.LowKwh        * 1.3m * mktLow
                                     + req.FridayPeakKwh * 1.3m * mktPeak;

                decimal withMatin = matinBill + totalDiff - totalCredit;
                decimal saving        = withoutMatin - withMatin;
                decimal savingPercent = withoutMatin > 0 ? saving / withoutMatin * 100m : 0;

                Repository<BillAnalysisReport>.InsertItem(new BillAnalysisReport
                {
                    SubscriptionId   = req.SubscriptionId,
                    Year             = req.Year,
                    Month            = req.Month,
                    PeakCons         = req.PeakKwh,
                    MidCons          = req.MidKwh,
                    LowCons          = req.LowKwh,
                    CostWithoutMatin = withoutMatin,
                    CostWithMatin    = withMatin,
                    NetSaving        = saving,
                    CreatedAt        = DateTime.Now,
                });

                return (object)new BillAnalysisResult
                {
                    MonthName              = JalaliMonths[req.Month],
                    Year                   = req.Year,
                    TotalConsumption       = totalCons,
                    ContractCapacityKw     = contractCapacity,
                    ContractedEnergyKwh    = totalContracted,
                    ContractRateRialPerKwh = contractRate,
                    PeakHoursPerDay        = peakHours,
                    MidHoursPerDay         = midHours,
                    LowHoursPerDay         = lowHours,
                    Bands                  = bands,
                    MarketPeakRate         = mktPeak,
                    MarketMidRate          = mktMid,
                    MarketLowRate          = mktLow,
                    BackupRate             = backup,
                    TotalDifferentialRial  = totalDiff,
                    TotalCreditRial        = totalCredit,
                    Article16Rial          = 0,
                    FuelFeeRial            = 0,
                    MatinBillRial          = matinBill,
                    WithoutMatinBillRial   = withoutMatin,
                    WithMatinBillRial      = withMatin,
                    SavingRial             = saving,
                    SavingPercent          = Math.Round(savingPercent, 2),
                };
            });
        }

        // ─── Legacy: ManualOptimalPurchaseCurve ──────────────────────────────

        [HttpPost]
        [Route("[controller]/ManualOptimalPurchaseCurve")]
        public ExecutionResult ManualOptimalPurchaseCurve([FromBody] ManualBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            decimal currentContractCapacity = subscription.ContractCapacityKw ?? 0m;
            if (currentContractCapacity <= 0)
                return new ExecutionResult(ResultType.Danger, "خطا", "قدرت قراردادی اشتراک تنظیم نشده است.", 400);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            return RunExceptionProof(() =>
            {
                var contract = Repository<Contract>.GetLast(i => i.SubscriptionId == req.SubscriptionId);
                decimal contractRate = contract?.ContractRate ?? 0m;

                var address = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
                int powerEntityId = address?.PowerEntityId ?? 0;

                var (peakHours, midHours, lowHours, _) = GetTouHours(req.Month, powerEntityId);
                int fridayPeakHours = 0;

                int     daysInMonth = DaysInJalaliMonth(req.Month);
                decimal monthHours  = daysInMonth * 24m;

                decimal totalCons = req.PeakKwh + req.MidKwh + req.LowKwh + req.FridayPeakKwh;
                decimal mktPeak   = marketRate.MarketPeak ?? 0;
                decimal mktMid    = marketRate.MarketMid  ?? 0;
                decimal mktLow    = marketRate.MarketLow  ?? 0;
                decimal backup    = mktLow;

                decimal withoutMatin =
                    req.PeakKwh       * 1.3m * mktPeak +
                    req.MidKwh        * 1.3m * mktMid  +
                    req.LowKwh        * 1.3m * mktLow  +
                    req.FridayPeakKwh * 1.3m * mktPeak;

                decimal peakImpliedKw   = peakHours       > 0 ? req.PeakKwh       * 24m / (monthHours * peakHours)       : 0m;
                decimal midImpliedKw    = midHours        > 0 ? req.MidKwh        * 24m / (monthHours * midHours)        : 0m;
                decimal lowImpliedKw    = lowHours        > 0 ? req.LowKwh        * 24m / (monthHours * lowHours)        : 0m;
                decimal fridayImpliedKw = fridayPeakHours > 0 ? req.FridayPeakKwh * 24m / (monthHours * fridayPeakHours) : 0m;

                decimal maxCapCandidate = Math.Max(
                    currentContractCapacity * 3m,
                    Math.Max(totalCons / monthHours * 2m,
                        Math.Max(Math.Max(peakImpliedKw, midImpliedKw),
                                 Math.Max(lowImpliedKw, fridayImpliedKw)) * 2m));

                decimal maxCap = maxCapCandidate > 0 ? maxCapCandidate : currentContractCapacity * 2m;

                decimal EvalSavingAtKw(decimal kw)
                {
                    decimal tc  = kw * monthHours;
                    decimal cp  = tc * peakHours       / 24m;
                    decimal cm  = tc * midHours        / 24m;
                    decimal cl  = tc * lowHours        / 24m;
                    decimal cfp = tc * fridayPeakHours / 24m;

                    decimal Penalty(decimal actual, decimal contracted, decimal mkt)
                        => Math.Max(actual - contracted, 0) * 1.3m * mkt;
                    decimal Credit(decimal actual, decimal contracted)
                        => Math.Max(contracted - actual, 0) * 0.75m * backup;

                    decimal diff   = Penalty(req.PeakKwh, cp, mktPeak)       + Penalty(req.MidKwh, cm, mktMid)
                                   + Penalty(req.LowKwh, cl, mktLow)         + Penalty(req.FridayPeakKwh, cfp, mktPeak);
                    decimal credit = Credit(req.PeakKwh, cp)  + Credit(req.MidKwh, cm)
                                   + Credit(req.LowKwh, cl)   + Credit(req.FridayPeakKwh, cfp);

                    return withoutMatin - (contractRate * tc + diff - credit);
                }

                decimal savingAtCurrent = EvalSavingAtKw(currentContractCapacity);
                decimal bestSaving = decimal.MinValue;
                decimal bestKw     = currentContractCapacity;

                int steps = 30;
                var points = new List<OptimalPurchaseCurvePoint>(steps + 1);

                for (int i = 0; i <= steps; i++)
                {
                    decimal kw = maxCap * i / steps;
                    decimal tc = kw * monthHours;

                    decimal cp  = tc * peakHours       / 24m;
                    decimal cm  = tc * midHours        / 24m;
                    decimal cl  = tc * lowHours        / 24m;
                    decimal cfp = tc * fridayPeakHours / 24m;

                    decimal Penalty(decimal actual, decimal contracted, decimal mkt)
                        => Math.Max(actual - contracted, 0) * 1.3m * mkt;
                    decimal Credit(decimal actual, decimal contracted)
                        => Math.Max(contracted - actual, 0) * 0.75m * backup;

                    decimal diff   = Penalty(req.PeakKwh, cp, mktPeak) + Penalty(req.MidKwh, cm, mktMid)
                                   + Penalty(req.LowKwh, cl, mktLow)   + Penalty(req.FridayPeakKwh, cfp, mktPeak);
                    decimal credit = Credit(req.PeakKwh, cp) + Credit(req.MidKwh, cm)
                                   + Credit(req.LowKwh, cl)  + Credit(req.FridayPeakKwh, cfp);

                    decimal withMatin = contractRate * tc + diff - credit;

                    decimal saving    = withoutMatin - withMatin;

                    points.Add(new OptimalPurchaseCurvePoint
                    {
                        ContractCapacityKw  = kw,
                        ContractedEnergyKwh = tc,
                        SavingRial          = saving,
                        WithMatinBillRial   = withMatin
                    });

                    if (saving > bestSaving) { bestSaving = saving; bestKw = kw; }
                }

                return (object)new OptimalPurchaseCurveResult
                {
                    CurrentContractCapacityKw   = currentContractCapacity,
                    SavingAtCurrentContractRial  = savingAtCurrent,
                    OptimalContractCapacityKw    = bestKw,
                    OptimalSavingRial            = bestSaving,
                    WithoutMatinBillRial         = withoutMatin,
                    Points                       = points
                };
            });
        }

        // ─── Advanced Bill Analysis (Excel model) ─────────────────────────────

        // درصد مشمول ماده سبز: delegated to CalculationConstants (single source of truth)
        private static decimal GreenLawPercent(bool isType4B, decimal actualDemandKw, int year) =>
            CalculationConstants.GetArticle16Percent(isType4B, actualDemandKw, year);

        [HttpPost]
        [Route("[controller]/AdvancedAnalysis")]
        public ExecutionResult AdvancedAnalysis([FromBody] AdvancedBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            // ── تعرفه مشتری از پروفایل ──────────────────────────────────
            var address         = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
            var customerProfile = address != null
                ? Repository<CustomerProfile>.GetLast(i => i.Id == address.CustomerProfileId)
                : null;

            if (customerProfile?.TariffCodeOptionId == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "تعرفه مشتری تنظیم نشده است. ابتدا از پروفایل تعرفه انتخاب کنید.", 400);

            int optionId      = customerProfile.TariffCodeOptionId.Value;
            var tariffOption  = Repository<TariffCodeOption>.GetLast(i => i.Id == optionId);
            var tariffCode    = tariffOption != null
                ? Repository<TariffCode>.GetLast(i => i.Id == tariffOption.TariffCodeId)
                : null;
            var tariffRate    = Repository<TariffCodeOptionRate>.GetLast(i =>
                i.TariffCodeOptionId == optionId && i.Year == req.Year);
            if (tariffRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ تعرفه برای سال {req.Year} ثبت نشده است.", 404);

            decimal penaltyMul = tariffOption?.PenaltyMultiplier ?? 1.3m;
            decimal creditMul  = tariffOption?.CreditMultiplier  ?? 0.75m;
            bool isType4B      = tariffCode?.Code?.Contains("ب") == true && tariffCode.Code.Contains("4");

            int powerEntityId = address?.PowerEntityId ?? 0;
            var (peakH, midH, lowH, touFound) = GetTouHours(req.Month, powerEntityId);
            if (!touFound)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "برنامه ساعات TOU برای این ماه تعریف نشده است. ابتدا جدول ساعات را از پنل مدیریت تنظیم کنید.", 400);

            return RunExceptionProof(() =>
            {
                // ── 2. مصرف برق ───────────────────────────────────────────
                decimal touTotal  = (decimal)(peakH + midH + lowH); // > 0 (touFound guard)
                decimal peakRatio = (decimal)peakH / touTotal;
                decimal midRatio  = (decimal)midH  / touTotal;
                decimal lowRatio  = (decimal)lowH  / touTotal;

                decimal peakKwh, midKwh, lowKwh;
                if (req.ConsumptionMode == "total" && req.TotalKwh.HasValue)
                {
                    decimal total = req.TotalKwh.Value;
                    peakKwh = total * peakRatio;
                    midKwh  = total * midRatio;
                    lowKwh  = total * lowRatio;
                }
                else
                {
                    peakKwh = req.PeakKwh ?? 0;
                    midKwh  = req.MidKwh  ?? 0;
                    lowKwh  = req.LowKwh  ?? 0;
                }
                decimal totalKwh = peakKwh + midKwh + lowKwh;

                // ── 3. نرخ تعرفه — برای ماده ۱۶ و نمایش در جدول مقایسه ──
                // tariffMid: نرخ پشتیبانی میان‌بار (از TariffCodeOptionRate)
                // displayPeak/Low: فقط برای نمایش ستون "تعرفه صنعتی" در UI
                decimal tariffMid   = tariffRate.RateRialPerKwh;
                decimal displayPeak = tariffRate.RatePeakRialPerKwh > 0
                    ? tariffRate.RatePeakRialPerKwh
                    : tariffMid * CalculationConstants.TouPeakMultiplier;
                decimal displayLow  = tariffRate.RateLowRialPerKwh > 0
                    ? tariffRate.RateLowRialPerKwh
                    : tariffMid * CalculationConstants.TouLowMultiplier;

                // ── 4. نرخ‌های بازار ──────────────────────────────────────
                decimal mktPeak      = marketRate.MarketPeak ?? 0;    // حداکثر اوج
                decimal mktMid       = marketRate.MarketMid  ?? 0;    // حداکثر میان
                decimal mktLow       = marketRate.MarketLow  ?? 0;    // حداکثر کم
                decimal avgMarket    = marketRate.MarketAvg  ?? 0;    // متوسط بازار
                decimal greenLawRate = marketRate.IndustrialTariffBase ?? 0; // نرخ قانون جهش
                decimal boardPeak    = marketRate.BoardPeak ?? 0;
                decimal boardMid     = marketRate.BoardMid  ?? 0;
                decimal boardLow     = marketRate.BoardLow  ?? 0;

                // ── 5. حداکثر نرخ عمده‌فروشی = ۱.۳ × حداکثر بازار ────────
                decimal maxWholePeak = penaltyMul * mktPeak;
                decimal maxWholeMid  = penaltyMul * mktMid;
                decimal maxWholeLow  = penaltyMul * mktLow;

                // ── 6. درصد و مقدار مشمول قانون جهش تولید ────────────────
                decimal greenPercent    = GreenLawPercent(isType4B, req.ActualDemandKw, req.Year);
                decimal greenSubjectKwh = totalKwh * greenPercent;  // مصرف مشمول قانون جهش

                // ── 7. توزیع انرژی خریداری‌شده بر اساس نسبت ساعات TOU ────
                decimal greenPeak = req.GreenLawKwh * peakRatio;
                decimal greenMid  = req.GreenLawKwh * midRatio;
                decimal greenLow  = req.GreenLawKwh * lowRatio;

                decimal bilateralPeak = req.BilateralKwh * peakRatio;
                decimal bilateralMid  = req.BilateralKwh * midRatio;
                decimal bilateralLow  = req.BilateralKwh * lowRatio;

                decimal exchangePeak = req.ExchangeKwh * peakRatio;
                decimal exchangeMid  = req.ExchangeKwh * midRatio;
                decimal exchangeLow  = req.ExchangeKwh * lowRatio;

                // ── 8. کل انرژی بازار هر بازه (دوجانبه+بورس+سبز) ────────
                decimal mktEnergyPeak = bilateralPeak + exchangePeak + greenPeak;
                decimal mktEnergyMid  = bilateralMid  + exchangeMid  + greenMid;
                decimal mktEnergyLow  = bilateralLow  + exchangeLow  + greenLow;

                // ── 9. باقیمانده از شبکه توزیع ───────────────────────────
                decimal remainingPeak = Math.Max(0, peakKwh - mktEnergyPeak);
                decimal remainingMid  = Math.Max(0, midKwh  - mktEnergyMid);
                decimal remainingLow  = Math.Max(0, lowKwh  - mktEnergyLow);

                // ── 10. بهای انرژی شبکه = حداکثر نرخ بازار × ضریب جریمه (بدون هاردکد) ─
                // maxWholePeak/Mid/Low از Step 5 = mktBand × penaltyMul
                decimal energyBefore = peakKwh * maxWholePeak + midKwh * maxWholeMid + lowKwh * maxWholeLow;
                decimal energyAfter  = remainingPeak * maxWholePeak + remainingMid * maxWholeMid + remainingLow * maxWholeLow;

                // ── 11. مابه التفاوت ماده ۱۶ ─────────────────────────────
                // قبل = greenSubject × max(greenLawRate - tariffMid, 0)
                // بعد  = max((greenSubject - greenPurchased) × (greenLawRate - tariffMid), 0)
                decimal greenDiff       = greenLawRate - tariffMid;
                decimal article16Before = greenSubjectKwh * Math.Max(greenDiff, 0);
                decimal article16After  = Math.Max((greenSubjectKwh - req.GreenLawKwh) * greenDiff, 0);

                // ── 12. بستانکاری — Model A: Market Settlement Credit ─────────
                // تصمیم معماری (1404): برای AdvancedAnalysis از Model A استفاده می‌شود.
                // Model A = market settlement: وقتی خرید بازار > مصرف در یک بازه TOU،
                //           مازاد به نرخ تابلو × creditMul بستانکار می‌شود.
                // Model B (utility billing) = reserved for ManualAnalysis (legacy contract billing).
                //
                // فرمول: Σ min((مصرف_بازه - خرید_بازار_بازه) × نرخ_تابلو × creditMul, 0)
                // - وقتی مصرف < خرید: مقدار منفی → کاهش costWithMatin (بستانکاری)
                // - وقتی مصرف > خرید: مقدار مثبت → Math.Min(...,0) = 0 (جریمه‌ای نیست)
                decimal creditPeak  = Math.Min((peakKwh - mktEnergyPeak) * boardPeak * creditMul, 0);
                decimal creditMid   = Math.Min((midKwh  - mktEnergyMid)  * boardMid  * creditMul, 0);
                decimal creditLow   = Math.Min((lowKwh  - mktEnergyLow)  * boardLow  * creditMul, 0);
                decimal totalCredit = creditPeak + creditMid + creditLow;

                // ── 13. صورتحساب‌های بازار ───────────────────────────────
                decimal bilateralBill = req.BilateralKwh * req.BilateralRate;
                decimal exchangeBill  = req.ExchangeKwh  * req.ExchangeRate;
                decimal greenBill     = req.GreenLawKwh  * req.GreenRate;

                // ── 14. هزینه نهایی — مطابق PDF spec ────────────────────────
                // FinalCost = GridCost + Article16 + ExchangeCost + BilateralCost + GreenCost - Credits
                decimal costWithoutMatin = energyBefore + article16Before;
                decimal costWithMatin    = energyAfter  + article16After
                                         + totalCredit
                                         + bilateralBill + exchangeBill + greenBill;

                decimal netSaving     = costWithoutMatin - costWithMatin;
                decimal savingPercent = costWithoutMatin > 0
                    ? Math.Round(netSaving / costWithoutMatin * 100m, 2) : 0;

                // ── 15. ذخیره در دیتابیس ──────────────────────────────────
                if (req.SaveReport)
                {
                    Repository<BillAnalysisReport>.InsertItem(new BillAnalysisReport
                    {
                        SubscriptionId     = req.SubscriptionId,
                        Year               = req.Year,
                        Month              = req.Month,
                        PeakCons           = peakKwh,
                        MidCons            = midKwh,
                        LowCons            = lowKwh,
                        TariffCodeOptionId = optionId,
                        ContractDemandKw   = req.ContractDemandKw,
                        ActualDemandKw     = req.ActualDemandKw,
                        BilateralKwh       = req.BilateralKwh,
                        BilateralRate      = req.BilateralRate,
                        ExchangeKwh        = req.ExchangeKwh,
                        ExchangeRate       = req.ExchangeRate,
                        GreenLawKwh        = req.GreenLawKwh,
                        GreenRate          = req.GreenRate,
                        CostWithoutMatin   = costWithoutMatin,
                        CostWithMatin      = costWithMatin,
                        NetSaving          = netSaving,
                        BilateralBillRial  = bilateralBill,
                        ExchangeBillRial   = exchangeBill,
                        GreenBillRial      = greenBill,
                        CreatedAt          = DateTime.Now,
                    });
                }

                return (object)new AdvancedBillAnalysisResult
                {
                    MonthName           = JalaliMonths[req.Month],
                    Year                = req.Year,
                    Month               = req.Month,
                    PeakKwh             = peakKwh,
                    MidKwh              = midKwh,
                    LowKwh              = lowKwh,
                    TotalKwh            = totalKwh,
                    PeakHoursPerDay     = peakH,
                    MidHoursPerDay      = midH,
                    LowHoursPerDay      = lowH,
                    TariffPeakRial      = displayPeak,   // نرخ تعرفه صنعتی — فقط نمایش
                    TariffMidRial       = tariffMid,     // نرخ تعرفه صنعتی میان‌بار
                    TariffLowRial       = displayLow,    // نرخ تعرفه صنعتی — فقط نمایش
                    MaxWholePeak        = maxWholePeak,
                    MaxWholeMid         = maxWholeMid,
                    MaxWholeLow         = maxWholeLow,
                    AvgMarket           = avgMarket,
                    GreenLawRate        = greenLawRate,
                    GreenPercent        = greenPercent,
                    GreenSubjectKwh     = greenSubjectKwh,
                    MarketEnergyPeak    = mktEnergyPeak,
                    MarketEnergyMid     = mktEnergyMid,
                    MarketEnergyLow     = mktEnergyLow,
                    RemainingPeak       = remainingPeak,
                    RemainingMid        = remainingMid,
                    RemainingLow        = remainingLow,
                    EnergyBeforeRial    = energyBefore,
                    Article16BeforeRial = article16Before,
                    RegulatoryBeforeRial= 0m,
                    EnergyAfterRial     = energyAfter,
                    Article16AfterRial  = article16After,
                    RegulatoryAfterRial = 0m,
                    CreditRial          = totalCredit,
                    BilateralBillRial   = bilateralBill,
                    ExchangeBillRial    = exchangeBill,
                    GreenBillRial       = greenBill,
                    CostWithoutMatin    = costWithoutMatin,
                    CostWithMatin       = costWithMatin,
                    NetSaving           = netSaving,
                    SavingPercent       = savingPercent,
                };
            });
        }

        // ─── Advanced Optimal Purchase Curve (Exchange sweep) ────────────────
        // محور X: خرید از بورس برق (kWh)، از صفر تا کل مصرف
        // هزینه‌ها از همان مدل AdvancedAnalysis محاسبه می‌شوند (نه مدل قراردادی)

        [HttpPost]
        [Route("[controller]/AdvancedOptimalPurchaseCurve")]
        public ExecutionResult AdvancedOptimalPurchaseCurve([FromBody] AdvancedBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            var address         = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
            var customerProfile = address != null
                ? Repository<CustomerProfile>.GetLast(i => i.Id == address.CustomerProfileId)
                : null;

            if (customerProfile?.TariffCodeOptionId == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "تعرفه مشتری تنظیم نشده است.", 400);

            int optionId     = customerProfile.TariffCodeOptionId.Value;
            var tariffOption = Repository<TariffCodeOption>.GetLast(i => i.Id == optionId);
            var tariffCode   = tariffOption != null
                ? Repository<TariffCode>.GetLast(i => i.Id == tariffOption.TariffCodeId)
                : null;
            var tariffRate   = Repository<TariffCodeOptionRate>.GetLast(i =>
                i.TariffCodeOptionId == optionId && i.Year == req.Year);
            if (tariffRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ تعرفه برای سال {req.Year} ثبت نشده است.", 404);

            decimal penaltyMul = tariffOption?.PenaltyMultiplier ?? CalculationConstants.DefaultPenaltyMultiplier;
            decimal creditMul  = tariffOption?.CreditMultiplier  ?? CalculationConstants.DefaultCreditMultiplier;
            bool isType4B      = tariffCode?.Code?.Contains("ب") == true && tariffCode.Code.Contains("4");

            int powerEntityId = address?.PowerEntityId ?? 0;
            var (peakH, midH, lowH, touFound) = GetTouHours(req.Month, powerEntityId);
            if (!touFound)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "برنامه ساعات TOU برای این ماه تعریف نشده است. ابتدا جدول ساعات را از پنل مدیریت تنظیم کنید.", 400);

            return RunExceptionProof(() =>
            {
                // ── مصرف (همان منطق AdvancedAnalysis) ────────────────────
                decimal touTotal  = (decimal)(peakH + midH + lowH);
                decimal peakRatio = (decimal)peakH / touTotal;
                decimal midRatio  = (decimal)midH  / touTotal;
                decimal lowRatio  = (decimal)lowH  / touTotal;

                decimal peakKwh, midKwh, lowKwh;
                if (req.ConsumptionMode == "total" && req.TotalKwh.HasValue)
                {
                    decimal total = req.TotalKwh.Value;
                    peakKwh = total * peakRatio;
                    midKwh  = total * midRatio;
                    lowKwh  = total * lowRatio;
                }
                else
                {
                    peakKwh = req.PeakKwh ?? 0;
                    midKwh  = req.MidKwh  ?? 0;
                    lowKwh  = req.LowKwh  ?? 0;
                }
                decimal totalKwh = peakKwh + midKwh + lowKwh;

                // ── نرخ‌ها ────────────────────────────────────────────────
                // tariffMid = نرخ پشتیبانی میان‌بار — فقط برای ماده ۱۶
                decimal tariffMid    = tariffRate.RateRialPerKwh;
                decimal mktPeak      = marketRate.MarketPeak ?? 0;
                decimal mktMid       = marketRate.MarketMid  ?? 0;
                decimal mktLow       = marketRate.MarketLow  ?? 0;
                decimal avgMarket    = marketRate.MarketAvg  ?? 0;
                decimal greenLawRate = marketRate.IndustrialTariffBase ?? 0;
                decimal boardPeak    = marketRate.BoardPeak ?? 0;
                decimal boardMid     = marketRate.BoardMid  ?? 0;
                decimal boardLow     = marketRate.BoardLow  ?? 0;

                // نرخ بهای انرژی شبکه = حداکثر نرخ بازار × ضریب جریمه
                decimal gridRatePeak = mktPeak * penaltyMul;
                decimal gridRateMid  = mktMid  * penaltyMul;
                decimal gridRateLow  = mktLow  * penaltyMul;

                // ── ثوابت ماده ۱۶ ─────────────────────────────────────────
                decimal greenPercent    = GreenLawPercent(isType4B, req.ActualDemandKw, req.Year);
                decimal greenSubjectKwh = totalKwh * greenPercent;
                decimal greenDiff       = greenLawRate - tariffMid;
                decimal article16Before = greenSubjectKwh * Math.Max(greenDiff, 0);

                // ── ثابت: دوجانبه و سبز (مستقل از مقدار بورس) ──────────
                decimal bilateralPeak = req.BilateralKwh * peakRatio;
                decimal bilateralMid  = req.BilateralKwh * midRatio;
                decimal bilateralLow  = req.BilateralKwh * lowRatio;
                decimal greenPeak     = req.GreenLawKwh   * peakRatio;
                decimal greenMid      = req.GreenLawKwh   * midRatio;
                decimal greenLow      = req.GreenLawKwh   * lowRatio;

                decimal bilateralBill = req.BilateralKwh * req.BilateralRate;
                decimal greenBill     = req.GreenLawKwh  * req.GreenRate;

                decimal article16AfterFixed = Math.Max((greenSubjectKwh - req.GreenLawKwh) * greenDiff, 0);

                // ── هزینه بدون قرارداد (کل مصرف از شبکه) ───────────────
                decimal energyBefore     = peakKwh * gridRatePeak + midKwh * gridRateMid + lowKwh * gridRateLow;
                decimal costWithoutMatin = energyBefore + article16Before;

                // ── تابع هزینه برای هر مقدار خرید بورس ─────────────────────
                decimal ComputeCostWithMatin(decimal exchKwh)
                {
                    decimal ePeak = exchKwh * peakRatio;
                    decimal eMid  = exchKwh * midRatio;
                    decimal eLow  = exchKwh * lowRatio;

                    decimal mktEnergyPeak = bilateralPeak + ePeak + greenPeak;
                    decimal mktEnergyMid  = bilateralMid  + eMid  + greenMid;
                    decimal mktEnergyLow  = bilateralLow  + eLow  + greenLow;

                    decimal remainingPeak = Math.Max(0, peakKwh - mktEnergyPeak);
                    decimal remainingMid  = Math.Max(0, midKwh  - mktEnergyMid);
                    decimal remainingLow  = Math.Max(0, lowKwh  - mktEnergyLow);

                    decimal energyAfter =
                        remainingPeak * gridRatePeak +
                        remainingMid  * gridRateMid  +
                        remainingLow  * gridRateLow;

                    decimal creditP = Math.Min((peakKwh - mktEnergyPeak) * boardPeak * creditMul, 0);
                    decimal creditM = Math.Min((midKwh  - mktEnergyMid)  * boardMid  * creditMul, 0);
                    decimal creditL = Math.Min((lowKwh  - mktEnergyLow)  * boardLow  * creditMul, 0);

                    return energyAfter + article16AfterFixed
                         + creditP + creditM + creditL
                         + bilateralBill + (exchKwh * req.ExchangeRate) + greenBill;
                }

                // ── پیمایش ۳۰ نقطه از ۰ تا totalKwh ─────────────────────
                int steps = 30;
                var points = new List<OptimalPurchaseCurvePoint>(steps + 1);
                decimal bestSaving      = decimal.MinValue;
                decimal bestExchangeKwh = req.ExchangeKwh;

                for (int i = 0; i <= steps; i++)
                {
                    decimal exchKwh      = totalKwh * i / steps;
                    decimal costWithMatin = ComputeCostWithMatin(exchKwh);
                    decimal saving        = costWithoutMatin - costWithMatin;

                    points.Add(new OptimalPurchaseCurvePoint
                    {
                        ContractCapacityKw  = exchKwh,
                        ContractedEnergyKwh = exchKwh + req.BilateralKwh + req.GreenLawKwh,
                        SavingRial          = saving,
                        WithMatinBillRial   = costWithMatin,
                    });

                    if (saving > bestSaving) { bestSaving = saving; bestExchangeKwh = exchKwh; }
                }

                decimal savingAtCurrent = costWithoutMatin - ComputeCostWithMatin(req.ExchangeKwh);

                return (object)new OptimalPurchaseCurveResult
                {
                    CurrentContractCapacityKw   = req.ExchangeKwh,
                    SavingAtCurrentContractRial  = savingAtCurrent,
                    OptimalContractCapacityKw    = bestExchangeKwh,
                    OptimalSavingRial            = bestSaving,
                    WithoutMatinBillRial         = costWithoutMatin,
                    Points                       = points,
                };
            });
        }

        // ─── Greedy Recommendation Engine ────────────────────────────────────
        // پیشنهاد بهینه خرید انرژی — نسخه MVP (Greedy, Phase 3A)
        // ورودی: همان AdvancedBillAnalysisRequest — خروجی: OptimizationResult با reasoning

        [HttpPost]
        [Route("[controller]/GetRecommendation")]
        public ExecutionResult GetRecommendation([FromBody] AdvancedBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            var address         = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
            var customerProfile = address != null
                ? Repository<CustomerProfile>.GetLast(i => i.Id == address.CustomerProfileId)
                : null;

            if (customerProfile?.TariffCodeOptionId == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "تعرفه مشتری تنظیم نشده است.", 400);

            int optionId     = customerProfile.TariffCodeOptionId.Value;
            var tariffOption = Repository<TariffCodeOption>.GetLast(i => i.Id == optionId);
            var tariffCode   = tariffOption != null
                ? Repository<TariffCode>.GetLast(i => i.Id == tariffOption.TariffCodeId)
                : null;
            var tariffRate   = Repository<TariffCodeOptionRate>.GetLast(i =>
                i.TariffCodeOptionId == optionId && i.Year == req.Year);
            if (tariffRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ تعرفه برای سال {req.Year} ثبت نشده است.", 404);

            decimal penaltyMul = tariffOption?.PenaltyMultiplier ?? CalculationConstants.DefaultPenaltyMultiplier;
            bool isType4B      = tariffCode?.Code?.Contains("ب") == true && tariffCode.Code.Contains("4");

            int powerEntityId = address?.PowerEntityId ?? 0;
            var (peakH, midH, lowH, touFound) = GetTouHours(req.Month, powerEntityId);
            if (!touFound)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "برنامه ساعات TOU برای این ماه تعریف نشده است. ابتدا جدول ساعات را از پنل مدیریت تنظیم کنید.", 400);

            return RunExceptionProof(() =>
            {
                // ── مصرف (همان منطق AdvancedAnalysis) ────────────────────
                decimal touTotal  = (decimal)(peakH + midH + lowH);
                decimal peakRatio = (decimal)peakH / touTotal;
                decimal midRatio  = (decimal)midH  / touTotal;
                decimal lowRatio  = (decimal)lowH  / touTotal;

                decimal peakKwh, midKwh, lowKwh;
                if (req.ConsumptionMode == "total" && req.TotalKwh.HasValue)
                {
                    decimal total = req.TotalKwh.Value;
                    peakKwh = total * peakRatio;
                    midKwh  = total * midRatio;
                    lowKwh  = total * lowRatio;
                }
                else
                {
                    peakKwh = req.PeakKwh ?? 0;
                    midKwh  = req.MidKwh  ?? 0;
                    lowKwh  = req.LowKwh  ?? 0;
                }
                decimal totalKwh = peakKwh + midKwh + lowKwh;

                // ── نرخ‌های مرجع ──────────────────────────────────────────
                decimal tariffMid    = tariffRate.RateRialPerKwh;
                decimal mktPeak      = marketRate.MarketPeak          ?? 0;
                decimal mktMid       = marketRate.MarketMid           ?? 0;
                decimal mktLow       = marketRate.MarketLow           ?? 0;
                decimal greenLawRate = marketRate.IndustrialTariffBase ?? 0;

                // ── ساخت input برای استراتژی ─────────────────────────────
                decimal greenPercent    = GreenLawPercent(isType4B, req.ActualDemandKw, req.Year);
                decimal greenSubjectKwh = totalKwh * greenPercent;

                var input = new EnergyOptimizationInput
                {
                    TotalKwh              = totalKwh,
                    PeakKwh               = peakKwh,
                    MidKwh                = midKwh,
                    LowKwh                = lowKwh,
                    PeakRatio             = peakRatio,
                    MidRatio              = midRatio,
                    LowRatio              = lowRatio,
                    GreenSubjectKwh       = greenSubjectKwh,
                    ExchangePrice         = req.ExchangeRate,
                    GreenPrice            = req.GreenRate,
                    BilateralPrice        = req.BilateralRate,
                    EffectiveGridRatePeak = penaltyMul * mktPeak,
                    EffectiveGridRateMid  = penaltyMul * mktMid,
                    EffectiveGridRateLow  = penaltyMul * mktLow,
                    GreenLawRate          = greenLawRate,
                    TariffMidRate         = tariffMid,
                    ExchangeMarketCapacity = null,  // بدون سقف برای MVP
                };

                IOptimizationStrategy strategy = new GreedyOptimizationStrategy();
                var result = strategy.Optimize(input);
                return (object)result;
            });
        }

        // ─── GetOptimalPortfolio ──────────────────────────────────────────────
        // موتور بهینه‌سازی سبد انرژی — Phase 3B (Constrained LP)
        //
        // الگوریتم: Fractional Knapsack — بهینه اثبات‌شده برای LP با objective جدا‌پذیر
        //   1. WT = میانگین وزنی تعرفه (نرخ موثر شبکه per kWh)
        //   2. مزیت هر کانال = WT (+ A16 برای سبز اول) − نرخ کانال
        //   3. پر کردن greedy از بالاترین مزیت تا تمام شدن بودجه بازار
        //   4. شبکه = باقیمانده ≥ رزرو عملیاتی

        [HttpPost]
        [Route("[controller]/GetOptimalPortfolio")]
        public ExecutionResult GetOptimalPortfolio([FromBody] PortfolioOptimizationRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "شناسه یافت نشد.", 404);

            var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                i.Year == req.Year && i.Month == req.Month);
            if (marketRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

            var address         = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
            var customerProfile = address != null
                ? Repository<CustomerProfile>.GetLast(i => i.Id == address.CustomerProfileId)
                : null;

            if (customerProfile?.TariffCodeOptionId == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "تعرفه مشتری تنظیم نشده است. ابتدا از پروفایل تعرفه انتخاب کنید.", 400);

            int optionId     = customerProfile.TariffCodeOptionId.Value;
            var tariffOption = Repository<TariffCodeOption>.GetLast(i => i.Id == optionId);
            var tariffCode   = tariffOption != null
                ? Repository<TariffCode>.GetLast(i => i.Id == tariffOption.TariffCodeId)
                : null;
            var tariffRate   = Repository<TariffCodeOptionRate>.GetLast(i =>
                i.TariffCodeOptionId == optionId && i.Year == req.Year);
            if (tariffRate == null)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    $"نرخ تعرفه برای سال {req.Year} ثبت نشده است.", 404);

            bool isType4B = tariffCode?.Code?.Contains("ب") == true && tariffCode.Code.Contains("4");

            int powerEntityId = address?.PowerEntityId ?? 0;
            var (peakH, midH, lowH, touFound) = GetTouHours(req.Month, powerEntityId);
            if (!touFound)
                return new ExecutionResult(ResultType.Danger, "خطا",
                    "برنامه ساعات TOU برای این ماه تعریف نشده است.", 400);

            return RunExceptionProof(() =>
            {
                // ── مصرف ────────────────────────────────────────────────────
                decimal touTotal  = (decimal)(peakH + midH + lowH);
                decimal peakRatio = (decimal)peakH / touTotal;
                decimal midRatio  = (decimal)midH  / touTotal;
                decimal lowRatio  = (decimal)lowH  / touTotal;

                decimal peakKwh, midKwh, lowKwh;
                if (req.ConsumptionMode == "total" && req.TotalKwh.HasValue)
                {
                    peakKwh = req.TotalKwh.Value * peakRatio;
                    midKwh  = req.TotalKwh.Value * midRatio;
                    lowKwh  = req.TotalKwh.Value * lowRatio;
                }
                else
                {
                    peakKwh = req.PeakKwh ?? 0m;
                    midKwh  = req.MidKwh  ?? 0m;
                    lowKwh  = req.LowKwh  ?? 0m;
                }
                decimal totalKwh = peakKwh + midKwh + lowKwh;

                if (totalKwh <= 0m)
                    return (object)new { error = "مصرف کل صفر است" };

                // ── تعرفه — فقط برای ماده ۱۶ ──────────────────────────────────
                decimal tariffMid  = tariffRate.RateRialPerKwh;
                decimal penaltyMulP = tariffOption?.PenaltyMultiplier ?? CalculationConstants.DefaultPenaltyMultiplier;

                // ── نرخ‌های بازار ─────────────────────────────────────────────
                decimal mktPeakP = marketRate.MarketPeak ?? 0m;
                decimal mktMidP  = marketRate.MarketMid  ?? 0m;
                decimal mktLowP  = marketRate.MarketLow  ?? 0m;

                // نرخ بهای انرژی شبکه = حداکثر نرخ بازار × ضریب جریمه
                decimal gridRatePeakP = mktPeakP * penaltyMulP;
                decimal gridRateMidP  = mktMidP  * penaltyMulP;
                decimal gridRateLowP  = mktLowP  * penaltyMulP;

                // ── ماده ۱۶ ──────────────────────────────────────────────────
                decimal greenPercent    = GreenLawPercent(isType4B, req.ActualDemandKw, req.Year);
                decimal greenSubjectKwh = totalKwh * greenPercent;
                decimal greenLawRate    = marketRate.IndustrialTariffBase ?? 0m;

                // ── محدودیت‌ها: صفر = نامحدود تا totalKwh ────────────────────
                decimal maxExchange  = req.MaxExchangeKwh       > 0m ? req.MaxExchangeKwh        : totalKwh;
                decimal greenAvail   = req.GreenAvailabilityKwh > 0m ? req.GreenAvailabilityKwh  : greenSubjectKwh;
                decimal maxBilateral = req.MaxBilateralKwh      > 0m ? req.MaxBilateralKwh       : totalKwh;

                // ── ساخت input ────────────────────────────────────────────────
                var inp = new PortfolioOptimizationInput
                {
                    TotalKwh = totalKwh,
                    PeakKwh  = peakKwh,
                    MidKwh   = midKwh,
                    LowKwh   = lowKwh,

                    PeakRatio = peakRatio,
                    MidRatio  = midRatio,
                    LowRatio  = lowRatio,

                    TariffPeak = gridRatePeakP,
                    TariffMid  = gridRateMidP,
                    TariffLow  = gridRateLowP,

                    ExchangeRate  = req.ExchangeRate,
                    GreenRate     = req.GreenRate,
                    BilateralRate = req.BilateralRate,

                    GreenSubjectKwh      = greenSubjectKwh,
                    GreenLawRate         = greenLawRate,

                    MaxExchangeKwh        = maxExchange,
                    GreenAvailabilityKwh  = greenAvail,
                    MaxBilateralKwh       = maxBilateral,
                    OperationalReserveKwh = req.OperationalReserveKwh,
                };

                // ── اجرای solver ──────────────────────────────────────────────
                var solverOut = PortfolioSolver.Solve(inp);
                if (!solverOut.IsFeasible)
                    return (object)new { error = solverOut.InfeasibleReason };

                var alloc    = solverOut.Allocation!;
                var cost     = CostEvaluator.Evaluate(inp, alloc);
                var baseline = CostEvaluator.Baseline(inp);

                decimal WT  = solverOut.WeightedTariff;
                decimal A16 = solverOut.Article16Benefit;

                // ── Explainability ────────────────────────────────────────────
                var reasoning = new List<ChannelDecision>
                {
                    new()
                    {
                        Channel        = "exchange",
                        ChannelName    = "بورس برق",
                        IsActive       = alloc.ExchangeKwh > 0m,
                        KwhAllocated   = alloc.ExchangeKwh,
                        CostRial       = cost.ExchangeBill,
                        ChannelRate    = req.ExchangeRate,
                        GridSavingRate = WT,
                        Message        = alloc.ExchangeKwh > 0m
                            ? $"بورس ({req.ExchangeRate:N0} ریال/kWh) ارزان‌تر از شبکه ({WT:N0} ریال/kWh) — {alloc.ExchangeKwh:N0} kWh خریداری شد"
                            : req.ExchangeRate <= 0m
                                ? "نرخ بورس وارد نشده است"
                                : $"بورس ({req.ExchangeRate:N0} ریال/kWh) گران‌تر از شبکه ({WT:N0} ریال/kWh) — خرید توصیه نمی‌شود",
                    },
                    new()
                    {
                        Channel        = "green",
                        ChannelName    = "برق سبز",
                        IsActive       = alloc.GreenKwh > 0m,
                        KwhAllocated   = alloc.GreenKwh,
                        CostRial       = cost.GreenBill,
                        ChannelRate    = req.GreenRate,
                        GridSavingRate = WT + A16,
                        Message        = alloc.GreenKwh > 0m
                            ? $"برق سبز ({req.GreenRate:N0} ریال/kWh): {alloc.GreenKwh:N0} kWh — "
                              + (greenSubjectKwh > 0m
                                  ? $"جریمه ماده ۱۶ ({A16:N0} ریال/kWh صرفه) حذف شد"
                                  : "ارزان‌تر از شبکه")
                            : greenSubjectKwh <= 0m
                                ? "مشمول ماده ۱۶ نیست"
                                : $"برق سبز ({req.GreenRate:N0} ریال/kWh) گران‌تر از صرفه ({WT + A16:N0} ریال/kWh) — خرید توصیه نمی‌شود",
                    },
                    new()
                    {
                        Channel        = "bilateral",
                        ChannelName    = "قرارداد دوجانبه",
                        IsActive       = alloc.BilateralKwh > 0m,
                        KwhAllocated   = alloc.BilateralKwh,
                        CostRial       = cost.BilateralBill,
                        ChannelRate    = req.BilateralRate,
                        GridSavingRate = WT,
                        Message        = alloc.BilateralKwh > 0m
                            ? $"دوجانبه ({req.BilateralRate:N0} ریال/kWh): {alloc.BilateralKwh:N0} kWh — ارزان‌تر از شبکه"
                            : req.BilateralRate <= 0m
                                ? "نرخ دوجانبه وارد نشده است"
                                : $"دوجانبه ({req.BilateralRate:N0} ریال/kWh) گران‌تر از شبکه ({WT:N0} ریال/kWh) — خرید توصیه نمی‌شود",
                    },
                    new()
                    {
                        Channel      = "grid",
                        ChannelName  = "شبکه توزیع",
                        IsActive     = true,
                        KwhAllocated = alloc.GridKwh,
                        CostRial     = cost.EnergyAfter,
                        ChannelRate  = WT,
                        Message      = alloc.GridKwh <= req.OperationalReserveKwh + 0.5m && req.OperationalReserveKwh > 0m
                            ? $"شبکه ({alloc.GridKwh:N0} kWh): رزرو عملیاتی حفظ شد"
                            : $"شبکه ({alloc.GridKwh:N0} kWh): باقیمانده از شبکه تامین می‌شود",
                    },
                };

                return (object)new PortfolioOptimizationResult
                {
                    OptimalMix = new OptimalMix
                    {
                        ExchangeKwh   = alloc.ExchangeKwh,
                        GreenKwh      = alloc.GreenKwh,
                        BilateralKwh  = alloc.BilateralKwh,
                        GridKwh       = alloc.GridKwh,
                        ExchangeCost  = cost.ExchangeBill,
                        GreenCost     = cost.GreenBill,
                        BilateralCost = cost.BilateralBill,
                        GridCost      = cost.EnergyAfter,
                    },
                    TotalCost        = cost.CostWith,
                    BaselineCost     = baseline.CostWithout,
                    Saving           = baseline.CostWithout - cost.CostWith,
                    SavingPercent    = baseline.CostWithout > 0m
                        ? Math.Round((baseline.CostWithout - cost.CostWith) / baseline.CostWithout * 100m, 2)
                        : 0m,
                    ResidualGridCost = cost.EnergyAfter,
                    ExchangeBill     = cost.ExchangeBill,
                    GreenBill        = cost.GreenBill,
                    BilateralBill    = cost.BilateralBill,
                    Article16After   = cost.Article16After,
                    Article16Saved   = baseline.Article16Before - cost.Article16After,
                    WeightedGridTariff = WT,
                    Article16Benefit   = A16,
                    ConstraintHits   = solverOut.ConstraintHits,
                    Reasoning        = reasoning,
                    SolverTrace      = solverOut.Steps,
                };
            });
        }

        // ─── SubscriptionRate: پیش‌بارگذاری نرخ مشتری ───────────────────────

        [HttpGet]
        [Route("[controller]/GetSubscriptionRate/{subscriptionId}/{year}/{month}")]
        public ExecutionResult GetSubscriptionRate(int subscriptionId, int year, int month)
        {
            return RunExceptionProof(() =>
            {
                // ماه جاری یا آخرین ماه ثبت‌شده
                var rate = Repository<SubscriptionRate>.GetLast(i =>
                    i.SubscriptionId == subscriptionId &&
                    (i.Year < year || (i.Year == year && i.Month <= month)));

                return (object)rate;
            });
        }

        [HttpPost]
        [Route("[controller]/SaveSubscriptionRate")]
        public ExecutionResult SaveSubscriptionRate([FromBody] SubscriptionRateRequest req)
        {
            return RunExceptionProof(() =>
            {
                var existing = Repository<SubscriptionRate>.GetLast(i =>
                    i.SubscriptionId == req.SubscriptionId &&
                    i.Year == req.Year && i.Month == req.Month);

                if (existing != null)
                {
                    existing.RateType       = req.RateType;
                    existing.SingleRateRial = req.SingleRateRial;
                    existing.PeakRateRial   = req.PeakRateRial;
                    existing.MidRateRial    = req.MidRateRial;
                    existing.LowRateRial    = req.LowRateRial;
                    Repository<SubscriptionRate>.UpdateItem(existing);
                }
                else
                {
                    Repository<SubscriptionRate>.InsertItem(new SubscriptionRate
                    {
                        SubscriptionId = req.SubscriptionId,
                        Year           = req.Year,
                        Month          = req.Month,
                        RateType       = req.RateType,
                        SingleRateRial = req.SingleRateRial,
                        PeakRateRial   = req.PeakRateRial,
                        MidRateRial    = req.MidRateRial,
                        LowRateRial    = req.LowRateRial,
                        CreatedAt      = DateTime.Now,
                    });
                }

                return (object)"نرخ ذخیره شد.";
            });
        }

        // ─── GetBillHistory ───────────────────────────────────────────────────

        [HttpGet]
        [Route("[controller]/GetBillHistory/{subscriptionId}")]
        public ExecutionResult GetBillHistory(int subscriptionId)
        {
            return RunExceptionProof(() =>
            {
                var reports = Repository<BillAnalysisReport>.GetSelectiveList(
                    i => new
                    {
                        i.Id,
                        i.Year,
                        i.Month,
                        i.PeakCons,
                        i.MidCons,
                        i.LowCons,
                        i.TariffCodeOptionId,
                        i.BilateralKwh,
                        i.ExchangeKwh,
                        i.GreenLawKwh,
                        i.CostWithoutMatin,
                        i.CostWithMatin,
                        i.NetSaving,
                        i.CreatedAt,
                    },
                    i => i.SubscriptionId == subscriptionId);

                return (object)reports;
            });
        }
    }
}
