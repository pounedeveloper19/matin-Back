using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class OptimumPurchaseSuggestion
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }

    public decimal SuggestedKwh { get; set; }

    public decimal? EstimatedSaving { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
