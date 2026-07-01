using InventoryMapper.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryMapper.Infrastructure.Data.Configurations;

public class EnrollmentTokenConfiguration : IEntityTypeConfiguration<EnrollmentToken>
{
    public void Configure(EntityTypeBuilder<EnrollmentToken> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConsumedByAgentKey).HasMaxLength(200);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
