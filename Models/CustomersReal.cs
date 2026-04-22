using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class CustomersReal : CustomerProfile
{

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string NationalCode { get; set; } = null!;

    public string Mobile { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual CustomerProfile IdNavigation { get; set; } = null!;
}
