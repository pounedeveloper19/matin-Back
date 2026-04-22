using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumTicketStatus
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
