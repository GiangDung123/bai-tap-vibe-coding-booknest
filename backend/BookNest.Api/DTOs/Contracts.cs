using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BookNest.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.DTOs;
public record RegisterDto([Required, StringLength(120, MinimumLength = 2)] string FullName, [Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(128, MinimumLength = 8)] string Password, [Required] string ConfirmPassword);
public record LoginDto([Required, EmailAddress] string Email, [Required] string Password);
public record CategoryDto([Required, StringLength(120)] string Name, [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(180)] string Slug, [StringLength(4000)] string Description, bool IsActive = true);
public record ProductWriteDto([Required, StringLength(200)] string Name, [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(180)] string Slug, [Required, StringLength(100)] string SKU, int CategoryId, [Required, StringLength(200)] string Author, [Required, StringLength(200)] string Publisher, [Required, StringLength(4000)] string Description, [Range(typeof(decimal), "0.01", "1000000000")] decimal Price, [Range(0, 1000000)] int StockQuantity, bool IsActive, bool IsFeatured, long Version, [MaxLength(8)] string[] Images);
public record CartLine(int ProductId, [Range(1, 1000)] int Quantity);
public record CheckoutDto([Required, StringLength(120, MinimumLength = 2)] string RecipientName, [Required, StringLength(40, MinimumLength = 8)] string RecipientPhone, [Required, StringLength(1000, MinimumLength = 10)] string ShippingAddress, [StringLength(1000)] string Note, [Required, StringLength(100, MinimumLength = 16)] string IdempotencyKey, [Required, MinLength(1), MaxLength(100)] CartLine[] Items);
public record PaymentDto(bool Success);
public record StatusDto([Required] string Status);
public record ReviewDto([Range(1, 5)] int Rating, [Required, StringLength(1000)] string Comment);
public record ChatDto([Required, StringLength(1000, MinimumLength = 2)] string Message);
public record ProductView(int Id, int CategoryId, string CategoryName, string Name, string Slug, string SKU, string Author, string Publisher, string Description, decimal Price, int StockQuantity, bool IsActive, bool IsFeatured, long Version, DateTime CreatedAt, string[] Images, double? AverageRating, int ReviewCount);
public static class Views
{
    public static readonly Expression<Func<Product, ProductView>> Product = p => new ProductView(p.Id, p.CategoryId, p.Category.Name, p.Name, p.Slug, p.SKU, p.Author, p.Publisher, p.Description, p.Price, p.StockQuantity, p.IsActive && p.Category.IsActive, p.IsFeatured, p.Version, p.CreatedAt, p.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToArray(), p.Reviews.Select(r => (double?)r.Rating).Average(), p.Reviews.Count);
    public static object User(User u) => new { u.Id, u.FullName, u.Email, u.Role, u.PhoneNumber, u.DefaultShippingAddress, u.IsActive, u.CreatedAt, u.UpdatedAt };
    public static object Order(Order o) => new { o.Id, o.OrderCode, o.UserId, BuyerName = o.User?.FullName, o.RecipientName, o.RecipientPhone, o.ShippingAddress, o.Note, o.Subtotal, o.ShippingFee, o.TotalAmount, o.OrderStatus, o.PaymentStatus, o.CreatedAt, o.UpdatedAt, o.PaidAt, o.CompletedAt, o.CancelledAt, Items = o.Items.Select(i => new { i.ProductId, i.ProductNameSnapshot, i.SKUSnapshot, i.UnitPrice, i.Quantity, i.LineTotal }) };
}
public record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public static class Paging
{
    public static async Task<PageResult<T>> PageAsync<T>(this IQueryable<T> q, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var count = await q.CountAsync(ct);
        return new(await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), page, pageSize, count, (int)Math.Ceiling(count / (double)pageSize));
    }
}