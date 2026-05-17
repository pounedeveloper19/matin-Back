using MatinPower.Infrastructure;
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

        private const int TOU_PEAK        = 1;
        private const int TOU_MID         = 2;
        private const int TOU_LOW         = 3;
        private const int TOU_FRIDAY_PEAK = 4;

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static int DaysInJalaliMonth(int month) =>
            month <= 6 ? 31 : month <= 11 ? 30 : 29;

        private (int peak, int mid, int low, int fridayPeak) GetTouHours(int month, int powerEntityId)
        {
            var schedule = Repository<Touschedule>.GetList(null, i =>
                i.MonthNumber == month &&
                (powerEntityId == 0 || i.PowerEntityId == powerEntityId));

            int peak   = schedule.Item1.Where(s => s.ToutypeId == TOU_PEAK).Select(s => s.HourNumber).Distinct().Count();
            int mid    = schedule.Item1.Where(s => s.ToutypeId == TOU_MID).Select(s => s.HourNumber).Distinct().Count();
            int low    = schedule.Item1.Where(s => s.ToutypeId == TOU_LOW).Select(s => s.HourNumber).Distinct().Count();
            int friday = schedule.Item1.Where(s => s.ToutypeId == TOU_FRIDAY_PEAK).Select(s => s.HourNumber).Distinct().Count();

            if (peak + mid + low == 0) { peak = 5; mid = 12; low = 7; }

            return (peak, mid, low, friday);
        }

        // ─── Legacy: ManualAnalysis ───────────────────────────────────────────

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

                var (peakHours, midHours, lowHours, fridayPeakHours) = GetTouHours(req.Month, powerEntityId);

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

                var (peakHours, midHours, lowHours, fridayPeakHours) = GetTouHours(req.Month, powerEntityId);

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

        // درصد مشمول ماده سبز: 4،ب=0، دیماند≤1000=0، 1404=3%، 1405=4%، بقیه=5%
        private static decimal GreenLawPercent(bool isType4B, decimal actualDemandKw, int year)
        {
            if (isType4B || actualDemandKw <= 1000m) return 0m;
            return year == 1404 ? 0.03m : year == 1405 ? 0.04m : 0.05m;
        }

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

            return RunExceptionProof(() =>
            {
                // ── 1. ساعات TOU ──────────────────────────────────────────
                int powerEntityId = address?.PowerEntityId ?? 0;
                var (peakH, midH, lowH, _) = GetTouHours(req.Month, powerEntityId);

                // ── 2. مصرف برق ───────────────────────────────────────────
                decimal peakKwh, midKwh, lowKwh;
                if (req.ConsumptionMode == "total" && req.TotalKwh.HasValue)
                {
                    decimal total = req.TotalKwh.Value;
                    peakKwh = total * peakH / 24m;
                    midKwh  = total * midH  / 24m;
                    lowKwh  = total * lowH  / 24m;
                }
                else
                {
                    peakKwh = req.PeakKwh ?? 0;
                    midKwh  = req.MidKwh  ?? 0;
                    lowKwh  = req.LowKwh  ?? 0;
                }
                decimal totalKwh = peakKwh + midKwh + lowKwh;

                // ── 3. نرخ تعرفه صنعتی ────────────────────────────────────
                // میان = پایه، کم = نصف، اوج = دو برابر
                decimal tariffMid  = tariffRate.RateRialPerKwh;
                decimal tariffLow  = tariffMid / 2m;
                decimal tariffPeak = tariffMid * 2m;

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
                decimal greenPeak = req.GreenLawKwh * (peakH / 24m);
                decimal greenMid  = req.GreenLawKwh * (midH  / 24m);
                decimal greenLow  = req.GreenLawKwh * (lowH  / 24m);

                decimal bilateralPeak = req.BilateralKwh * (peakH / 24m);
                decimal bilateralMid  = req.BilateralKwh * (midH  / 24m);
                decimal bilateralLow  = req.BilateralKwh * (lowH  / 24m);

                decimal exchangePeak = req.ExchangeKwh * (peakH / 24m);
                decimal exchangeMid  = req.ExchangeKwh * (midH  / 24m);
                decimal exchangeLow  = req.ExchangeKwh * (lowH  / 24m);

                // ── 8. کل انرژی بازار هر بازه (دوجانبه+بورس+سبز) ────────
                decimal mktEnergyPeak = bilateralPeak + exchangePeak + greenPeak;
                decimal mktEnergyMid  = bilateralMid  + exchangeMid  + greenMid;
                decimal mktEnergyLow  = bilateralLow  + exchangeLow  + greenLow;

                // ── 9. باقیمانده از شبکه توزیع ───────────────────────────
                decimal remainingPeak = Math.Max(0, peakKwh - mktEnergyPeak);
                decimal remainingMid  = Math.Max(0, midKwh  - mktEnergyMid);
                decimal remainingLow  = Math.Max(0, lowKwh  - mktEnergyLow);

                // ── 10. بهای انرژی (Σ maxWhole × مصرف یا باقیمانده) ──────
                decimal energyBefore = peakKwh * maxWholePeak + midKwh * maxWholeMid + lowKwh * maxWholeLow;
                decimal energyAfter  = remainingPeak * maxWholePeak + remainingMid * maxWholeMid + remainingLow * maxWholeLow;

                // ── 11. مابه التفاوت ماده ۱۶ ─────────────────────────────
                // قبل = greenSubject × max(greenLawRate - tariffMid, 0)
                // بعد  = max((greenSubject - greenPurchased) × (greenLawRate - tariffMid), 0)
                decimal greenDiff       = greenLawRate - tariffMid;
                decimal article16Before = greenSubjectKwh * Math.Max(greenDiff, 0);
                decimal article16After  = Math.Max((greenSubjectKwh - req.GreenLawKwh) * greenDiff, 0);

                // ── 12. مابه التفاوت اجرای مقررات ────────────────────────
                // = Σ مصرف_بازه × max(تعرفه_بازه - متوسط بازار, 0)
                // قبل: مصرف کامل؛ بعد: مصرف منهای سبز
                decimal regPeak = Math.Max(tariffPeak - avgMarket, 0);
                decimal regMid  = Math.Max(tariffMid  - avgMarket, 0);
                decimal regLow  = Math.Max(tariffLow  - avgMarket, 0);

                decimal regulatoryBefore = peakKwh * regPeak + midKwh * regMid + lowKwh * regLow;
                decimal regulatoryAfter  =
                    (peakKwh - greenPeak) * regPeak +
                    (midKwh  - greenMid)  * regMid  +
                    (lowKwh  - greenLow)  * regLow;

                // ── 13. بستانکاری خرید خارج از بازار ────────────────────
                // = Σ min((مصرف - همه خریدها) × نرخ تابلو × 0.75, 0)
                decimal creditPeak = Math.Min((peakKwh - mktEnergyPeak) * boardPeak * creditMul, 0);
                decimal creditMid  = Math.Min((midKwh  - mktEnergyMid)  * boardMid  * creditMul, 0);
                decimal creditLow  = Math.Min((lowKwh  - mktEnergyLow)  * boardLow  * creditMul, 0);
                decimal totalCredit = creditPeak + creditMid + creditLow;

                // ── 14. صورتحساب‌های بازار ───────────────────────────────
                decimal bilateralBill = req.BilateralKwh * req.BilateralRate;
                decimal exchangeBill  = req.ExchangeKwh  * req.ExchangeRate;
                decimal greenBill     = req.GreenLawKwh  * req.GreenRate;

                // ── 15. هزینه نهایی ───────────────────────────────────────
                decimal costWithoutMatin = energyBefore + article16Before + regulatoryBefore;
                decimal costWithMatin    = energyAfter  + article16After  + regulatoryAfter
                                         + totalCredit
                                         + bilateralBill + exchangeBill + greenBill;

                decimal netSaving     = costWithoutMatin - costWithMatin;
                decimal savingPercent = costWithoutMatin > 0
                    ? Math.Round(netSaving / costWithoutMatin * 100m, 2) : 0;

                // ── 16. ذخیره در دیتابیس ──────────────────────────────────
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
                    TariffPeakRial      = tariffPeak,
                    TariffMidRial       = tariffMid,
                    TariffLowRial       = tariffLow,
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
                    RegulatoryBeforeRial= regulatoryBefore,
                    EnergyAfterRial     = energyAfter,
                    Article16AfterRial  = article16After,
                    RegulatoryAfterRial = regulatoryAfter,
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
