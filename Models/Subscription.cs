using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Subscription
{
    public int Id { get; set; }

    public int AddressId { get; set; }

    public string BillIdentifier { get; set; } = null!;

    public decimal? ContractCapacityKw { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual ICollection<BillAnalysisReport> BillAnalysisReports { get; set; } = new List<BillAnalysisReport>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<OptimumPurchaseSuggestion> OptimumPurchaseSuggestions { get; set; } = new List<OptimumPurchaseSuggestion>();
}
