using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumContractStatus
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
