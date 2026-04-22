using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class SiteMap
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? ControlKey { get; set; }

    public int Indexer { get; set; }

    public bool IsSelectable { get; set; }

    public int? ParentId { get; set; }

    public string? Arguments { get; set; }

    public string? Description { get; set; }

    public bool IsInMenu { get; set; }

    public string? PhysicalPath { get; set; }

    public string? Icon { get; set; }

    public virtual ICollection<SiteMapRole> SiteMapRoles { get; set; } = new List<SiteMapRole>();
}
