using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using InventoryMapper.Infrastructure.Identity;
using InventoryMapper.Infrastructure.Repositories;
using InventoryMapper.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
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

    /// <summary>
    /// Registers the Identity core (UserManager/RoleManager/SignInManager) backed by
    /// ApplicationDbContext, without picking a login scheme. Callers (Web = cookie, API = JWT)
    /// wire up their own AddAuthentication(...) scheme on top of this.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
