using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using InventoryMapper.Infrastructure.Repositories;
using InventoryMapper.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryMapper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("InventoryMapper.Infrastructure")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IBlueprintService, BlueprintService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IImportService, ImportService>();

        return services;
    }
}
