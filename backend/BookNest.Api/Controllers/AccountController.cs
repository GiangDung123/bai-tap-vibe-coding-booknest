using System.Security.Claims;
using System.Text.RegularExpressions;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace BookNest.Api.Controllers;
[ApiController, Authorize, Route("api/account")]
public class AccountController(BookNestDb db, IPasswordHasher<User> hasher) : ControllerBase
{
    private int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct) => Ok(Views.User(await db.Users.AsNoTracking().SingleAsync(u => u.Id == Uid, ct)));
    [HttpPut("profile")]
    public async Task<IActionResult> Update(ProfileDto dto, CancellationToken ct)
    {
        var name = Regex.Replace(dto.FullName.Trim(), @"\s+", " ");
        if (name.Length is < 2 or > 120) throw new AppException(400, "Họ tên cần từ 2 đến 120 ký tự.");
        await db.Users.Where(u => u.Id == Uid).ExecuteUpdateAsync(s => s.SetProperty(u => u.FullName, name).SetProperty(u => u.PhoneNumber, (dto.PhoneNumber ?? "").Trim()).SetProperty(u => u.DefaultShippingAddress, (dto.DefaultShippingAddress ?? "").Trim()).SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);
        return await Profile(ct);
    }
    [HttpPost("change-password"), EnableRateLimiting("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().SingleAsync(u => u.Id == Uid, ct);
        if (hasher.VerifyHashedPassword(u, u.PasswordHash, dto.CurrentPassword) == PasswordVerificationResult.Failed) throw new AppException(400, "Mật khẩu hiện tại không đúng.");
        if (dto.NewPassword != dto.ConfirmPassword) throw new AppException(400, "Mật khẩu xác nhận không khớp.");
        if (hasher.VerifyHashedPassword(u, u.PasswordHash, dto.NewPassword) != PasswordVerificationResult.Failed) throw new AppException(400, "Mật khẩu mới phải khác mật khẩu hiện tại.");
        var hash = hasher.HashPassword(u, dto.NewPassword);
        var changed = await db.Users.Where(x => x.Id == Uid && x.IsActive && x.AuthVersion == u.AuthVersion && x.PasswordHash == u.PasswordHash).ExecuteUpdateAsync(s => s.SetProperty(x => x.PasswordHash, hash).SetProperty(x => x.AuthVersion, x => x.AuthVersion + 1).SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
        if (changed == 0) throw new AppException(409, "Tài khoản vừa thay đổi. Vui lòng đăng nhập lại.");
        return NoContent();
    }
}