using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class ElectricityOrder
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public int UserId { get; set; }

    public decimal RequestedKwh { get; set; }

    public int EnergyTypeId { get; set; }

    public decimal PriceAtMoment { get; set; }

    public int StatusId { get; set; }

    public DateTime? OrderDate { get; set; }

    public bool IsPriceRequest { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual EnumEnergyType EnergyType { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual EnumOrderStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
