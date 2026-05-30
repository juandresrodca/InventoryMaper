using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.Web.Controllers;

public class ImportController(IImportService importService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var history = await importService.GetImportHistoryAsync();
        return View(history);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? assetType)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file.";
            return RedirectToAction(nameof(Index));
        }

        var allowed = new[] { ".xlsx", ".csv" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "Only .xlsx and .csv files are supported.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var batch = await importService.ImportFromStreamAsync(stream, file.FileName, assetType,
            User.Identity?.Name ?? "Admin");

        TempData["Success"] = $"Import complete: {batch.SuccessRows} added, {batch.DuplicateRows} updated, {batch.ErrorRows} errors.";
        return RedirectToAction(nameof(Report), new { id = batch.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Report(Guid id)
    {
        var batch = await importService.GetImportBatchAsync(id);
        if (batch == null) return NotFound();
        return View(batch);
    }

    [HttpGet]
    public async Task<IActionResult> Template(string assetType = "PhysicalDevice")
    {
        var bytes = await importService.GenerateImportTemplateAsync(assetType);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"import-template-{assetType.ToLower()}.xlsx");
    }
}
