using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Touschedule
{
    public int Id { get; set; }

    public int PowerEntityId { get; set; }

    public int MonthNumber { get; set; }

    public int HourNumber { get; set; }

    public int ToutypeId { get; set; }

    public virtual PowerEntity PowerEntity { get; set; } = null!;

    public virtual EnumToutype Toutype { get; set; } = null!;
}
