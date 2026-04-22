using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class User
{
    public int Id { get; set; }

    public int? CustomerProfileId { get; set; }

    public string? FullName { get; set; }

    public string Mobile { get; set; } = null!;

    public bool? IsActive { get; set; }

    public string? Password { get; set; }

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ICollection<ElectricityOrder> ElectricityOrders { get; set; } = new List<ElectricityOrder>();

    public virtual ICollection<TicketMessage> TicketMessages { get; set; } = new List<TicketMessage>();
}
