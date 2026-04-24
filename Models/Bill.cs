using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int SubscriptionId { get; set; }

    public string? BillNumber { get; set; }

    public DateTime? PeriodStart { get; set; }

    public DateTime? PeriodEnd { get; set; }

    public int? TotalConsumption { get; set; }

    public decimal? TotalAmount { get; set; }

    public int TariffId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? BillScanFileId { get; set; }

    public virtual ICollection<BillUsingByTou> BillUsingByTous { get; set; } = new List<BillUsingByTou>();

    public virtual ICollection<ElectricityOrder> ElectricityOrders { get; set; } = new List<ElectricityOrder>();

    public virtual Subscription Subscription { get; set; } = null!;
}
