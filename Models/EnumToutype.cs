using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class EnumToutype
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public virtual ICollection<BillUsingByTou> BillUsingByTous { get; set; } = new List<BillUsingByTou>();

    public virtual ICollection<Touschedule> Touschedules { get; set; } = new List<Touschedule>();
}
