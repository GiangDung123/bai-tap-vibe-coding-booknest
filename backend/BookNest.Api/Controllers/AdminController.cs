using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using BookNest.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace BookNest.Api.Controllers;
[ApiController, Authorize(Roles = "Admin"), Route("api/admin")]
public class AdminController(BookNestDb db, OrderService orders, DashboardService dashboard, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(string? search, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var q = db.Categories.AsNoTracking(); if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Name.Contains(search));
        return Ok(await q.OrderBy(c => c.Id).Select(c => new { c.Id, c.Name, c.Slug, c.Description, c.IsActive }).PageAsync(page, pageSize, ct));
    }
    [HttpPost("categories")]
    public async Task<IActionResult> AddCategory(CategoryDto dto, CancellationToken ct)
    {
        var c = new Category { Name = dto.Name.Trim(), Slug = dto.Slug, Description = dto.Description, IsActive = dto.IsActive }; db.Categories.Add(c); await db.SaveChangesAsync(ct); return StatusCode(201, new { c.Id });
    }
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> EditCategory(int id, CategoryDto dto, CancellationToken ct)
    {
        var c = await db.Categories.FindAsync([id], ct) ?? throw new AppException(404, "Không tìm thấy danh mục."); c.Name = dto.Name.Trim(); c.Slug = dto.Slug; c.Description = dto.Description; c.IsActive = dto.IsActive; c.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> HideCategory(int id, CancellationToken ct)
    {
        var c = await db.Categories.FindAsync([id], ct) ?? throw new AppException(404, "Không tìm thấy danh mục."); c.IsActive = false; c.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("products")]
    public async Task<IActionResult> Products(string? search, int? categoryId, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var q = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.Contains(search) || p.SKU.Contains(search) || p.Author.Contains(search));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId); if (isActive.HasValue) q = q.Where(p => p.IsActive == isActive);
        return Ok(await q.OrderByDescending(p => p.Id).Select(Views.Product).PageAsync(page, pageSize, ct));
    }
    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> Product(int id, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking().Where(p => p.Id == id).Select(Views.Product).SingleOrDefaultAsync(ct); return p == null ? NotFound() : Ok(p);
    }
    private async Task Apply(Product p, ProductWriteDto dto, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct)) throw new AppException(400, "Danh mục không hợp lệ.");
        if (dto.Images == null || dto.Images.Any(i => !System.Text.RegularExpressions.Regex.IsMatch(i, "^/(uploads/[a-f0-9]{32}\\.(jpg|png|webp)|images/cover-[1-5]\\.png)$"))) throw new AppException(400, "Ảnh phải là tệp đã upload trên server.");
        p.Name = dto.Name.Trim(); p.Slug = dto.Slug; p.SKU = dto.SKU.Trim(); p.CategoryId = dto.CategoryId; p.Author = dto.Author.Trim(); p.Publisher = dto.Publisher.Trim(); p.Description = dto.Description.Trim(); p.Price = dto.Price; p.StockQuantity = dto.StockQuantity; p.IsActive = dto.IsActive; p.IsFeatured = dto.IsFeatured; p.UpdatedAt = DateTime.UtcNow;
        db.ProductImages.RemoveRange(p.Images); p.Images = dto.Images.Select((url, i) => new ProductImage { ImageUrl = url, SortOrder = i }).ToList();
    }
    [HttpPost("products")]
    public async Task<IActionResult> AddProduct(ProductWriteDto dto, CancellationToken ct)
    {
        var p = new Product(); await Apply(p, dto, ct); db.Products.Add(p); await db.SaveChangesAsync(ct); return StatusCode(201, new { p.Id });
    }
    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> EditProduct(int id, ProductWriteDto dto, CancellationToken ct)
    {
        var p = await db.Products.Include(p => p.Images).SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new AppException(404, "Không tìm thấy sách.");
        if (p.Version != dto.Version) throw new AppException(409, "Tồn kho hoặc sản phẩm đã thay đổi. Tải lại form trước khi lưu.");
        await Apply(p, dto, ct); p.Version++; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> HideProduct(int id, CancellationToken ct)
    {
        var n = await db.Products.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false).SetProperty(p => p.Version, p => p.Version + 1).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct); return n == 0 ? NotFound() : NoContent();
    }
    [HttpPost("uploads/images"), RequestSizeLimit(5 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 5 * 1024 * 1024) throw new AppException(400, "Ảnh tối đa 5 MB.");
        using var memory = new MemoryStream(); await file.CopyToAsync(memory, ct);
        using var encoded = SKData.CreateCopy(memory.ToArray());
        using var codec = SKCodec.Create(encoded) ?? throw new AppException(400, "Nội dung không phải ảnh hợp lệ.");
        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp)) throw new AppException(400, "Chỉ nhận nội dung JPEG, PNG, WebP.");
        if ((long)codec.Info.Width * codec.Info.Height > 20000000 || codec.Info.Width <= 0 || codec.Info.Height <= 0) throw new AppException(400, "Ảnh tối đa 20 triệu pixel.");
        using var bitmap = new SKBitmap(codec.Info);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new AppException(400, "Ảnh bị hỏng.");
        using var image = SKImage.FromBitmap(bitmap); using var clean = image.Encode(SKEncodedImageFormat.Png, 100);
        var dir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads"); Directory.CreateDirectory(dir); var name = Guid.NewGuid().ToString("N") + ".png";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, name), clean.ToArray(), ct);
        return Ok(new { imageUrl = "/uploads/" + name });
    }
    [HttpGet("orders")]
    public async Task<IActionResult> Orders(string? search, string? orderStatus, string? paymentStatus, DateOnly? from, DateOnly? to, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var q = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(o => o.OrderCode.Contains(search) || o.User.FullName.Contains(search) || o.User.Email.Contains(search));
        if (!string.IsNullOrEmpty(orderStatus)) q = q.Where(o => o.OrderStatus == orderStatus); if (!string.IsNullOrEmpty(paymentStatus)) q = q.Where(o => o.PaymentStatus == paymentStatus);
        if (from.HasValue || to.HasValue) { var (s, e) = DashboardService.Range(from ?? to!.Value.AddDays(-365), to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))); q = q.Where(o => o.CreatedAt >= s && o.CreatedAt < e); }
        var result = await q.OrderByDescending(o => o.CreatedAt).Include(o => o.User).Include(o => o.Items).PageAsync(page, pageSize, ct);
        return Ok(new PageResult<object>(result.Items.Select(Views.Order).ToList(), result.Page, result.PageSize, result.TotalItems, result.TotalPages));
    }
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Order(int id, CancellationToken ct) => Ok(Views.Order(await orders.Get(id, 0, true, ct)));
    [HttpPatch("orders/{id:int}/status")]
    public async Task<IActionResult> Status(int id, StatusDto dto, CancellationToken ct)
    {
        if (dto.Status is not ("Shipping" or "Completed" or "Cancelled")) throw new AppException(400, "Admin chỉ được chuyển sang Đang giao, Hoàn thành hoặc Đã hủy theo quy tắc.");
        return Ok(Views.Order(await orders.Change(id, 0, true, dto.Status, ct)));
    }
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(DateOnly? from, DateOnly? to, CancellationToken ct) => Ok(await dashboard.Get(from, to, ct));
}