using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class TicketMessage
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int SenderUserId { get; set; }

    public string? Body { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User SenderUser { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
