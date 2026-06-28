namespace MatinPower.Server.Models;

public class TariffCodeOptionRate
{
    public int Id { get; set; }
    public int TariffCodeOptionId { get; set; }
    public int Year { get; set; }

    // نرخ میان‌بار — همچنین به عنوان tariffMid در محاسبه ماده ۱۶ استفاده می‌شود
    public decimal RateRialPerKwh { get; set; }

    // نرخ اوج‌بار — اگر صفر باشد فال‌بک به RateRialPerKwh × 2
    public decimal RatePeakRialPerKwh { get; set; }

    // نرخ کم‌بار — اگر صفر باشد فال‌بک به RateRialPerKwh × 0.5
    public decimal RateLowRialPerKwh { get; set; }

    public virtual TariffCodeOption TariffCodeOption { get; set; } = null!;
}
