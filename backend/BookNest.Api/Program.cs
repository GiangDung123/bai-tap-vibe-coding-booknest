using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using BookNest.Api.Data;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using BookNest.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>();
var cs = builder.Configuration.GetConnectionString("BookNest") ?? throw new InvalidOperationException("Cấu hình ConnectionStrings:BookNest bằng user-secrets hoặc appsettings.Local.json.");
var secret = builder.Configuration["Jwt:Secret"] ?? "";
if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Jwt:Secret cần ít nhất 32 byte, cấu hình ngoài mã nguồn.");
builder.Services.AddDbContext<BookNestDb>(o => o.UseMySql(cs, new MariaDbServerVersion(new Version(10, 4, 32))));
builder.Services.AddControllers();
// Never enable HTTP body logging or EF sensitive-data logging: auth/profile requests contain secrets/PII.
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddHttpClient<ChatService>(c => c.Timeout = TimeSpan.FromSeconds(25));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), ClockSkew = TimeSpan.FromSeconds(15) };
    o.Events = new JwtBearerEvents {
        OnTokenValidated = async c => {
            if (!int.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || !long.TryParse(c.Principal?.FindFirstValue("auth_version"), out var version)) { c.Fail("Session revoked"); return; }
            var db = c.HttpContext.RequestServices.GetRequiredService<BookNestDb>();
            var u = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.IsActive, u.AuthVersion, u.Role }).SingleOrDefaultAsync(c.HttpContext.RequestAborted);
            if (u == null || !u.IsActive || u.AuthVersion != version || u.Role != c.Principal!.FindFirstValue(ClaimTypes.Role)) c.Fail("Session revoked");
        },
        OnChallenge = async c => { c.HandleResponse(); await Results.Problem(statusCode: 401, title: "Vui lòng đăng nhập lại.").ExecuteAsync(c.HttpContext); }, OnForbidden = c => Results.Problem(statusCode: 403, title: "Bạn không có quyền truy cập.").ExecuteAsync(c.HttpContext) };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.OnRejected = async (c, ct) => await Results.Problem(statusCode: 429, title: "Quá nhiều yêu cầu. Vui lòng thử lại sau một phút.").ExecuteAsync(c.HttpContext);
    o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("password", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("chat", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "BookNest API", Version = "v1", Description = "Đặt hàng dùng idempotencyKey; thanh toán chỉ giả lập; endpoint Admin cần JWT role Admin." }); o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }); o.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [] }); });
var app = builder.Build();
app.UseMiddleware<ErrorMiddleware>();
app.UseCors();
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = c => c.Context.Response.Headers["X-Content-Type-Options"] = "nosniff" });
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
if (args.Contains("--migrate") || args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>();
    await db.Database.MigrateAsync();
    if (args.Contains("--seed"))
    {
        if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Seed chỉ được chạy trong Development.");
        await Seed.Run(scope.ServiceProvider, builder.Configuration);
    }
    return;
}
app.Run();
public partial class Program { }