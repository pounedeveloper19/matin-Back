namespace MatinPower.Server.Models.Body
{
    public class ManualBillAnalysisRequest
    {
        public int SubscriptionId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal PeakKwh { get; set; }
        public decimal MidKwh { get; set; }
        public decimal LowKwh { get; set; }
        public decimal FridayPeakKwh { get; set; }
    }

    public class BillAnalysisBand
    {
        public string Name { get; set; }
        public decimal ActualKwh { get; set; }
        public decimal ContractedKwh { get; set; }
        public decimal ExcessKwh { get; set; }
        public decimal DeficitKwh { get; set; }
        public decimal MarketRateRial { get; set; }
        public decimal PenaltyRial { get; set; }
        public decimal CreditRial { get; set; }
    }

    public class BillAnalysisResult
    {
        public string MonthName { get; set; }
        public int Year { get; set; }
        public decimal TotalConsumption { get; set; }
        public decimal ContractCapacityKw { get; set; }
        public decimal ContractedEnergyKwh { get; set; }
        public decimal ContractRateRialPerKwh { get; set; }
        public int PeakHoursPerDay { get; set; }
        public int MidHoursPerDay { get; set; }
        public int LowHoursPerDay { get; set; }
        public List<BillAnalysisBand> Bands { get; set; }
        public decimal MarketPeakRate { get; set; }
        public decimal MarketMidRate { get; set; }
        public decimal MarketLowRate { get; set; }
        public decimal BackupRate { get; set; }
        public decimal TotalDifferentialRial { get; set; }
        public decimal TotalCreditRial { get; set; }
        public decimal Article16Rial { get; set; }
        public decimal FuelFeeRial { get; set; }
        public decimal MatinBillRial { get; set; }
        public decimal WithoutMatinBillRial { get; set; }
        public decimal WithMatinBillRial { get; set; }
        public decimal SavingRial { get; set; }
        public decimal SavingPercent { get; set; }
    }

    public class OptimalPurchaseCurvePoint
    {
        public decimal ContractCapacityKw { get; set; }
        public decimal ContractedEnergyKwh { get; set; }
        public decimal SavingRial { get; set; }
        public decimal WithMatinBillRial { get; set; }
    }

    public class OptimalPurchaseCurveResult
    {
        public decimal CurrentContractCapacityKw { get; set; }
        public decimal SavingAtCurrentContractRial { get; set; }
        public decimal OptimalContractCapacityKw { get; set; }
        public decimal OptimalSavingRial { get; set; }
        public decimal WithoutMatinBillRial { get; set; }
        public List<OptimalPurchaseCurvePoint> Points { get; set; }
    }

    // ─── Advanced Bill Analysis (Excel-based model) ───────────────────────────

    public class AdvancedBillAnalysisRequest
    {
        public int SubscriptionId { get; set; }
        public int Year  { get; set; }   // Jalali year e.g. 1404
        public int Month { get; set; }   // Jalali month 1-12

        // مصرف: "split" = هر بازه جداگانه | "total" = کل مصرف (سیستم توزیع می‌کند)
        public string ConsumptionMode { get; set; } = "split";
        public decimal? TotalKwh { get; set; }  // برای حالت total
        public decimal? PeakKwh  { get; set; }  // اوج بار
        public decimal? MidKwh   { get; set; }  // میان بار
        public decimal? LowKwh   { get; set; }  // کم بار

        // دیماند
        public decimal ContractDemandKw { get; set; }
        public decimal ActualDemandKw   { get; set; }

        // انرژی خریداری‌شده از بازار
        public decimal BilateralKwh  { get; set; }   // قرارداد دوجانبه (kWh)
        public decimal BilateralRate { get; set; }   // نرخ دوجانبه (ریال/kWh)
        public decimal ExchangeKwh   { get; set; }   // بورس (kWh)
        public decimal ExchangeRate  { get; set; }   // نرخ بورس (ریال/kWh)
        public decimal GreenLawKwh   { get; set; }   // قانون جهش تولید (kWh)
        public decimal GreenRate     { get; set; }   // نرخ برق سبز (ریال/kWh)

        // ── [RESERVED — Phase 3: Simulation / What-If / Custom Negotiation] ──
        // UseCustomRate و فیلدهای زیر در AdvancedAnalysis فعلاً استفاده نمی‌شوند.
        // در Phase 3 برای سناریوهای "اگر نرخ قرارداد X بود چقدر صرفه‌جویی داشتیم؟"
        // یا مذاکره تعرفه شخصی‌سازی‌شده استفاده خواهند شد.
        // تغییر ندهید بدون به‌روزرسانی IOptimizationStrategy input model.
        public bool     UseCustomRate  { get; set; } = false;
        public string?  RateType       { get; set; } = "single";  // "single" | "split"
        public decimal? SingleRateRial { get; set; }
        public decimal? PeakRateRial   { get; set; }
        public decimal? MidRateRial    { get; set; }
        public decimal? LowRateRial    { get; set; }

        public bool SaveReport { get; set; } = true;
    }

    public class AdvancedBillAnalysisResult
    {
        public string MonthName { get; set; } = "";
        public int Year  { get; set; }
        public int Month { get; set; }

        // مصرف برق (kWh)
        public decimal PeakKwh  { get; set; }
        public decimal MidKwh   { get; set; }
        public decimal LowKwh   { get; set; }
        public decimal TotalKwh { get; set; }

        // ساعات TOU (ساعت در روز)
        public int PeakHoursPerDay { get; set; }
        public int MidHoursPerDay  { get; set; }
        public int LowHoursPerDay  { get; set; }

        // نرخ تعرفه صنعتی (ریال/kWh): میان=پایه، کم=نصف، اوج=دو برابر
        public decimal TariffPeakRial { get; set; }
        public decimal TariffMidRial  { get; set; }
        public decimal TariffLowRial  { get; set; }

        // حداکثر نرخ بازار عمده‌فروشی = 1.3 × حداکثر بازار TOU (ریال/kWh)
        public decimal MaxWholePeak { get; set; }
        public decimal MaxWholeMid  { get; set; }
        public decimal MaxWholeLow  { get; set; }

        // متوسط قیمت بازار و نرخ قانون جهش (ریال/kWh)
        public decimal AvgMarket    { get; set; }
        public decimal GreenLawRate { get; set; }

        // مصرف برق مشمول قانون جهش تولید
        public decimal GreenPercent    { get; set; }   // درصد (مثلاً 0.03)
        public decimal GreenSubjectKwh { get; set; }   // کل مصرف × درصد

        // انرژی بازار تقسیم‌شده بر بازه‌های TOU (kWh)
        public decimal MarketEnergyPeak { get; set; }
        public decimal MarketEnergyMid  { get; set; }
        public decimal MarketEnergyLow  { get; set; }

        // باقیمانده از شبکه توزیع (kWh) — پس از کسر همه خریدهای بازار
        public decimal RemainingPeak { get; set; }
        public decimal RemainingMid  { get; set; }
        public decimal RemainingLow  { get; set; }

        // اجزای هزینه قبل از قرارداد (ریال)
        public decimal EnergyBeforeRial      { get; set; }  // بهای انرژی = Σ maxWhole × consumption
        public decimal Article16BeforeRial   { get; set; }  // مابه التفاوت ماده 16
        public decimal RegulatoryBeforeRial  { get; set; }  // مابه التفاوت اجرای مقررات

        // اجزای هزینه بعد از قرارداد (ریال)
        public decimal EnergyAfterRial       { get; set; }  // بهای انرژی = Σ maxWhole × remaining
        public decimal Article16AfterRial    { get; set; }  // مابه التفاوت ماده 16
        public decimal RegulatoryAfterRial   { get; set; }  // مابه التفاوت اجرای مقررات
        public decimal CreditRial            { get; set; }  // بستانکاری خرید خارج از بازار

        // صورتحساب‌های بازار (ریال)
        public decimal BilateralBillRial { get; set; }
        public decimal ExchangeBillRial  { get; set; }
        public decimal GreenBillRial     { get; set; }

        // نتایج نهایی (ریال)
        public decimal CostWithoutMatin { get; set; }
        public decimal CostWithMatin    { get; set; }
        public decimal NetSaving        { get; set; }
        public decimal SavingPercent    { get; set; }
    }

    // ─── Portfolio Optimization Request ──────────────────────────────────────

    /// <summary>
    /// Request body for GetOptimalPortfolio endpoint.
    /// Specifies consumption, market prices, and hard constraints.
    /// The solver finds the minimum-cost mix of (Exchange, Green, Bilateral, Grid).
    /// </summary>
    public class PortfolioOptimizationRequest
    {
        public int SubscriptionId { get; set; }
        public int Year  { get; set; }
        public int Month { get; set; }

        // ── Consumption — same semantics as AdvancedBillAnalysisRequest ──────
        public string   ConsumptionMode { get; set; } = "split";
        public decimal? TotalKwh        { get; set; }
        public decimal? PeakKwh         { get; set; }
        public decimal? MidKwh          { get; set; }
        public decimal? LowKwh          { get; set; }

        // ── Demand ───────────────────────────────────────────────────────────
        public decimal ActualDemandKw { get; set; }

        // ── Market prices (ریال/kWh) — what we'd pay per channel ─────────────
        public decimal ExchangeRate  { get; set; }
        public decimal GreenRate     { get; set; }
        public decimal BilateralRate { get; set; }

        // ── Constraints: capacity limits per channel (kWh) ───────────────────
        /// Maximum kWh available from exchange (0 = unconstrained up to TotalKwh)
        public decimal MaxExchangeKwh       { get; set; } = 0m;
        /// Maximum green energy purchasable in market (0 = GreenSubjectKwh as cap)
        public decimal GreenAvailabilityKwh { get; set; } = 0m;
        /// Maximum bilateral contract volume available (0 = unconstrained)
        public decimal MaxBilateralKwh      { get; set; } = 0m;

        /// Minimum kWh that must stay on grid (operational/reliability reserve).
        /// Example: 0.1 × TotalKwh = 10% reserve.
        public decimal OperationalReserveKwh { get; set; } = 0m;
    }

    public class SubscriptionRateRequest
    {
        public int SubscriptionId { get; set; }
        public int Year  { get; set; }
        public int Month { get; set; }
        public string  RateType       { get; set; } = "single";
        public decimal? SingleRateRial { get; set; }
        public decimal? PeakRateRial   { get; set; }
        public decimal? MidRateRial    { get; set; }
        public decimal? LowRateRial    { get; set; }
    }
}
