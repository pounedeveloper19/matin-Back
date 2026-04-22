using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class City
{
    public int Id { get; set; }

    public int ProvinceId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Province Province { get; set; } = null!;
}
