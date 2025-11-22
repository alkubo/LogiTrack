using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace LogiTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;
    public InventoryController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: /api/inventory
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAll(CancellationToken ct)
    {
        const string cacheKey = "inventory_all";
        if (_cache.TryGetValue(cacheKey, out List<InventoryItem>? cached))
        {
            Response.Headers["X-Cache-Hit"] = "true";
            return Ok(cached);
        }

        var sw = Stopwatch.StartNew();
        var items = await _context.InventoryItems.AsNoTracking().ToListAsync(ct);
        sw.Stop();
        Response.Headers["X-Query-MS"] = sw.ElapsedMilliseconds.ToString();

        _cache.Set(cacheKey, items, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            Size = items.Count // if size limit configured
        });
        Response.Headers["X-Cache-Hit"] = "false";
        return Ok(items);
    }

    public record InventoryItemCreateDto(string Name, int Quantity, string Location);

    // POST: /api/inventory
    [HttpPost]
    public async Task<ActionResult<InventoryItem>> Create([FromBody] InventoryItemCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return ValidationProblem("Name is required.");
        }
        var entity = new InventoryItem
        {
            Name = dto.Name.Trim(),
            Quantity = dto.Quantity,
            Location = dto.Location.Trim()
        };
        _context.InventoryItems.Add(entity);
        await _context.SaveChangesAsync(ct);
        _cache.Remove("inventory_all"); // invalidate cache
        return CreatedAtAction(nameof(GetAll), new { id = entity.ItemId }, entity); // simple response
    }

    // DELETE: /api/inventory/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _context.InventoryItems.FindAsync([id], ct);
        if (entity == null)
        {
            return NotFound(Problem(detail: $"Inventory item {id} not found", statusCode: 404));
        }
        _context.InventoryItems.Remove(entity);
        await _context.SaveChangesAsync(ct);
        _cache.Remove("inventory_all"); // invalidate cache
        return NoContent();
    }
}
