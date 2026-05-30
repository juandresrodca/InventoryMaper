using InventoryMapper.Core.DTOs;
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await blueprintService.DeleteBlueprintAsync(id);
        TempData["Success"] = "Blueprint deleted.";
        return RedirectToAction(nameof(Index));
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
