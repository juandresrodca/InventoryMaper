using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Enums;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryMapper.Web.Controllers;

public class AssetsController(IAssetService assetService, IRepository<Core.Entities.Location> locationRepo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(AssetFilterDto filter)
    {
        var result = await assetService.GetAssetsAsync(filter);
        var locations = await locationRepo.GetAllAsync();

        ViewBag.Filter = filter;
        ViewBag.Locations = new SelectList(locations, "Id", "Name");
        ViewBag.AssetTypes = Enum.GetValues<AssetType>().Select(t => new SelectListItem(t.ToString(), t.ToString()));
        ViewBag.Statuses = Enum.GetValues<AssetStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString()));
        ViewBag.OnlineStates = Enum.GetValues<OnlineState>().Select(o => new SelectListItem(o.ToString(), o.ToString()));

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var asset = await assetService.GetAssetByIdAsync(id);
        if (asset == null) return NotFound();
        return View(asset);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateViewBag();
        return View(new CreateAssetDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAssetDto model)
    {
        if (!ModelState.IsValid) { await PopulateViewBag(); return View(model); }
        var asset = await assetService.CreateAssetAsync(model);
        TempData["Success"] = $"Asset '{asset.Hostname}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = asset.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var asset = await assetService.GetAssetByIdAsync(id);
        if (asset == null) return NotFound();

        await PopulateViewBag();
        var dto = new UpdateAssetDto
        {
            Hostname = asset.Hostname, IpAddress = asset.IpAddress, MacAddress = asset.MacAddress,
            SerialNumber = asset.SerialNumber, AssetType = asset.AssetType, Manufacturer = asset.Manufacturer,
            Model = asset.Model, OperatingSystem = asset.OperatingSystem, OsVersion = asset.OsVersion,
            OrganizationalUnit = asset.OrganizationalUnit, AssignedUser = asset.AssignedUser,
            Department = asset.Department, Status = asset.Status, Notes = asset.Notes,
            LocationId = asset.LocationId, MonitoringMethod = asset.MonitoringMethod,
            IsMonitored = asset.IsMonitored,
            Tags = asset.Tags.Select(t => new TagDto { Key = t.Key, Value = t.Value, Color = t.Color }).ToList()
        };
        ViewBag.AssetId = id;
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateAssetDto model)
    {
        if (!ModelState.IsValid) { await PopulateViewBag(); ViewBag.AssetId = id; return View(model); }
        await assetService.UpdateAssetAsync(id, model);
        TempData["Success"] = $"Asset '{model.Hostname}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await assetService.DeleteAssetAsync(id);
        TempData["Success"] = "Asset deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Ping(Guid id)
    {
        var result = await assetService.GetAssetByIdAsync(id);
        if (result == null) return NotFound();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var result = await assetService.GetAssetsAsync(new AssetFilterDto { Search = q, PageSize = 20 });
        return Json(result.Items.Select(a => new { a.Id, a.Hostname, a.IpAddress, Type = a.AssetType.ToString(), State = a.OnlineState.ToString() }));
    }

    private async Task PopulateViewBag()
    {
        var locations = await locationRepo.GetAllAsync();
        ViewBag.Locations = new SelectList(locations, "Id", "Name");
        ViewBag.AssetTypes = Enum.GetValues<AssetType>().Select(t => new SelectListItem(t.ToString(), t.ToString())).ToList();
        ViewBag.Statuses = Enum.GetValues<AssetStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString())).ToList();
        ViewBag.MonitoringMethods = Enum.GetValues<MonitoringMethod>().Select(m => new SelectListItem(m.ToString(), m.ToString())).ToList();
    }
}
