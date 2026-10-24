using System.Security.Claims;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.Controllers;
[ApiController, Route("api")]
public class CatalogController(BookNestDb db) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) => Ok(await db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Id).Select(c => new { c.Id, c.Name, c.Slug, c.Description, c.IsActive }).ToListAsync(ct));
    [HttpGet("products")]
    public async Task<IActionResult> Products(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string sort = "newest", int page = 1, int pageSize = 12, bool? featured = null, CancellationToken ct = default)
    {
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice) throw new AppException(400, "Khoảng giá không hợp lệ.");
        var q = db.Products.AsNoTracking().Where(p => p.IsActive && p.Category.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) { search = search.Trim(); q = q.Where(p => p.Name.Contains(search) || p.Author.Contains(search) || p.Description.Contains(search)); }
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        if (featured.HasValue) q = q.Where(p => p.IsFeatured == featured);
        q = sort switch { "priceAsc" => q.OrderBy(p => p.Price).ThenBy(p => p.Id), "priceDesc" => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id), _ => q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id) };
        return Ok(await q.Select(Views.Product).PageAsync(page, pageSize, ct));
    }
    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> Product(int id, CancellationToken ct)
    {
        // Hidden products remain readable for cart reconciliation but cannot be purchased.
        var product = await db.Products.AsNoTracking().Where(p => p.Id == id).Select(Views.Product).SingleOrDefaultAsync(ct);
        return product == null ? NotFound() : Ok(product);
    }
    [HttpGet("products/{id:int}/reviews")]
    public async Task<IActionResult> Reviews(int id, int page = 1, int pageSize = 10, CancellationToken ct = default) => Ok(await db.Reviews.AsNoTracking().Where(r => r.ProductId == id).OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.UserId, r.User.FullName, r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt }).PageAsync(page, pageSize, ct));
    [Authorize(Roles = "Customer"), HttpPost("products/{id:int}/reviews")]
    public Task<IActionResult> Review(int id, ReviewDto dto, CancellationToken ct) => SaveReview(id, dto, false, ct);
    [Authorize(Roles = "Customer"), HttpPut("products/{id:int}/reviews/me")]
    public Task<IActionResult> EditReview(int id, ReviewDto dto, CancellationToken ct) => SaveReview(id, dto, true, ct);
    private async Task<IActionResult> SaveReview(int id, ReviewDto dto, bool edit, CancellationToken ct)
    {
        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await db.Orders.AnyAsync(o => o.UserId == uid && o.OrderStatus == "Completed" && o.Items.Any(i => i.ProductId == id), ct)) throw new AppException(403, "Bạn cần hoàn thành đơn mua sách này trước khi đánh giá.");
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.UserId == uid && r.ProductId == id, ct);
        if (!edit && r != null) throw new AppException(409, "Bạn đã đánh giá. Hãy dùng chức năng sửa.");
        if (edit && r == null) throw new AppException(404, "Chưa có đánh giá để sửa.");
        if (r == null) { r = new Review { UserId = uid, ProductId = id }; db.Reviews.Add(r); }
        r.Rating = dto.Rating; r.Comment = dto.Comment.Trim(); r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct);
        return Ok(new { r.Id, r.Rating, r.Comment, r.UpdatedAt });
    }
}