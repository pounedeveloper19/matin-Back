using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumGuaranteeType
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
