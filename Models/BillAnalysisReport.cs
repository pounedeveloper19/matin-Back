using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class BillAnalysisReport
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public decimal? PeakCons { get; set; }

    public decimal? MidCons { get; set; }

    public decimal? LowCons { get; set; }

    public decimal? CostWithoutMatin { get; set; }

    public decimal? CostWithMatin { get; set; }

    public decimal? NetSaving { get; set; }

    public Guid? BillFileId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
