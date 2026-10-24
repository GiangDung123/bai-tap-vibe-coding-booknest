using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BookNest.Api.Data;
using BookNest.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BookNest.Tests;
[Collection("MariaDB")]
public class AccountTests(TestHost host)
{
    private const string Password = "Account-Test-123!";
    private async Task<(HttpClient Client, User User)> Create(string role = "Customer")
    {
        Assert.True(host.Enabled, "Use scripts/Test-MariaDb.ps1; never run mutations against the real database.");
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>();
        var u = new User { FullName = "Account Test", Email = Guid.NewGuid().ToString("N") + "@example.test", Role = role };
        u.NormalizedEmail = u.Email.ToUpperInvariant(); u.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(u, Password);
        db.Users.Add(u); await db.SaveChangesAsync(); return (await Login(u.Email, Password), u);
    }
    private async Task<HttpClient> Login(string email, string password)
    {
        var c = host.CreateClient(); var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode); var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString()); return c;
    }
    [Fact] public async Task OwnProfileRejectsPrivilegedFieldsAndKeepsOrderSnapshots()
    {
        var (c, u) = await Create(); var (_, other) = await Create();
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PatchAsJsonAsync($"/api/admin/users/{other.Id}/status", new { isActive = false })).StatusCode);
        foreach (var payload in new object[] { new { fullName = "Injected", role = "Admin" }, new { fullName = "Injected", isActive = false }, new { fullName = "Injected", userId = other.Id } })
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync("/api/account/profile", payload)).StatusCode);
        using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); db.Orders.Add(new Order { UserId = u.Id, OrderCode = Guid.NewGuid().ToString("N"), IdempotencyKey = Guid.NewGuid().ToString("N"), RecipientName = "Old recipient", ShippingAddress = "Old address" }); await db.SaveChangesAsync(); }
        var response = await c.PutAsJsonAsync("/api/account/profile", new { fullName = "  Updated   Name  ", phoneNumber = "+44 (0)20 1234 5678 ext 12", defaultShippingAddress = "New default address" });
        response.EnsureSuccessStatusCode(); var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("authVersion", text); Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(u.Id, profile.GetProperty("id").GetInt32()); Assert.Equal("Updated Name", profile.GetProperty("fullName").GetString()); Assert.Equal("Customer", profile.GetProperty("role").GetString());
        using var check = host.Services.CreateScope(); var database = check.ServiceProvider.GetRequiredService<BookNestDb>(); Assert.Equal("Old address", (await database.Orders.SingleAsync(o => o.UserId == u.Id)).ShippingAddress); Assert.Equal("Account Test", (await database.Users.FindAsync(other.Id))!.FullName);
    }
    [Fact] public async Task LockUnlockAndRoleChangesPermanentlyRevokeOldTokensAndAudit()
    {
        var (admin, a) = await Create("Admin"); var (c, u) = await Create();
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/admin/users/{a.Id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/admin/users/{a.Id}/role", new { role = "Customer" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/admin/users/{u.Id}/role", new { role = "Owner" })).StatusCode);
        (await admin.PatchAsJsonAsync($"/api/admin/users/{u.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/account/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = u.Email, password = Password })).StatusCode);
        (await admin.PatchAsJsonAsync($"/api/admin/users/{u.Id}/status", new { isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/account/profile")).StatusCode);
        var fresh = await Login(u.Email, Password); (await fresh.GetAsync("/api/account/profile")).EnsureSuccessStatusCode();
        (await admin.PatchAsJsonAsync($"/api/admin/users/{u.Id}/role", new { role = "Admin" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.GetAsync("/api/account/profile")).StatusCode);
        var promoted = await Login(u.Email, Password); (await promoted.GetAsync("/api/admin/users")).EnsureSuccessStatusCode();
        (await admin.PatchAsJsonAsync($"/api/admin/users/{u.Id}/role", new { role = "Customer" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await promoted.GetAsync("/api/admin/users")).StatusCode);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); var audits = await db.UserAudits.Where(x => x.TargetId == u.Id).ToListAsync(); Assert.Equal(4, audits.Count); Assert.All(audits, x => { Assert.Equal(a.Id, x.ActorId); Assert.NotEqual(x.OldValue, x.NewValue); Assert.True(x.CreatedAt > DateTime.UtcNow.AddMinutes(-5)); });
    }
    [Fact] public async Task PasswordChangeChecksPolicyAndRevokesAllSessions()
    {
        var (c, u) = await Create(); var second = await Login(u.Email, Password);
        foreach (var p in new[] { new { currentPassword = "Incorrect", newPassword = "Changed-123!", confirmPassword = "Changed-123!" }, new { currentPassword = Password, newPassword = Password, confirmPassword = Password }, new { currentPassword = Password, newPassword = "short", confirmPassword = "short" }, new { currentPassword = Password, newPassword = "Changed-123!", confirmPassword = "Mismatch-123!" } }) Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/account/change-password", p)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/account/change-password", new { currentPassword = Password, newPassword = "Changed-123!", confirmPassword = "Changed-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/account/profile")).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = u.Email, password = Password })).StatusCode); (await (await Login(u.Email, "Changed-123!")).GetAsync("/api/account/profile")).EnsureSuccessStatusCode();
    }
    [Fact] public async Task AdminSearchPagingAndSpendingAreFilteredCorrectly()
    {
        var (admin, _) = await Create("Admin"); var (_, u) = await Create();
        using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); foreach (var status in new[] { ("Completed", "Paid"), ("Pending", "Paid"), ("Completed", "Unpaid") }) db.Orders.Add(new Order { UserId = u.Id, OrderCode = Guid.NewGuid().ToString("N"), IdempotencyKey = Guid.NewGuid().ToString("N"), OrderStatus = status.Item1, PaymentStatus = status.Item2, CompletedAt = status.Item1 == "Completed" ? DateTime.UtcNow : null, TotalAmount = 125000 }); await db.SaveChangesAsync(); }
        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users?search={u.Email}&role=Customer&isActive=true&pageSize=1&page=1"); Assert.Equal(1, page.GetProperty("totalItems").GetInt32()); Assert.Single(page.GetProperty("items").EnumerateArray());
        var empty = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users?search={u.Email}&role=Admin"); Assert.Equal(0, empty.GetProperty("totalItems").GetInt32());
        var second = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users?search={u.Email}&pageSize=1&page=2"); Assert.Empty(second.GetProperty("items").EnumerateArray()); Assert.Equal(1, second.GetProperty("totalItems").GetInt32());
        var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users/{u.Id}"); Assert.Equal(3, detail.GetProperty("totalOrders").GetInt32()); Assert.Equal(125000, detail.GetProperty("totalSpent").GetDecimal()); Assert.Equal(3, detail.GetProperty("recentOrders").GetArrayLength()); Assert.DoesNotContain("password", detail.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public async Task ConcurrentAdminsCannotRemoveEachOtherAndLoseLastActiveAdmin()
    {
        var (a, ua) = await Create("Admin"); var (b, ub) = await Create("Admin");
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>();
        var others = await db.Users.Where(u => u.Role == "Admin" && u.IsActive && u.Id != ua.Id && u.Id != ub.Id).Select(u => u.Id).ToListAsync();
        await db.Users.Where(u => others.Contains(u.Id)).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
        try {
            var responses = await Task.WhenAll(a.PatchAsJsonAsync($"/api/admin/users/{ub.Id}/status", new { isActive = false }), b.PatchAsJsonAsync($"/api/admin/users/{ua.Id}/role", new { role = "Customer" }));
            Assert.Single(responses, r => r.IsSuccessStatusCode); Assert.All(responses.Where(r => !r.IsSuccessStatusCode), r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Conflict }));
            Assert.Equal(1, await db.Users.CountAsync(u => u.IsActive && u.Role == "Admin"));
        } finally { await db.Users.Where(u => others.Contains(u.Id)).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, true)); }
    }
    [Fact] public async Task LegacyTokenWithoutVersionIsRejected()
    {
        var (_, u) = await Create();
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("BookNest", "BookNest.Client", [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, u.Id.ToString()), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")], expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(new string('t', 64))), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/account/profile")).StatusCode);
    }
    [Fact] public async Task BrowserFlowsOnDedicatedDatabase()
    {
        // Optional live browser harness: all credentials stay in process environment/memory.
        if (Environment.GetEnvironmentVariable("BOOKNEST_BROWSER_TEST") != "1") return;
        var (customer, u) = await Create(); var (admin, _) = await Create("Admin");
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var api = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root, "backend/BookNest.Api"), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        api.ArgumentList.Add(Path.Combine(root, "backend/BookNest.Api/bin/Debug/net8.0/BookNest.Api.dll"));
        api.Environment["ConnectionStrings__BookNest"] = host.Connection;
        api.Environment["Jwt__Secret"] = new string('t', 64);
        api.Environment["ASPNETCORE_URLS"] = "http://localhost:5081";
        api.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        api.Environment["Logging__LogLevel__Default"] = "Warning";
        using var server = System.Diagnostics.Process.Start(api)!;
        server.OutputDataReceived += (_, _) => { }; server.ErrorDataReceived += (_, _) => { }; server.BeginOutputReadLine(); server.BeginErrorReadLine();
        try {
            using var health = new HttpClient(); bool ready = false;
            for (var i = 0; i < 50; i++) { try { ready = (await health.GetAsync("http://localhost:5081/api/health")).IsSuccessStatusCode; if (ready) break; } catch (HttpRequestException) { } await Task.Delay(200); }
            Assert.True(ready, "Isolated API failed to start.");
            var node = new System.Diagnostics.ProcessStartInfo("node") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            node.ArgumentList.Add(Path.Combine(root, "scripts/account-regression.cjs"));
            node.Environment["BOOKNEST_BROWSER_CUSTOMER_TOKEN"] = customer.DefaultRequestHeaders.Authorization!.Parameter;
            node.Environment["BOOKNEST_BROWSER_ADMIN_TOKEN"] = admin.DefaultRequestHeaders.Authorization!.Parameter;
            node.Environment["BOOKNEST_BROWSER_CUSTOMER_ID"] = u.Id.ToString();
            using var browser = System.Diagnostics.Process.Start(node)!;
            var output = browser.StandardOutput.ReadToEndAsync(); var errors = browser.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try { await browser.WaitForExitAsync(timeout.Token); } catch { browser.Kill(true); throw; }
            Assert.True(browser.ExitCode == 0, await output + await errors);
        } finally { if (!server.HasExited) server.Kill(true); }
    }
}