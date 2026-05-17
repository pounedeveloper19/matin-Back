namespace MatinPower.Server.Models;

public class SubscriptionRate
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public int Year  { get; set; }
    public int Month { get; set; }

    // 'single' | 'split'
    public string RateType { get; set; } = "single";

    // نرخ کلی (ریال/kWh) — وقتی RateType = 'single'
    public decimal? SingleRateRial { get; set; }

    // نرخ تفکیکی (ریال/kWh) — وقتی RateType = 'split'
    public decimal? PeakRateRial { get; set; }
    public decimal? MidRateRial  { get; set; }
    public decimal? LowRateRial  { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
