using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BookNest.Api.Services;
public class OrderService(BookNestDb db)
{
    public static CartLine[] Normalize(CartLine[] lines)
    {
        if (lines.Length is 0 or > 100 || lines.Any(i => i.ProductId <= 0 || i.Quantity is <= 0 or > 1000)) throw new AppException(400, "Giỏ hàng hoặc số lượng không hợp lệ.");
        var result = lines.GroupBy(i => i.ProductId).Select(g => new CartLine(g.Key, g.Sum(i => i.Quantity))).OrderBy(i => i.ProductId).ToArray();
        if (result.Any(i => i.Quantity > 1000)) throw new AppException(400, "Số lượng tối đa mỗi sách là 1000.");
        return result;
    }
    public static string Fingerprint(CheckoutDto dto, CartLine[] items) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Name = dto.RecipientName.Trim(), Phone = dto.RecipientPhone.Trim(), Address = dto.ShippingAddress.Trim(), Note = dto.Note.Trim(), Items = items }))));
    private static Order Match(Order existing, string hash)
    {
        if (existing.RequestHash != hash) throw new AppException(409, "Mã yêu cầu đã dùng cho nội dung khác. Hãy bắt đầu lần đặt hàng mới.");
        return existing;
    }
    public async Task<Order> Create(int userId, CheckoutDto dto, CancellationToken ct)
    {
        var lines = Normalize(dto.Items); var hash = Fingerprint(dto, lines);
        var existing = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == dto.IdempotencyKey, ct);
        if (existing != null) return Match(existing, hash);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = new Order { UserId = userId, OrderCode = "BN-" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(), RecipientName = dto.RecipientName.Trim(), RecipientPhone = dto.RecipientPhone.Trim(), ShippingAddress = dto.ShippingAddress.Trim(), Note = dto.Note.Trim(), IdempotencyKey = dto.IdempotencyKey, RequestHash = hash };
            db.Orders.Add(order);
            // Claim unique key before stock changes: a concurrent retry waits on this insert.
            await db.SaveChangesAsync(ct);
            foreach (var line in lines)
            {
                var candidate = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == line.ProductId, ct) ?? throw new AppException(409, "Sản phẩm không còn tồn tại.");
                // Shared category lock serializes hiding a category against checkout.
                var category = await db.Categories.FromSqlInterpolated($"SELECT * FROM Categories WITH (HOLDLOCK, ROWLOCK) WHERE Id={candidate.CategoryId}").AsNoTracking().ToListAsync(ct);
                if (category.Count == 0 || !category[0].IsActive) throw new AppException(409, "Danh mục đã ngừng bán.");
                var product = (await db.Products.FromSqlInterpolated($"SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id={line.ProductId}").AsNoTracking().ToListAsync(ct)).Single();
                if (!product.IsActive || product.CategoryId != candidate.CategoryId || product.StockQuantity < line.Quantity) throw new AppException(409, $"Sách {product.Name} ngừng bán hoặc không đủ tồn kho. Hãy tải lại giỏ.");
                var changed = await db.Products.Where(p => p.Id == product.Id && p.Version == product.Version && p.StockQuantity >= line.Quantity && p.IsActive).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - line.Quantity).SetProperty(p => p.Version, p => p.Version + 1).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
                if (changed != 1) throw new AppException(409, "Tồn kho thay đổi, vui lòng thử lại.");
                order.Items.Add(new OrderItem { ProductId = product.Id, ProductNameSnapshot = product.Name, SKUSnapshot = product.SKU, UnitPrice = product.Price, Quantity = line.Quantity, LineTotal = product.Price * line.Quantity });
            }
            order.Subtotal = order.Items.Sum(i => i.LineTotal); order.TotalAmount = order.Subtotal;
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return order;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
            existing = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == dto.IdempotencyKey, ct);
            return existing != null ? Match(existing, hash) : throw new AppException(409, "Mã đơn bị trùng, vui lòng thử lại.");
        }
    }
    public async Task<Order> Get(int id, int userId, bool admin, CancellationToken ct)
    {
        var o = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id && (admin || x.UserId == userId), ct);
        return o ?? throw new AppException(404, "Không tìm thấy đơn hàng.");
    }
    public async Task<Order> Change(int id, int userId, bool admin, string action, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var o = (await db.Orders.FromSqlInterpolated($"SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK) WHERE Id={id}").ToListAsync(ct)).SingleOrDefault();
        if (o == null || (!admin && o.UserId != userId)) throw new AppException(404, "Không tìm thấy đơn hàng.");
        await db.Entry(o).Collection(x => x.Items).LoadAsync(ct);
        if (action is "Paid" or "Failed")
        {
            if (o.OrderStatus != "Pending" || o.PaymentStatus == "Paid") throw new AppException(409, "Đơn không thể thanh toán hoặc đã thanh toán.");
            o.PaymentStatus = action; if (action == "Paid") o.PaidAt = DateTime.UtcNow;
        }
        else if (action == "Cancelled")
        {
            if (o.OrderStatus != "Pending" || o.PaymentStatus == "Paid") throw new AppException(409, "Chỉ hủy đơn đang xử lý và chưa thanh toán.");
            o.OrderStatus = "Cancelled"; o.CancelledAt = DateTime.UtcNow;
            foreach (var item in o.Items.OrderBy(x => x.ProductId))
                await db.Products.Where(p => p.Id == item.ProductId).ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + item.Quantity).SetProperty(p => p.Version, p => p.Version + 1).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
        }
        else if (admin && action == "Shipping" && o.OrderStatus == "Pending" && o.PaymentStatus == "Paid") o.OrderStatus = "Shipping";
        else if (admin && action == "Completed" && o.OrderStatus == "Shipping" && o.PaymentStatus == "Paid") { o.OrderStatus = "Completed"; o.CompletedAt = DateTime.UtcNow; }
        else throw new AppException(409, "Chuyển trạng thái không hợp lệ.");
        o.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return o;
    }
}