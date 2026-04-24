using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class CustomersLegal
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = null!;

    public string NationalId { get; set; } = null!;

    public string? EconomicCode { get; set; }

    public string? CeoFullName { get; set; }

    public string CeoMobile { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual CustomerProfile CustomerProfile { get; set; } = null!;
}
