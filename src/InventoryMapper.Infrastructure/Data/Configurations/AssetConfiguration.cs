using InventoryMapper.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryMapper.Infrastructure.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hostname).HasMaxLength(255).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.MacAddress).HasMaxLength(17);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.Manufacturer).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.OperatingSystem).HasMaxLength(100);
        builder.Property(x => x.OsVersion).HasMaxLength(50);
        builder.Property(x => x.OrganizationalUnit).HasMaxLength(500);
        builder.Property(x => x.AssignedUser).HasMaxLength(200);
        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.AssetType).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.OnlineState).HasConversion<string>();
        builder.Property(x => x.MonitoringMethod).HasConversion<string>();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.Hostname);
        builder.HasIndex(x => x.IpAddress);
        builder.HasIndex(x => x.SerialNumber);
        builder.HasIndex(x => x.AssetType);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OnlineState);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Assets)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Blueprint)
            .WithMany(x => x.PlacedAssets)
            .HasForeignKey(x => x.BlueprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Agent)
            .WithOne(x => x.Asset)
            .HasForeignKey<Asset>(x => x.AgentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Tags)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MonitoringHistory)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Alerts)
            .WithOne(x => x.Asset)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
