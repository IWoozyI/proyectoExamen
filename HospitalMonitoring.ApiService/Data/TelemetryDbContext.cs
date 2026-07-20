using Microsoft.EntityFrameworkCore;

namespace HospitalMonitoring.ApiService.Data;

public class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public DbSet<VitalSignRecord> VitalSignRecords => Set<VitalSignRecord>();
    public DbSet<PendingMessage> PendingMessages => Set<PendingMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VitalSignRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.RecordedAtUtc);
        });

        modelBuilder.Entity<PendingMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IsProcessed, e.CreatedAtUtc });
        });
    }
}
