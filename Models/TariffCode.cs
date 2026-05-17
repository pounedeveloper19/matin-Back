namespace MatinPower.Server.Models;

public class TariffCode
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Title { get; set; } = null!;

    public virtual ICollection<TariffCodeOption> Options { get; set; } = new List<TariffCodeOption>();
}
