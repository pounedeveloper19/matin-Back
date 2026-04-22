using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumPaymentStatus
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
