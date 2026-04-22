using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Guarantee
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public decimal Amount { get; set; }

    public int TypeId { get; set; }

    public Guid? FileId { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual EnumGuaranteeType Type { get; set; } = null!;
}
