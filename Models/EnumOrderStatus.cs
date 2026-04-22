using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumOrderStatus
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<ElectricityOrder> ElectricityOrders { get; set; } = new List<ElectricityOrder>();
}
