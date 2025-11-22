using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace LogiTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;
    public OrderController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: /api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var orders = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .Select(o => new { o.OrderId, o.CustomerName, o.DatePlaced, ItemCount = o.Items.Count })
            .ToListAsync(ct);
        sw.Stop();
        Response.Headers["X-Query-MS"] = sw.ElapsedMilliseconds.ToString();
        return Ok(orders);
    }

    // GET: /api/orders/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetById(int id, CancellationToken ct)
    {
        var cacheKey = $"order_{id}";
        if (_cache.TryGetValue(cacheKey, out Order? cached))
        {
            Response.Headers["X-Cache-Hit"] = "true";
            return Ok(cached);
        }
        var sw = Stopwatch.StartNew();
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == id, ct);
        sw.Stop();
        Response.Headers["X-Query-MS"] = sw.ElapsedMilliseconds.ToString();
        if (order == null)
        {
            return NotFound(Problem(detail: $"Order {id} not found", statusCode: 404));
        }
        _cache.Set(cacheKey, order, TimeSpan.FromSeconds(30));
        Response.Headers["X-Cache-Hit"] = "false";
        return Ok(order);
    }

    public record OrderCreateDto(string CustomerName, DateTime? DatePlaced, List<OrderItemCreateDto> Items);
    public record OrderItemCreateDto(string Name, int Quantity, string Location);

    // POST: /api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] OrderCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return ValidationProblem("CustomerName is required.");
        }
        var order = new Order
        {
            CustomerName = dto.CustomerName.Trim(),
            DatePlaced = dto.DatePlaced ?? DateTime.UtcNow
        };
        if (dto.Items != null)
        {
            foreach (var i in dto.Items)
            {
                order.AddItem(new InventoryItem
                {
                    Name = i.Name.Trim(),
                    Quantity = i.Quantity,
                    Location = i.Location.Trim()
                });
            }
        }
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        _cache.Remove($"order_{order.OrderId}");
        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    // DELETE: /api/orders/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == id, ct);
        if (order == null)
        {
            return NotFound(Problem(detail: $"Order {id} not found", statusCode: 404));
        }
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync(ct);
        _cache.Remove($"order_{id}");
        return NoContent();
    }
}
