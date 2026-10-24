using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BookNest.Api.Data;
using BookNest.Api.DTOs;
using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using BookNest.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BookNest.Tests;
[CollectionDefinition("MariaDB", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<TestHost> { }
public class TestHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string Connection { get; }
    public bool Enabled { get; }
    public TestHost()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", new string('t', 64));
        var input = Environment.GetEnvironmentVariable("BOOKNEST_TEST_CONNECTION");
        Enabled = !string.IsNullOrWhiteSpace(input);
        Connection = input ?? "Server=localhost;Database=booknest_test_unconfigured;User=root;";
        var cs = new MySqlConnectionStringBuilder(Connection);
        if (!cs.Database.StartsWith("booknest_test_", StringComparison.Ordinal) || cs.Database.Length < 20) throw new InvalidOperationException("Test database name must start booknest_test_ and be dedicated to this run.");
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:BookNest"] = Connection, ["Jwt:Secret"] = new string('t', 64), ["AI_API_KEY"] = "", ["AI_MODEL"] = "" }));
        builder.ConfigureServices(services =>
        {
            var old = services.Where(d => d.ServiceType == typeof(DbContextOptions<BookNestDb>) || d.ServiceType == typeof(BookNestDb)).ToList(); foreach (var d in old) services.Remove(d);
            services.AddDbContext<BookNestDb>(o => o.UseMySql(Connection, new MariaDbServerVersion(new Version(10, 4, 32))));
            // Rate limiting is tested separately; business tests must not share a quota.
            var limits = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<RateLimiterOptions>)).ToList(); foreach (var d in limits) services.Remove(d);
            services.Configure<RateLimiterOptions>(o => { o.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("test")); o.AddPolicy("chat", _ => RateLimitPartition.GetNoLimiter("test")); o.AddPolicy("password", _ => RateLimitPartition.GetNoLimiter("test")); });
        });
    }
    public async Task InitializeAsync()
    {
        if (!Enabled) return;
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); await db.Database.MigrateAsync();
        if (!await db.Users.AnyAsync(u => u.Email == "business-admin@example.test")) { db.Users.Add(new User { FullName = "Business test admin", Email = "business-admin@example.test", NormalizedEmail = "BUSINESS-ADMIN@EXAMPLE.TEST", Role = "Admin" }); await db.SaveChangesAsync(); }
    }
    async Task IAsyncLifetime.DisposeAsync() { await DisposeAsync(); }
}
[Collection("MariaDB")]
public class BusinessTests(TestHost host)
{
    private HttpClient Admin()
    {
        var client = host.CreateClient();
        var token = new JwtSecurityToken("BookNest", "BookNest.Client", [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, "Admin"), new Claim("auth_version", "1")], expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('t',64))), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token)); return client;
    }
    private static string Id() => Guid.NewGuid().ToString("N");
    private async Task<(HttpClient Client, int UserId)> Customer(string? email = null)
    {
        var c = host.CreateClient(); var res = await c.PostAsJsonAsync("/api/auth/register", new { fullName = "Khách kiểm thử", email = email ?? Id() + "@example.test", password = "Secure-Test-123!", confirmPassword = "Secure-Test-123!" }); res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<JsonElement>(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("token").GetString()); return (c, data.GetProperty("user").GetProperty("id").GetInt32());
    }
    private async Task<Product> Product(int stock = 5, decimal price = 125000)
    {
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>();
        var cat = new Category { Name = "Kiểm thử", Slug = Id() }; var p = new Product { Category = cat, Name = "Sách kiểm thử", Slug = Id(), SKU = Id(), Author = "Test", Publisher = "Test", Description = "Dữ liệu test độc lập", Price = price, StockQuantity = stock }; db.Products.Add(p); await db.SaveChangesAsync(); return p;
    }
    private static CheckoutDto Checkout(params CartLine[] lines) => new("Khách kiểm thử", "0901234567", "Địa chỉ kiểm thử tại Việt Nam", "", Id(), lines);
    private static async Task<Order> Create(HttpClient c, CheckoutDto dto) { var r = await c.PostAsJsonAsync("/api/orders", dto); r.EnsureSuccessStatusCode(); return (await r.Content.ReadFromJsonAsync<Order>())!; }
    private async Task<int> Stock(int id) { using var scope = host.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<BookNestDb>().Products.Where(p => p.Id == id).Select(p => p.StockQuantity).SingleAsync(); }
    [Fact] public void QuantityAndFingerprintAreCanonical()
    {
        var dto = Checkout(new CartLine(2, 1), new(1, 1), new(2, 2)); var lines = OrderService.Normalize(dto.Items); Assert.Equal(2, lines.Length); Assert.Equal(3, lines[1].Quantity);
        Assert.Equal(OrderService.Fingerprint(dto, lines), OrderService.Fingerprint(dto with { Items = [new(1, 1), new(2, 3)] }, OrderService.Normalize([new(1, 1), new(2, 3)])));
        Assert.Throws<AppException>(() => OrderService.Normalize([new(1, -1)]));
    }
    [Fact] public void VietnamRangeConvertsToUtc()
    {
        var (start, end) = DashboardService.Range(new DateOnly(2026,10,23), new DateOnly(2026,10,23)); Assert.Equal(new DateTime(2026,10,22,17,0,0,DateTimeKind.Utc), start); Assert.Equal(24, (end-start).TotalHours);
    }
    [Fact] public async Task DuplicateEmailAndAdminAuthorization()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var email = Id() + "@example.test"; var (c, _) = await Customer(email); var r = await c.PostAsJsonAsync("/api/auth/register", new { fullName = "Test", email = email.ToUpperInvariant(), password = "Password123!", confirmPassword = "Password123!", role = "Admin" }); Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/admin/products")).StatusCode);
    }
    [Fact] public async Task OwnershipAndSnapshotAndServerPrice()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (a, _) = await Customer(); var (b, _) = await Customer(); var p = await Product(); var dto = Checkout(new CartLine(p.Id, 2));
        var response = await a.PostAsJsonAsync("/api/orders", new { dto.RecipientName, dto.RecipientPhone, dto.ShippingAddress, dto.Note, dto.IdempotencyKey, dto.Items, totalAmount = 1, unitPrice = 1 }); response.EnsureSuccessStatusCode(); var o = (await response.Content.ReadFromJsonAsync<Order>())!; Assert.Equal(250000, o.TotalAmount);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/orders/{o.Id}")).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/orders/{o.Id}/simulate-payment", new { success = true })).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/orders/{o.Id}/cancel", null)).StatusCode);
        using (var scope = host.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); await db.Products.Where(x => x.Id == p.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Price, 999999).SetProperty(x => x.IsActive, false)); }
        var saved = (await a.GetFromJsonAsync<Order>($"/api/orders/{o.Id}"))!; Assert.Equal(125000, saved.Items[0].UnitPrice); Assert.Equal(p.Name, saved.Items[0].ProductNameSnapshot);
    }
    [Fact] public async Task InvalidQuantityAndTransactionRollback()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(3); var outOfStock = await Product(0);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/orders", Checkout(new CartLine(p.Id, 0)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/orders", Checkout(new CartLine(p.Id, 1), new CartLine(outOfStock.Id, 1)))).StatusCode); Assert.Equal(3, await Stock(p.Id));
    }
    [Fact] public async Task LastCopyHasOnlyOneBuyer()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (a, _) = await Customer(); var (b, _) = await Customer(); var p = await Product(1);
        var results = await Task.WhenAll(a.PostAsJsonAsync("/api/orders", Checkout(new CartLine(p.Id, 1))), b.PostAsJsonAsync("/api/orders", Checkout(new CartLine(p.Id, 1)))); Assert.Single(results.Where(r => r.IsSuccessStatusCode)); Assert.Equal(0, await Stock(p.Id));
    }
    [Fact] public async Task ConcurrentRetryClaimsKeyOnlyOnce()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(8); var dto = Checkout(new CartLine(p.Id, 2));
        var responses = await Task.WhenAll(c.PostAsJsonAsync("/api/orders", dto), c.PostAsJsonAsync("/api/orders", dto)); foreach (var r in responses) r.EnsureSuccessStatusCode(); var first = (await responses[0].Content.ReadFromJsonAsync<Order>())!; var second = (await responses[1].Content.ReadFromJsonAsync<Order>())!; Assert.Equal(first.Id, second.Id); Assert.Equal(6, await Stock(p.Id));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/orders", dto with { Note = "Nội dung khác" })).StatusCode);
    }
    [Fact] public async Task PaidCannotBePaidAgainOrCancelled()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(); var o = await Create(c, Checkout(new CartLine(p.Id, 1)));
        (await c.PostAsJsonAsync($"/api/orders/{o.Id}/simulate-payment", new { success = true })).EnsureSuccessStatusCode(); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/orders/{o.Id}/simulate-payment", new { success = true })).StatusCode); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync($"/api/orders/{o.Id}/cancel", null)).StatusCode); Assert.Equal(4, await Stock(p.Id));
    }
    [Fact] public async Task CancelRestoresStockExactlyOnce()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(); var o = await Create(c, Checkout(new CartLine(p.Id, 2))); var results = await Task.WhenAll(c.PostAsync($"/api/orders/{o.Id}/cancel", null), c.PostAsync($"/api/orders/{o.Id}/cancel", null)); Assert.Single(results.Where(r => r.IsSuccessStatusCode)); Assert.Equal(5, await Stock(p.Id));
    }
    [Fact] public async Task PaymentAndCancellationCannotBothWin()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(); var o = await Create(c, Checkout(new CartLine(p.Id, 1))); var results = await Task.WhenAll(c.PostAsJsonAsync($"/api/orders/{o.Id}/simulate-payment", new { success = true }), c.PostAsync($"/api/orders/{o.Id}/cancel", null)); Assert.Single(results.Where(r => r.IsSuccessStatusCode)); var saved = (await c.GetFromJsonAsync<Order>($"/api/orders/{o.Id}"))!; Assert.False(saved.OrderStatus == "Cancelled" && saved.PaymentStatus == "Paid"); Assert.Equal(saved.OrderStatus == "Cancelled" ? 5 : 4, await Stock(p.Id));
    }
    [Fact] public async Task OptimisticVersionDetectsOrderStockChange()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, _) = await Customer(); var p = await Product(); using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); var stale = await db.Products.SingleAsync(x => x.Id == p.Id); await Create(c, Checkout(new CartLine(p.Id, 1))); stale.StockQuantity = 50; stale.Version++; await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync()); Assert.Equal(4, await Stock(p.Id));
    }
    [Fact] public async Task InvalidTransitionsAndRevenueAndReviewEligibility()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var (c, uid) = await Customer(); var p = await Product(); var o = await Create(c, Checkout(new CartLine(p.Id, 1)));
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync($"/api/products/{p.Id}/reviews", new { rating = 5, comment = "Chưa mua xong" })).StatusCode);
        using var scope = host.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<OrderService>(); await Assert.ThrowsAsync<AppException>(() => service.Change(o.Id, 0, true, "Completed", default)); await Assert.ThrowsAsync<AppException>(() => service.Change(o.Id, 0, true, "Shipping", default));
        await service.Change(o.Id, uid, false, "Paid", default); await service.Change(o.Id, 0, true, "Shipping", default); await service.Change(o.Id, 0, true, "Completed", default); await Assert.ThrowsAsync<AppException>(() => service.Change(o.Id, 0, true, "Pending", default));
        (await c.PostAsJsonAsync($"/api/products/{p.Id}/reviews", new { rating = 5, comment = "Sách rất hữu ích" })).EnsureSuccessStatusCode(); Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/products/{p.Id}/reviews", new { rating = 4, comment = "Trùng" })).StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<BookNestDb>(); var expected = await db.Orders.Where(x => x.OrderStatus == "Completed" && x.PaymentStatus == "Paid").SumAsync(x => x.TotalAmount); var report = JsonSerializer.SerializeToElement(await scope.ServiceProvider.GetRequiredService<DashboardService>().Get(null, null, default)); Assert.Equal(expected, report.GetProperty("totalRevenue").GetDecimal());
    }
    [Fact] public async Task CombinedFilteringAndPagination()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var p = await Product(5, 125000); var c = host.CreateClient(); var result = (await c.GetFromJsonAsync<PageResult<ProductView>>($"/api/products?search=ki%E1%BB%83m&categoryId={p.CategoryId}&minPrice=120000&maxPrice=130000&sort=priceAsc&pageSize=1"))!; Assert.Equal(1, result.TotalItems); Assert.Equal(p.Id, result.Items.Single().Id); Assert.Equal(1, result.TotalPages); Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/products?minPrice=200&maxPrice=100")).StatusCode);
    }
    [Fact] public async Task MissingAiKeyIsSafeAndCatalogStillWorks()
    {
        Assert.True(host.Enabled, "Run scripts/Test-MariaDb.ps1 for isolated MariaDB.");
        var c = host.CreateClient(); Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PostAsJsonAsync("/api/chat", new { message = "Tư vấn sách" })).StatusCode); (await c.GetAsync("/api/products")).EnsureSuccessStatusCode();
    }
    [Fact] public async Task AdminStockApiRejectsStaleVersionAndPrivilegedPayment()
    {
        Assert.True(host.Enabled, "Run isolated MariaDB tests.");
        var p = await Product(); var (customer, _) = await Customer(); var admin = Admin();
        var o = await Create(customer, Checkout(new CartLine(p.Id,1)));
        var stale = new ProductWriteDto(p.Name,p.Slug,p.SKU,p.CategoryId,p.Author,p.Publisher,p.Description,p.Price,50,true,false,p.Version,[]);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/admin/products/{p.Id}",stale)).StatusCode);
        Assert.Equal(4,await Stock(p.Id));
        Assert.Equal(HttpStatusCode.BadRequest,(await admin.PatchAsJsonAsync($"/api/admin/orders/{o.Id}/status",new { status="Paid" })).StatusCode);
    }
    [Fact] public async Task UploadDecodesContentAndRejectsDisguisedSvg()
    {
        Assert.True(host.Enabled, "Run isolated MariaDB tests.");
        var admin = Admin();
        using var bad = new MultipartFormDataContent(); bad.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'></svg>")),"file","cover.jpg");
        Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsync("/api/admin/uploads/images",bad)).StatusCode);
        using var bitmap = new SkiaSharp.SKBitmap(2,2); bitmap.Erase(SkiaSharp.SKColors.Blue); using var image = SkiaSharp.SKImage.FromBitmap(bitmap); using var png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);
        using var good = new MultipartFormDataContent(); good.Add(new ByteArrayContent(png.ToArray()),"file","original.png");
        var response = await admin.PostAsync("/api/admin/uploads/images",good); response.EnsureSuccessStatusCode(); var data = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Matches("^/uploads/[a-f0-9]{32}\\.png$",data.GetProperty("imageUrl").GetString()!);
    }
    [Fact] public async Task HiddenCategoryCannotBePurchased()
    {
        Assert.True(host.Enabled, "Run isolated MariaDB tests.");
        var p = await Product(); var admin=Admin(); (await admin.DeleteAsync($"/api/admin/categories/{p.CategoryId}")).EnsureSuccessStatusCode();
        var (c, _) = await Customer(); Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/orders",Checkout(new CartLine(p.Id,1)))).StatusCode); Assert.Equal(5,await Stock(p.Id));
    }
    [Fact] public async Task ExpiredAndForgedJwtAreRejected()
    {
        Assert.True(host.Enabled, "Run isolated MariaDB tests.");
        var c=host.CreateClient();
        foreach(var signingKey in new[] { new string('t',64), new string('x',64) })
        {
            var token=new JwtSecurityToken("BookNest","BookNest.Client",[new Claim(ClaimTypes.NameIdentifier,"1"),new Claim(ClaimTypes.Role,"Admin")],notBefore:DateTime.UtcNow.AddHours(-2),expires:DateTime.UtcNow.AddHours(-1),signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),SecurityAlgorithms.HmacSha256));
            c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new JwtSecurityTokenHandler().WriteToken(token)); Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/admin/orders")).StatusCode);
        }
    }
    private sealed class ProviderFailure : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
    [Fact] public async Task AiProviderErrorIsSanitized()
    {
        Assert.True(host.Enabled, "Run isolated MariaDB tests.");
        using var scope=host.Services.CreateScope(); using var http=new HttpClient(new ProviderFailure());
        var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["AI_PROVIDER"]="Gemini",["AI_API_KEY"]="test-only-key",["AI_MODEL"]="test-model" }).Build();
        var service=new ChatService(http,scope.ServiceProvider.GetRequiredService<BookNestDb>(),cfg); var ex=await Assert.ThrowsAsync<AppException>(()=>service.Ask("Tư vấn sách",default)); Assert.Equal(503,ex.Status); Assert.Equal("Trợ lý hiện chưa sẵn sàng",ex.Message);
    }
}