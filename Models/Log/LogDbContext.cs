using Microsoft.EntityFrameworkCore;

namespace MatinPower.Server.Models.Log;

public static class AuditType
{
    public const int Insert = 1;
    public const int Update = 2;
    public const int Delete = 3;
}

public class AuditTrailEntry
{
    public long   AuditId     { get; set; }
    public string? TableName  { get; set; }
    public int?   RecordId    { get; set; }
    public int?   AuditTypeId { get; set; }
    public string? OldValue   { get; set; }
    public string? NewValue   { get; set; }
    public int?   UserId      { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class ApplicationLogEntry
{
    public long     LogId     { get; set; }
    public DateTime LoggedAt  { get; set; }
    public string?  Level     { get; set; }
    public string?  Logger    { get; set; }
    public string?  Message   { get; set; }
    public string?  Exception { get; set; }
    public int?     UserId    { get; set; }
    public string?  Action    { get; set; }
    public string?  Entity    { get; set; }
    public string?  EntityId  { get; set; }
    public string?  ClientIp  { get; set; }
}

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options) { }

    public DbSet<AuditTrailEntry>    AuditTrail      { get; set; }
    public DbSet<ApplicationLogEntry> ApplicationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<AuditTrailEntry>()
            .ToTable("AuditTrail")
            .HasKey(e => e.AuditId);

        mb.Entity<ApplicationLogEntry>()
            .ToTable("ApplicationLogs")
            .HasKey(e => e.LogId);
    }
}
