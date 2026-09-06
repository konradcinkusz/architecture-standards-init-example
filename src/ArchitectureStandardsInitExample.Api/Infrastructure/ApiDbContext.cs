using ArchitectureStandardsInitExample.Api.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<BootRecord> BootRecords => Set<BootRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BootRecord>(entity =>
        {
            entity.ToTable("boot_records");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Version).HasMaxLength(64).IsRequired();
            entity.Property(record => record.Environment).HasMaxLength(64).IsRequired();
            entity.Property(record => record.DatabaseProvider).HasMaxLength(32).IsRequired();
            entity.Property(record => record.DegradedIntegrations).HasMaxLength(512);

            // The list endpoint's only sort. An index that matches the query the
            // endpoint actually issues, rather than every column just in case.
            entity.HasIndex(record => record.RecordedAt).IsDescending();
        });

        // No HasData anywhere. Migrations describe schema; reference data is
        // seeded separately (P4) — HasData embeds the whole dataset in every
        // migration snapshot and makes the diffs unreviewable.
    }
}
