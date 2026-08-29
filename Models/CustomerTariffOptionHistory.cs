using System;

namespace MatinPower.Server.Models;

public partial class CustomerTariffOptionHistory
{
    public int Id { get; set; }

    public int CustomerProfileId { get; set; }

    public int TariffCodeOptionId { get; set; }

    public int EffectiveYear { get; set; }

    public int EffectiveMonth { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CustomerProfile CustomerProfile { get; set; } = null!;

    public virtual TariffCodeOption TariffCodeOption { get; set; } = null!;
}
