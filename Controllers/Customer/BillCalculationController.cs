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

        // ToutypeId assumptions: 1=Peak, 2=Mid, 3=Low, 4=FridayPeak
        private const int TOU_PEAK = 1;
        private const int TOU_MID = 2;
        private const int TOU_LOW = 3;
        private const int TOU_FRIDAY_PEAK = 4;

        [HttpPost]
        [Route("[controller]/ManualAnalysis")]
        public ExecutionResult ManualAnalysis([FromBody] ManualBillAnalysisRequest req)
        {
            if (req.Month < 1 || req.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            return RunExceptionProof(() =>
            {
                // 1. اشتراک
                var subscription = Repository<Subscription>.GetLast(i => i.Id == req.SubscriptionId);
                if (subscription == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

                decimal contractCapacity = subscription.ContractCapacityKw ?? 0;
                if (contractCapacity <= 0)
                    return new ExecutionResult(ResultType.Danger, "خطا", "قدرت قراردادی اشتراک تنظیم نشده است.", 400);

                // 2. قرارداد فعال
                var contract = Repository<Contract>.GetLast(i => i.SubscriptionId == req.SubscriptionId);
                decimal contractRate = contract?.ContractRate ?? 0;

                // 3. نرخ‌های ماه
                var marketRate = Repository<MonthlyMarketRate>.GetLast(i =>
                    i.Year == req.Year && i.Month == req.Month);
                if (marketRate == null)
                    return new ExecutionResult(ResultType.Danger, "خطا",
                        $"نرخ بازار برای {JalaliMonths[req.Month]} {req.Year} ثبت نشده است.", 404);

                // 4. ساعات TOU از جدول برنامه (Touschedule)
                var address = Repository<Address>.GetLast(i => i.Id == subscription.AddressId);
                int powerEntityId = address?.PowerEntityId ?? 0;

                var schedule = Repository<Touschedule>.GetList(null, i =>
                    i.MonthNumber == req.Month &&
                    (powerEntityId == 0 || i.PowerEntityId == powerEntityId));

                int peakHours = schedule.Item1
                    .Where(s => s.ToutypeId == TOU_PEAK)
                    .Select(s => s.HourNumber).Distinct().Count();
                int midHours = schedule.Item1
                    .Where(s => s.ToutypeId == TOU_MID)
                    .Select(s => s.HourNumber).Distinct().Count();
                int lowHours = schedule.Item1
                    .Where(s => s.ToutypeId == TOU_LOW)
                    .Select(s => s.HourNumber).Distinct().Count();
                int fridayPeakHours = schedule.Item1
                    .Where(s => s.ToutypeId == TOU_FRIDAY_PEAK)
                    .Select(s => s.HourNumber).Distinct().Count();

                // اگر برنامه‌ای ثبت نشده بود، از مقادیر پیش‌فرض استفاده کن
                if (peakHours + midHours + lowHours == 0)
                {
                    peakHours = 2; midHours = 13; lowHours = 9;
                }

                // 5. انرژی قراردادی کل و هر باند
                decimal totalContracted = contractCapacity * 720m;
                decimal contractedPeak        = totalContracted * peakHours        / 24m;
                decimal contractedMid         = totalContracted * midHours         / 24m;
                decimal contractedLow         = totalContracted * lowHours         / 24m;
                decimal contractedFridayPeak  = totalContracted * fridayPeakHours  / 24m;

                // 6. نرخ‌های بازار
                decimal mktPeak = marketRate.MarketPeak ?? 0;
                decimal mktMid  = marketRate.MarketMid  ?? 0;
                decimal mktLow  = marketRate.MarketLow  ?? 0;
                decimal backup  = marketRate.BackupRate  ?? 0;

                // 7. محاسبه هر باند
                var bands = new List<BillAnalysisBand>();

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
                        Name          = name,
                        ActualKwh     = actual,
                        ContractedKwh = contracted,
                        ExcessKwh     = excess,
                        DeficitKwh    = deficit,
                        MarketRateRial = mktRate,
                        PenaltyRial   = penalty,
                        CreditRial    = credit,
                    });
                }

                CalcBand("اوج بار",          req.PeakKwh,        contractedPeak,       mktPeak);
                CalcBand("میان بار",         req.MidKwh,         contractedMid,        mktMid);
                CalcBand("کم بار",           req.LowKwh,         contractedLow,        mktLow);
                CalcBand("اوج بار جمعه",    req.FridayPeakKwh,  contractedFridayPeak, mktPeak);

                decimal totalCons = req.PeakKwh + req.MidKwh + req.LowKwh + req.FridayPeakKwh;

                // 8. هزینه‌های جانبی
                decimal article16 = totalCons * (marketRate.Article16Rate ?? 0);
                decimal fuelFee   = totalCons * (marketRate.FuelFee       ?? 0);

                // 9. صورتحساب متین
                decimal matinBill = contractRate * totalContracted;

                // 10. سناریو بدون قرارداد متین (نرخ پشتیبان ×۱.۳)
                decimal withoutMatin = req.PeakKwh       * 1.3m * mktPeak
                                     + req.MidKwh        * 1.3m * mktMid
                                     + req.LowKwh        * 1.3m * mktLow
                                     + req.FridayPeakKwh * 1.3m * mktPeak;

                // 11. صورتحساب با قرارداد متین
                decimal withMatin = matinBill + totalDiff - totalCredit + article16 + fuelFee;

                decimal saving        = withoutMatin - withMatin;
                decimal savingPercent = withoutMatin > 0 ? (saving / withoutMatin * 100m) : 0;

                // 12. ذخیره گزارش
                Repository<BillAnalysisReport>.InsertItem(new BillAnalysisReport
                {
                    SubscriptionId  = req.SubscriptionId,
                    Year            = req.Year,
                    Month           = req.Month,
                    PeakCons        = req.PeakKwh,
                    MidCons         = req.MidKwh,
                    LowCons         = req.LowKwh,
                    CostWithoutMatin = withoutMatin,
                    CostWithMatin   = withMatin,
                    NetSaving       = saving,
                    CreatedAt       = DateTime.Now,
                });

                var result = new BillAnalysisResult
                {
                    MonthName            = JalaliMonths[req.Month],
                    Year                 = req.Year,
                    TotalConsumption     = totalCons,
                    ContractCapacityKw   = contractCapacity,
                    ContractedEnergyKwh  = totalContracted,
                    ContractRateRialPerKwh = contractRate,
                    PeakHoursPerDay      = peakHours,
                    MidHoursPerDay       = midHours,
                    LowHoursPerDay       = lowHours,
                    Bands                = bands,
                    MarketPeakRate       = mktPeak,
                    MarketMidRate        = mktMid,
                    MarketLowRate        = mktLow,
                    BackupRate           = backup,
                    TotalDifferentialRial = totalDiff,
                    TotalCreditRial      = totalCredit,
                    Article16Rial        = article16,
                    FuelFeeRial          = fuelFee,
                    MatinBillRial        = matinBill,
                    WithoutMatinBillRial = withoutMatin,
                    WithMatinBillRial    = withMatin,
                    SavingRial           = saving,
                    SavingPercent        = Math.Round(savingPercent, 2),
                };

                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }

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
                        i.CostWithoutMatin,
                        i.CostWithMatin,
                        i.NetSaving,
                        i.CreatedAt,
                    },
                    i => i.SubscriptionId == subscriptionId);

                return new ExecutionResult(ResultType.Success, null, null, 200, reports);
            });
        }
    }
}
