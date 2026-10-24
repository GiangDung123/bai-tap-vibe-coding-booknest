using System.Security.Claims;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.Controllers;
[ApiController, Authorize, Route("api/orders")]
public class OrdersController(BookNestDb db, OrderService service) : ControllerBase
{
    private int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost]
    public async Task<IActionResult> Create(CheckoutDto dto, CancellationToken ct) { var o = await service.Create(Uid, dto, ct); return CreatedAtAction(nameof(Get), new { id = o.Id }, Views.Order(o)); }
    [HttpGet("my")]
    public async Task<IActionResult> My(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var data = await db.Orders.AsNoTracking().Where(o => o.UserId == Uid).OrderByDescending(o => o.CreatedAt).Include(o => o.Items).PageAsync(page, pageSize, ct);
        return Ok(new PageResult<object>(data.Items.Select(Views.Order).ToList(), data.Page, data.PageSize, data.TotalItems, data.TotalPages));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(Views.Order(await service.Get(id, Uid, false, ct)));
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct) => Ok(Views.Order(await service.Change(id, Uid, false, "Cancelled", ct)));
    [HttpPost("{id:int}/simulate-payment")]
    public async Task<IActionResult> Pay(int id, PaymentDto dto, CancellationToken ct) => Ok(Views.Order(await service.Change(id, Uid, false, dto.Success ? "Paid" : "Failed", ct)));
}