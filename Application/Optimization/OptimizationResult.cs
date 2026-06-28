namespace MatinPower.Server.Application.Optimization;

/// <summary>
/// Result from any IOptimizationStrategy implementation.
/// Contains the recommended energy mix + reasoning for the Explainability Layer.
/// </summary>
public class OptimizationResult
{
    // ── ترکیب پیشنهادی خرید (kWh) ────────────────────────────────────
    public decimal GridKwh      { get; set; }
    public decimal ExchangeKwh  { get; set; }
    public decimal GreenKwh     { get; set; }
    public decimal BilateralKwh { get; set; }

    // ── نتایج مالی تخمینی (ریال) ─────────────────────────────────────
    public decimal EstimatedSavingAmount  { get; set; }
    public decimal EstimatedSavingPercent { get; set; }

    // ── لایه Explainability ──────────────────────────────────────────
    public List<OptimizationReason> Reasoning   { get; set; } = [];
    public List<string>             Constraints { get; set; } = [];
}

/// <summary>
/// One reasoning step — why a particular energy source was chosen or skipped.
/// </summary>
public class OptimizationReason
{
    /// <summary>"exchange" | "green" | "bilateral" | "grid"</summary>
    public string EnergyType { get; set; } = "";

    /// <summary>پیام قابل نمایش به کاربر (فارسی)</summary>
    public string Message { get; set; } = "";

    /// <summary>آیا این strategy در نتیجه نهایی فعال شد؟</summary>
    public bool IsActive { get; set; }

    /// <summary>مقایسه عددی که منجر به تصمیم شد (ریال/kWh)</summary>
    public decimal? EffectiveCost      { get; set; }
    public decimal? AlternativeCost    { get; set; }
}
