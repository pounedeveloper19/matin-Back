using System;

namespace MatinPower.Server.Models;

public partial class CustomerDocument
{
    public int Id { get; set; }

    public int CustomerProfileId { get; set; }

    public Guid FileId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual CustomerProfile CustomerProfile { get; set; } = null!;
}
