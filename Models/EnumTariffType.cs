using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumTariffType
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();
}
