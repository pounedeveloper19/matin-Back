using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Contract
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public string? ContractNumber { get; set; }

    public decimal ContractRate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int StatusId { get; set; }

    public virtual EnumContractStatus Status { get; set; } = null!;

    public virtual Subscription Subscription { get; set; } = null!;

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
