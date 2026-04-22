using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Address
{
    public int Id { get; set; }

    public int CustomerProfileId { get; set; }

    public int PowerEntityId { get; set; }

    public int CityId { get; set; }

    public string MainAddress { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public virtual City City { get; set; } = null!;

    public virtual CustomerProfile CustomerProfile { get; set; } = null!;

    public virtual PowerEntity PowerEntity { get; set; } = null!;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
