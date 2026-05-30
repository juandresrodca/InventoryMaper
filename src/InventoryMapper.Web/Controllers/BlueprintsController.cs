using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventoryMapper.Web.Controllers;

public class BlueprintsController(IBlueprintService blueprintService, IRepository<Core.Entities.Location> locationRepo, IAssetService assetService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var blueprints = await blueprintService.GetAllBlueprintsAsync();
        return View(blueprints);
    }

    [HttpGet]
    public async Task<IActionResult> Canvas(Guid id)
    {
        var blueprint = await blueprintService.GetBlueprintByIdAsync(id);
        if (blueprint == null) return NotFound();

        var unplaced = await assetService.GetUnplacedAssetsAsync();
        ViewBag.UnplacedAssets = unplaced;
        return View(blueprint);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var locations = await locationRepo.GetAllAsync();
        ViewBag.Locations = new SelectList(locations, "Id", "Name");
        return View(new CreateBlueprintDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBlueprintDto model, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "A blueprint file is required.");
            var locs = await locationRepo.GetAllAsync();
            ViewBag.Locations = new SelectList(locs, "Id", "Name");
            return View(model);
        }

        await using var stream = file.OpenReadStream();
        var blueprint = await blueprintService.CreateBlueprintAsync(model, stream, file.FileName, file.ContentType);
        TempData["Success"] = $"Blueprint '{blueprint.Name}' uploaded successfully.";
        return RedirectToAction(nameof(Canvas), new { id = blueprint.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var bp = await blueprintService.GetBlueprintByIdAsync(id);
        if (bp == null) return NotFound();
        var locations = await locationRepo.GetAllAsync();
        ViewBag.Locations = new SelectList(locations, "Id", "Name", bp.LocationId);
        ViewBag.BlueprintId = id;
        ViewBag.BlueprintName = bp.Name;
        return View(new UpdateBlueprintDto
        {
            Name = bp.Name,
            Description = bp.Description,
            LocationId = bp.LocationId,
            CanvasWidth = bp.CanvasWidth,
            CanvasHeight = bp.CanvasHeight
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "")] UpdateBlueprintDto dto)
    {
        if (!ModelState.IsValid)
        {
            var locs = await locationRepo.GetAllAsync();
            ViewBag.Locations = new SelectList(locs, "Id", "Name", dto.LocationId);
            ViewBag.BlueprintId = id;
            ViewBag.BlueprintName = dto.Name;
            return View(dto);
        }
        await blueprintService.UpdateBlueprintAsync(id, dto);
        TempData["Success"] = $"Blueprint '{dto.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await blueprintService.DeleteBlueprintAsync(id);
        TempData["Success"] = "Blueprint deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RenameZone(Guid id, [FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name required.");
        await blueprintService.RenameZoneAsync(id, name);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> MigratePlan(Guid from, string assets)
    {
        var sourceBp = await blueprintService.GetBlueprintByIdAsync(from);
        if (sourceBp == null) return NotFound();

        var assetIds = assets.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue).Select(g => g!.Value).ToArray();

        var assetsToMove = new List<Asset>();
        foreach (var id in assetIds)
        {
            var asset = await assetService.GetAssetByIdAsync(id);
            if (asset != null) assetsToMove.Add(asset);
        }

        var allBlueprints = await blueprintService.GetAllBlueprintsAsync();
        ViewBag.SourceBlueprint = sourceBp;
        ViewBag.AssetsToMove = assetsToMove;
        ViewBag.Destinations = allBlueprints.Where(b => b.Id != from).ToList();
        ViewBag.AssetIds = assets;
        ViewBag.FromId = from;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteMigration(Guid from, Guid to, string assets)
    {
        var assetIds = assets.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue).Select(g => g!.Value).ToArray();

        await assetService.MigrateAssetsAsync(assetIds, to);

        var sourceBp = await blueprintService.GetBlueprintByIdAsync(from);
        var destBp   = await blueprintService.GetBlueprintByIdAsync(to);
        TempData["Success"] = $"Moved {assetIds.Length} asset(s) from '{sourceBp?.Name}' to '{destBp?.Name}'.";
        return RedirectToAction(nameof(Canvas), new { id = to });
    }

    // API-style endpoints used by the canvas JS engine
    [HttpGet]
    public async Task<IActionResult> Layout(Guid id)
    {
        var layout = await blueprintService.GetBlueprintLayoutAsync(id);
        return Json(layout);
    }

    [HttpPost]
    public async Task<IActionResult> SaveLayout([FromBody] BlueprintLayoutDto layout)
    {
        await blueprintService.SaveBlueprintLayoutAsync(layout.BlueprintId, layout);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> PlaceAsset(Guid blueprintId, Guid assetId, double x, double y)
    {
        await assetService.PlaceAssetOnBlueprintAsync(assetId, blueprintId, x, y);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAsset(Guid assetId)
    {
        await assetService.RemoveAssetFromBlueprintAsync(assetId);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> AddAnnotation(Guid id, [FromBody] CreateAnnotationDto dto)
    {
        var annotation = await blueprintService.AddAnnotationAsync(id, dto);
        return Json(annotation);
    }

    [HttpPost]
    public async Task<IActionResult> AddZone(Guid id, [FromBody] CreateZoneDto dto)
    {
        var zone = await blueprintService.AddZoneAsync(id, dto);
        return Json(zone);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAnnotation(Guid id)
    {
        await blueprintService.DeleteAnnotationAsync(id);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteZone(Guid id)
    {
        await blueprintService.DeleteZoneAsync(id);
        return Ok();
    }
}
