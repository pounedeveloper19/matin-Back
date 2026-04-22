using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class BillUsingByTou
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public int ToutypeId { get; set; }

    public int ConsumptionKwh { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual EnumToutype Toutype { get; set; } = null!;
}
