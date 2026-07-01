using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.API.Controllers;

[ApiController]
[Route("api/v1/blueprints")]
[Authorize]
public class BlueprintsApiController(IBlueprintService blueprintService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await blueprintService.GetAllBlueprintsAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var bp = await blueprintService.GetBlueprintByIdAsync(id, ct);
        return bp == null ? NotFound() : Ok(bp);
    }

    [HttpGet("{id:guid}/layout")]
    public async Task<IActionResult> GetLayout(Guid id, CancellationToken ct)
        => Ok(await blueprintService.GetBlueprintLayoutAsync(id, ct));

    [HttpPost("{id:guid}/layout")]
    public async Task<IActionResult> SaveLayout(Guid id, [FromBody] BlueprintLayoutDto layout, CancellationToken ct)
    {
        await blueprintService.SaveBlueprintLayoutAsync(id, layout, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await blueprintService.DeleteBlueprintAsync(id, ct);
        return NoContent();
    }
}
