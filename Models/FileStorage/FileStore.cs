namespace MatinPower.Server.Models.FileStorage;

public class FileStore
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OriginalName { get; set; } = null!;

    public string? MimeType { get; set; }

    public string? Extension { get; set; }

    public long SizeBytes { get; set; }

    public byte[]? Data { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public int? UploadedByUserId { get; set; }

    // Which entity type owns this file: Contract, Payment, Warranty, Ticket, Profile, Bill
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }
}
