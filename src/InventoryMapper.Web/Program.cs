using InventoryMapper.Infrastructure;
using InventoryMapper.Infrastructure.Data;
using InventoryMapper.Infrastructure.Identity;
using InventoryMapper.Web.Hubs;
using InventoryMapper.Web.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/inventorymapper-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(new AuthorizeFilter()));
    builder.Services.AddSignalR();
    // Set blueprint storage path for BlueprintService
    builder.Configuration["BlueprintStoragePath"] = Path.Combine(builder.Environment.WebRootPath, "blueprints");
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddIdentityServices();
    builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
        .AddCookie(IdentityConstants.ApplicationScheme, o =>
        {
            o.LoginPath = "/Account/Login";
            o.AccessDeniedPath = "/Account/AccessDenied";
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
            o.SlidingExpiration = true;
        });
    builder.Services.AddHostedService<MonitoringWorker>();

    builder.Services.AddResponseCompression();
    builder.Services.AddMemoryCache();

    var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbSeeder.SeedAsync(db, logger);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@inventorymapper.local";
        var adminPassword = builder.Configuration["Seed:AdminPassword"] ?? "ChangeMe123!";
        await DbSeeder.SeedIdentityAsync(roleManager, userManager, adminEmail, adminPassword, logger);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseResponseCompression();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<MonitoringHub>("/hubs/monitoring");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");

    app.Run();
}
catch (HostAbortedException)
{
    // EF Core design-time host aborts intentionally after migrations — not a real error
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
