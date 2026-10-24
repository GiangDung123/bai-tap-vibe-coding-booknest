using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.Services;
public class DashboardService(BookNestDb db)
{
    public static (DateTime Start, DateTime End) Range(DateOnly from, DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > 365) throw new AppException(400, "Khoảng ngày phải hợp lệ và không quá 366 ngày.");
        return (DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc), DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc));
    }
    public async Task<object> Get(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); var f = from ?? today.AddDays(-29); var t = to ?? today;
        var (start, end) = Range(f, t);
        var revenues = await db.Orders.Where(o => o.OrderStatus == "Completed" && o.PaymentStatus == "Paid" && o.CompletedAt >= start && o.CompletedAt < end).Select(o => new { o.TotalAmount, o.CompletedAt }).ToListAsync(ct);
        var days = Enumerable.Range(0, t.DayNumber - f.DayNumber + 1).Select(i => f.AddDays(i)).Select(d => new { date = d.ToString("yyyy-MM-dd"), revenue = revenues.Where(o => DateOnly.FromDateTime(o.CompletedAt!.Value.AddHours(7)) == d).Sum(o => o.TotalAmount) }).ToArray();
        var latest = await db.Orders.AsNoTracking().Include(o => o.User).Include(o => o.Items).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync(ct);
        return new { totalRevenue = revenues.Sum(o => o.TotalAmount), totalOrders = await db.Orders.CountAsync(o => o.CreatedAt >= start && o.CreatedAt < end, ct), totalCustomers = await db.Users.CountAsync(u => u.Role == "Customer", ct), activeProducts = await db.Products.CountAsync(p => p.IsActive && p.Category.IsActive, ct), dailyRevenue = days, latestOrders = latest.Select(Views.Order), from = f, to = t };
    }
}