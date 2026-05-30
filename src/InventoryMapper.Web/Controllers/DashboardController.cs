using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.Web.Controllers;

public class DashboardController(IAssetService assetService, IMonitoringService monitoring) : Controller
{
    public async Task<IActionResult> Index()
    {
        var stats = await assetService.GetDashboardStatsAsync();
        return View(stats);
    }

    [HttpGet]
    public async Task<IActionResult> StatsJson()
    {
        var stats = await assetService.GetDashboardStatsAsync();
        return Json(stats);
    }

    [HttpGet]
    public async Task<IActionResult> Alerts()
    {
        var alerts = await monitoring.GetActiveAlertsAsync();
        return View(alerts);
    }

    [HttpPost]
    public async Task<IActionResult> ResolveAlert(Guid id)
    {
        await monitoring.ResolveAlertAsync(id, User.Identity?.Name ?? "Admin");
        return RedirectToAction(nameof(Alerts));
    }
}
