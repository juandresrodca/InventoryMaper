using InventoryMapper.Infrastructure;
using InventoryMapper.Infrastructure.Data;
using InventoryMapper.Web.Hubs;
using InventoryMapper.Web.Workers;
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

    builder.Services.AddControllersWithViews();
    builder.Services.AddSignalR();
    // Set blueprint storage path for BlueprintService
    builder.Configuration["BlueprintStoragePath"] = Path.Combine(builder.Environment.WebRootPath, "blueprints");
    builder.Services.AddInfrastructure(builder.Configuration);
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
