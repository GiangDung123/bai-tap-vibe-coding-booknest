using System.Data;
using System.Security.Claims;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BookNest.Api.Controllers;
[ApiController, Authorize(Roles = "Admin"), Route("api/admin/users")]
public class UsersController(BookNestDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? search, string? role, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(role) && role is not ("Admin" or "Customer")) throw new AppException(400, "Vai trò không hợp lệ.");
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { search = search.Trim(); q = q.Where(u => u.FullName.Contains(search) || u.Email.Contains(search)); }
        if (!string.IsNullOrEmpty(role)) q = q.Where(u => u.Role == role);
        if (isActive.HasValue) q = q.Where(u => u.IsActive == isActive);
        return Ok(await q.OrderByDescending(u => u.Id).Select(u => new { u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role, u.IsActive, u.CreatedAt }).PageAsync(page, pageSize, ct));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct) ?? throw new AppException(404, "Không tìm thấy người dùng.");
        var orders = db.Orders.AsNoTracking().Where(o => o.UserId == id);
        return Ok(new { user = Views.User(u), totalOrders = await orders.CountAsync(ct), totalSpent = await orders.Where(o => o.OrderStatus == "Completed" && o.PaymentStatus == "Paid").SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0, recentOrders = await orders.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).Take(5).Select(o => new { o.Id, o.OrderCode, o.TotalAmount, o.OrderStatus, o.PaymentStatus, o.CreatedAt }).ToListAsync(ct) });
    }
    [HttpPatch("{id:int}/status")]
    public Task<IActionResult> Status(int id, UserStatusDto dto, CancellationToken ct) => Change(id, dto.IsActive, null, ct);
    [HttpPatch("{id:int}/role")]
    public Task<IActionResult> Role(int id, UserRoleDto dto, CancellationToken ct) => Change(id, null, dto.Role, ct);
    private async Task<IActionResult> Change(int id, bool? active, string? role, CancellationToken ct)
    {
        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == actorId) throw new AppException(400, "Bạn không thể tự khóa hoặc tự đổi vai trò.");
        // One persistent row serializes all role/status changes across processes, not just this server.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.SecurityGuards.FromSqlRaw("SELECT * FROM `SecurityGuards` WHERE `Id` = 1 FOR UPDATE").ToListAsync(ct);
        var actor = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actorId, ct);
        if (!actor.IsActive || actor.Role != "Admin" || actor.AuthVersion.ToString() != User.FindFirstValue("auth_version")) throw new AppException(401, "Vui lòng đăng nhập lại.");
        var u = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct) ?? throw new AppException(404, "Không tìm thấy người dùng.");
        var newActive = active ?? u.IsActive; var newRole = role ?? u.Role;
        if (u.IsActive && u.Role == "Admin" && (!newActive || newRole != "Admin") && await db.Users.CountAsync(x => x.IsActive && x.Role == "Admin", ct) <= 1) throw new AppException(409, "Không thể khóa hoặc hạ quyền Admin hoạt động cuối cùng.");
        var oldValue = active.HasValue ? u.IsActive.ToString() : u.Role;
        var newValue = active.HasValue ? newActive.ToString() : newRole;
        if (oldValue != newValue)
        {
            u.IsActive = newActive; u.Role = newRole; u.UpdatedAt = DateTime.UtcNow;
            // Atomic increment must not overwrite a concurrent password change.
            await db.Users.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.AuthVersion, x => x.AuthVersion + 1), ct);
            db.UserAudits.Add(new UserAudit { ActorId = actorId, TargetId = id, Action = active.HasValue ? "Status" : "Role", OldValue = oldValue, NewValue = newValue });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return Ok(Views.User(u));
    }
}