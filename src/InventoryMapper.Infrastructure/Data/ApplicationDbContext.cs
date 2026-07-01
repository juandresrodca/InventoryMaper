using InventoryMapper.Core.Entities;
using InventoryMapper.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryMapper.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Blueprint> Blueprints => Set<Blueprint>();
    public DbSet<BlueprintAnnotation> BlueprintAnnotations => Set<BlueprintAnnotation>();
    public DbSet<BlueprintZone> BlueprintZones => Set<BlueprintZone>();
    public DbSet<AssetTag> AssetTags => Set<AssetTag>();
    public DbSet<MonitoringRecord> MonitoringRecords => Set<MonitoringRecord>();
    public DbSet<AgentRegistration> AgentRegistrations => Set<AgentRegistration>();
    public DbSet<AlertNotification> Alerts => Set<AlertNotification>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EnrollmentToken> EnrollmentTokens => Set<EnrollmentToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Core.Entities.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
