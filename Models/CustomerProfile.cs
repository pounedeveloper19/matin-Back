using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class CustomerProfile
{
    public int Id { get; set; }

    public int CustomerTypeId { get; set; }

    public int? AddressId { get; set; }

    public bool? IsActive { get; set; }

    public int? FamiliarityType { get; set; }

    public Guid? IdentityDocFileId { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual EnumCustomerType CustomerType { get; set; } = null!;

    public virtual CustomersLegal? CustomersLegal { get; set; }

    public virtual CustomersReal? CustomersReal { get; set; }

    public virtual EnumFamiliarityType? FamiliarityTypeNavigation { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
