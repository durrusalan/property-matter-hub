using Microsoft.EntityFrameworkCore;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client>        Clients        => Set<Client>();
    public DbSet<Matter>        Matters        => Set<Matter>();
    public DbSet<Document>      Documents      => Set<Document>();
    public DbSet<KeyDate>       KeyDates       => Set<KeyDate>();
    public DbSet<EmailRecord>   EmailRecords   => Set<EmailRecord>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Template>      Templates      => Set<Template>();
    public DbSet<SyncLog>       SyncLogs       => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client
        modelBuilder.Entity<Client>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).HasMaxLength(200);
            e.HasIndex(c => c.Email);
        });

        // Matter
        modelBuilder.Entity<Matter>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.MatterRef).IsRequired().HasMaxLength(50);
            e.HasIndex(m => m.MatterRef).IsUnique();
            e.Property(m => m.Title).IsRequired().HasMaxLength(300);
            e.Property(m => m.Status).HasConversion<string>();
            e.HasOne(m => m.Client)
             .WithMany(c => c.Matters)
             .HasForeignKey(m => m.ClientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Document
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.FilePath);
            e.HasOne(d => d.Matter)
             .WithMany(m => m.Documents)
             .HasForeignKey(d => d.MatterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // KeyDate
        modelBuilder.Entity<KeyDate>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Severity).HasConversion<string>();
            e.HasOne(k => k.Matter)
             .WithMany(m => m.KeyDates)
             .HasForeignKey(k => k.MatterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailRecord
        modelBuilder.Entity<EmailRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.GmailMessageId).IsUnique();
            e.Property(r => r.Direction).HasConversion<string>();
            e.Property(r => r.ClassificationStatus).HasConversion<string>();
            e.HasOne(r => r.Matter)
             .WithMany(m => m.Emails)
             .HasForeignKey(r => r.MatterId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CalendarEvent
        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.HasKey(ce => ce.Id);
            e.HasIndex(ce => ce.GoogleEventId);
            e.Property(ce => ce.SyncStatus).HasConversion<string>();
            e.HasOne(ce => ce.Matter)
             .WithMany(m => m.CalendarEvents)
             .HasForeignKey(ce => ce.MatterId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // SyncLog
        modelBuilder.Entity<SyncLog>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.ResourceType, s.ResourceKey });
        });

        // Enable WAL mode and busy timeout via connection string — done in DbContextFactory
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured) return;
        // WAL mode + busy timeout applied after connection opens
    }
}
