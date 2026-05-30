using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.API.Controllers;

[ApiController]
[Route("api/v1/assets")]
public class AssetsApiController(IAssetService assetService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AssetFilterDto filter, CancellationToken ct)
    {
        var result = await assetService.GetAssetsAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var asset = await assetService.GetAssetByIdAsync(id, ct);
        return asset == null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetDto dto, CancellationToken ct)
    {
        var asset = await assetService.CreateAssetAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetDto dto, CancellationToken ct)
    {
        try
        {
            var asset = await assetService.UpdateAssetAsync(id, dto, ct);
            return Ok(asset);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await assetService.DeleteAssetAsync(id, ct);
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
        => Ok(await assetService.GetDashboardStatsAsync(ct));

    [HttpGet("unplaced")]
    public async Task<IActionResult> Unplaced(CancellationToken ct)
        => Ok(await assetService.GetUnplacedAssetsAsync(ct));

    [HttpPost("{id:guid}/place")]
    public async Task<IActionResult> Place(Guid id, [FromQuery] Guid blueprintId, [FromQuery] double x, [FromQuery] double y, CancellationToken ct)
    {
        await assetService.PlaceAssetOnBlueprintAsync(id, blueprintId, x, y, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unplace")]
    public async Task<IActionResult> Unplace(Guid id, CancellationToken ct)
    {
        await assetService.RemoveAssetFromBlueprintAsync(id, ct);
        return NoContent();
    }
}
