using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class SiteMapRole
{
    public int Id { get; set; }

    public int SiteMapId { get; set; }

    public int RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual SiteMap SiteMap { get; set; } = null!;
}
