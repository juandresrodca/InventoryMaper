using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InventoryMapper.Web.Controllers;

public class LocationsController(IRepository<Location> locationRepo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await locationRepo.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var location = await locationRepo.Query()
            .Include(l => l.Blueprints.Where(b => !b.IsDeleted)).ThenInclude(b => b.PlacedAssets)
            .Include(l => l.Assets.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == id);
        if (location == null) return NotFound();
        return View(location);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.LocationTypes = Enum.GetValues<LocationType>().Select(t => new SelectListItem(t.ToString(), t.ToString())).ToList();
        return View(new Location());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Location model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.LocationTypes = Enum.GetValues<LocationType>().Select(t => new SelectListItem(t.ToString(), t.ToString())).ToList();
            return View(model);
        }
        await locationRepo.AddAsync(model);
        TempData["Success"] = $"Location '{model.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await locationRepo.DeleteAsync(id);
        TempData["Success"] = "Location deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var locations = await locationRepo.GetAllAsync();
        return Json(locations.Select(l => new { l.Id, l.Name, Type = l.LocationType.ToString() }));
    }
}
