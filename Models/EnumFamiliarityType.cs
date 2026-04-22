using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumFamiliarityType
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<CustomerProfile> CustomerProfiles { get; set; } = new List<CustomerProfile>();
}
