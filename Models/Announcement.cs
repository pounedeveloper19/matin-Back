using System;
using System.Collections.Generic;

namespace MatinPower.Server.Models;

public partial class Announcement
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Contents { get; set; } = null!;

    public DateTime PublishDate { get; set; }

    public DateTime? FinishDate { get; set; }
}
