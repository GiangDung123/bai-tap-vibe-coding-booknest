using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BookNest.Api.Controllers;
[ApiController, Route("api/auth")]
public class AuthController(BookNestDb db, IPasswordHasher<User> hasher, IConfiguration config) : ControllerBase
{
    private object Session(User u)
    {
        var expires = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:Minutes", 120));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], [new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Role, u.Role), new Claim("auth_version", u.AuthVersion.ToString()), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())], expires: expires, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)), SecurityAlgorithms.HmacSha256));
        return new { token = new JwtSecurityTokenHandler().WriteToken(token), expiresAt = expires, user = Views.User(u) };
    }
    [HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        if (dto.Password != dto.ConfirmPassword) throw new AppException(400, "Mật khẩu xác nhận không khớp.");
        var email = dto.Email.Trim(); var normalized = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct)) throw new AppException(409, "Email đã được đăng ký.");
        var user = new User { FullName = dto.FullName.Trim(), Email = email, NormalizedEmail = normalized };
        user.PasswordHash = hasher.HashPassword(user, dto.Password); db.Users.Add(user); await db.SaveChangesAsync(ct);
        return StatusCode(201, Session(user));
    }
    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        var normalized = dto.Email.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
        if (user == null || !user.IsActive || hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed) throw new AppException(401, "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
        return Ok(Session(user));
    }
    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Users.FindAsync([id], ct) ?? throw new AppException(401, "Tài khoản không tồn tại."); return Ok(Views.User(u));
    }
}