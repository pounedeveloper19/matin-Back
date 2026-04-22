using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Role
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<SiteMapRole> SiteMapRoles { get; set; } = new List<SiteMapRole>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
