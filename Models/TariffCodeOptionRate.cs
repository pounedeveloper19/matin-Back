namespace MatinPower.Server.Models;

public class TariffCodeOptionRate
{
    public int Id { get; set; }
    public int TariffCodeOptionId { get; set; }
    public int Year { get; set; }
    public decimal RateRialPerKwh { get; set; }

    public virtual TariffCodeOption TariffCodeOption { get; set; } = null!;
}
