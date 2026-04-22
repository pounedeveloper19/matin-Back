using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class TariffSlab
{
    public int Id { get; set; }

    public int TariffId { get; set; }

    public decimal FromKwh { get; set; }

    public decimal ToKwh { get; set; }

    public decimal Multiplier { get; set; }

    public virtual Tariff Tariff { get; set; } = null!;
}
