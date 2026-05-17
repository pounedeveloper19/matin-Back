namespace MatinPower.Server.Models;

public class TariffCodeOption
{
    public int Id { get; set; }
    public int TariffCodeId { get; set; }
    public string Title { get; set; } = null!;
    public decimal PenaltyMultiplier { get; set; } = 1.3m;
    public decimal CreditMultiplier  { get; set; } = 0.75m;

    public virtual TariffCode TariffCode { get; set; } = null!;
    public virtual ICollection<CustomerProfile> CustomerProfiles { get; set; } = new List<CustomerProfile>();
    public virtual ICollection<TariffCodeOptionRate> Rates { get; set; } = new List<TariffCodeOptionRate>();
}
