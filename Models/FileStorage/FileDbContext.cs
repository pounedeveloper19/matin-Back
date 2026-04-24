using Microsoft.EntityFrameworkCore;

namespace MatinPower.Server.Models.FileStorage;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    public DbSet<FileStore> FileStore { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileStore>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.OriginalName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(200);
            entity.Property(e => e.Extension).HasMaxLength(20);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Data).IsRequired(false);

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(e => e.UploadedByUserId)
                .HasFilter("[IsDeleted] = 0");
        });
    }
}
