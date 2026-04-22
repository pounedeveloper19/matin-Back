using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumCustomerType
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<CustomerProfile> CustomerProfiles { get; set; } = new List<CustomerProfile>();

    public virtual ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();
}
