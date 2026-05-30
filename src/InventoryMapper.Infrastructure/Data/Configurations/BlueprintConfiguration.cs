using InventoryMapper.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryMapper.Infrastructure.Data.Configurations;

public class BlueprintConfiguration : IEntityTypeConfiguration<Blueprint>
{
    public void Configure(EntityTypeBuilder<Blueprint> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.FileStoragePath).HasMaxLength(1000);
        builder.Property(x => x.MimeType).HasMaxLength(100);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Blueprints)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Annotations)
            .WithOne(x => x.Blueprint)
            .HasForeignKey(x => x.BlueprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Zones)
            .WithOne(x => x.Blueprint)
            .HasForeignKey(x => x.BlueprintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Building).HasMaxLength(200);
        builder.Property(x => x.Floor).HasMaxLength(50);
        builder.Property(x => x.Room).HasMaxLength(100);
        builder.Property(x => x.Rack).HasMaxLength(100);
        builder.Property(x => x.Site).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.VirtualEnvironment).HasMaxLength(200);
        builder.Property(x => x.ClusterName).HasMaxLength(200);
        builder.Property(x => x.CloudProvider).HasMaxLength(100);
        builder.Property(x => x.CloudRegion).HasMaxLength(100);
        builder.Property(x => x.LocationType).HasConversion<string>();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AlertConfiguration : IEntityTypeConfiguration<AlertNotification>
{
    public void Configure(EntityTypeBuilder<AlertNotification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.AlertType).HasConversion<string>();
        builder.Property(x => x.Severity).HasConversion<string>();
        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => x.CreatedAt);
    }
}

public class MonitoringRecordConfiguration : IEntityTypeConfiguration<MonitoringRecord>
{
    public void Configure(EntityTypeBuilder<MonitoringRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.State).HasConversion<string>();
        builder.Property(x => x.Method).HasConversion<string>();
        builder.Property(x => x.Details).HasMaxLength(500);
        builder.HasIndex(x => x.AssetId);
        builder.HasIndex(x => x.CheckedAt);
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.TargetAssetType).HasConversion<string>();

        builder.HasMany(x => x.Records)
            .WithOne(x => x.ImportBatch)
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
