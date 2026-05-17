namespace MatinPower.Server.Models;

public partial class BillAnalysisReport
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }

    // مصرف به تفکیک بازه TOU
    public decimal? PeakCons { get; set; }
    public decimal? MidCons  { get; set; }
    public decimal? LowCons  { get; set; }

    // ورودی‌های تحلیل پیشرفته
    public int?     TariffCodeOptionId { get; set; }
    public decimal? ContractDemandKw   { get; set; }  // دیماند قراردادی
    public decimal? ActualDemandKw     { get; set; }  // دیماند مصرفی

    // انرژی‌های خریداری‌شده
    public decimal? BilateralKwh   { get; set; }  // قرارداد دوجانبه
    public decimal? BilateralRate  { get; set; }  // نرخ دوجانبه (ریال/kWh)
    public decimal? ExchangeKwh    { get; set; }  // بورس
    public decimal? ExchangeRate   { get; set; }  // نرخ بورس (ریال/kWh)
    public decimal? GreenLawKwh    { get; set; }  // قانون جهش تولید
    public decimal? GreenRate      { get; set; }  // نرخ برق سبز (ریال/kWh)

    // نتایج مالی
    public decimal? CostWithoutMatin  { get; set; }  // هزینه قبل از قرارداد
    public decimal? CostWithMatin     { get; set; }  // هزینه بعد از قرارداد
    public decimal? NetSaving         { get; set; }  // صرفه‌جویی
    public decimal? BilateralBillRial { get; set; }  // صورتحساب دوجانبه
    public decimal? ExchangeBillRial  { get; set; }  // صورتحساب بورس
    public decimal? GreenBillRial     { get; set; }  // صورتحساب برق سبز

    public Guid?     BillFileId { get; set; }
    public DateTime? CreatedAt  { get; set; }

    public virtual Subscription      Subscription      { get; set; } = null!;
    public virtual TariffCodeOption? TariffCodeOption  { get; set; }
}
