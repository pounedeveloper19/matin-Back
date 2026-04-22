using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public int MethodId { get; set; }

    public int StatusId { get; set; }

    public string? ReferenceNumber { get; set; }

    public Guid? ReceiptFileId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual EnumPaymentMethod Method { get; set; } = null!;

    public virtual ElectricityOrder Order { get; set; } = null!;

    public virtual EnumPaymentStatus Status { get; set; } = null!;
}
