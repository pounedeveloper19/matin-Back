namespace MatinPower.Server.Application.Optimization;

/// <summary>
/// All inputs the optimization engine needs — independent of DB or HTTP context.
/// Built by the orchestration layer (Controller/Service) after resolving tariff and market data.
/// </summary>
public class EnergyOptimizationInput
{
    // ── مصرف برق (kWh) ────────────────────────────────────────────────
    public decimal TotalKwh { get; set; }
    public decimal PeakKwh  { get; set; }
    public decimal MidKwh   { get; set; }
    public decimal LowKwh   { get; set; }

    // ── نسبت ساعات TOU (0–1) ─────────────────────────────────────────
    public decimal PeakRatio { get; set; }   // peakHours / 24
    public decimal MidRatio  { get; set; }   // midHours  / 24
    public decimal LowRatio  { get; set; }   // lowHours  / 24

    // ── مصرف مشمول قانون جهش تولید (kWh) ───────────────────────────
    public decimal GreenSubjectKwh { get; set; }

    // ── نرخ‌های خرید از بازار (ریال/kWh) ────────────────────────────
    public decimal ExchangePrice  { get; set; }
    public decimal GreenPrice     { get; set; }
    public decimal BilateralPrice { get; set; }

    // ── نرخ‌های مرجع برای مقایسه (ریال/kWh) ─────────────────────────
    // حداکثر نرخ عمده‌فروشی شبکه = penaltyMul × marketMax
    public decimal EffectiveGridRatePeak { get; set; }
    public decimal EffectiveGridRateMid  { get; set; }
    public decimal EffectiveGridRateLow  { get; set; }
    public decimal GreenLawRate          { get; set; }  // نرخ قانون جهش
    public decimal TariffMidRate         { get; set; }  // نرخ پایه تعرفه صنعتی

    // ── سقف ظرفیت بازار (kWh) — null = بدون محدودیت ─────────────────
    public decimal? ExchangeMarketCapacity { get; set; }
}
