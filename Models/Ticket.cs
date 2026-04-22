using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public int CustomerProfileId { get; set; }

    public string? Subject { get; set; }

    public int StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CustomerProfile CustomerProfile { get; set; } = null!;

    public virtual EnumTicketStatus Status { get; set; } = null!;

    public virtual ICollection<TicketMessage> TicketMessages { get; set; } = new List<TicketMessage>();
}
