using BookNest.Api.Entities;
using BookNest.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.Data;
public static class Seed
{
    public static async Task Run(IServiceProvider services, IConfiguration config)
    {
        await RunAccounts(services, config);
        var db = services.GetRequiredService<BookNestDb>();
        var names = new[] { "Văn học", "Kinh tế", "Kỹ năng sống", "Công nghệ", "Thiếu nhi" };
        var slugs = new[] { "van-hoc", "kinh-te", "ky-nang-song", "cong-nghe", "thieu-nhi" };
        for (var i = 0; i < names.Length; i++) if (!await db.Categories.AnyAsync(c => c.Slug == slugs[i])) db.Categories.Add(new Category { Name = names[i], Slug = slugs[i], Description = "Tủ sách " + names[i].ToLowerInvariant() });
        await db.SaveChangesAsync();
        string[][] titles = [["Những ngày nắng trong", "Đi qua mùa gió", "Một góc trời bình yên", "Chuyện kể bên hiên", "Những lá thư chưa gửi"], ["Tư duy kinh doanh bền vững", "Tài chính từ những điều nhỏ", "Bước đầu khởi nghiệp", "Thấu hiểu khách hàng", "Kinh tế quanh ta"], ["Sống chậm giữa phố đông", "Thói quen nhỏ mỗi ngày", "Nghệ thuật lắng nghe", "Hành trình hiểu chính mình", "Tập trung vào điều quan trọng"], ["Lập trình từ con số không", "Khám phá dữ liệu", "Thiết kế web hiện đại", "Trí tuệ nhân tạo nhập môn", "Câu chuyện của thuật toán"], ["Khu vườn của Mây", "Chuyến phiêu lưu của Cáo", "Bầu trời đầy câu hỏi", "Bạn nhỏ và rừng xanh", "Ngày đầu đến trường"]];
        for (var c = 0; c < 5; c++) for (var j = 0; j < 5; j++)
        {
            var sku = $"BN-DEMO-{c + 1}{j + 1}"; if (await db.Products.AnyAsync(p => p.SKU == sku)) continue;
            var cat = await db.Categories.SingleAsync(x => x.Slug == slugs[c]);
            db.Products.Add(new Product { Name = titles[c][j], Slug = $"sach-minh-hoa-{c + 1}-{j + 1}", SKU = sku, CategoryId = cat.Id, Author = new[] { "An Nhiên", "Minh Anh", "Hoàng Nam", "Thu Hà", "Nhóm BookNest" }[j], Publisher = "Nhà xuất bản minh họa BookNest", Description = $"{titles[c][j]} là sách minh họa do BookNest xây dựng cho đồ án E-Commerce, không đại diện cho một ấn phẩm thương mại thực tế. Nội dung giới thiệu những góc nhìn gần gũi về {names[c].ToLowerInvariant()}, phù hợp để khám phá và mở rộng kiến thức. Dữ liệu tên sách, tác giả và ảnh bìa đều phục vụ mục đích học tập.", Price = 65000 + c * 23000 + j * 15000, StockQuantity = j == 4 ? 0 : j == 3 ? 2 : 30, IsFeatured = j < 2, Images = [new ProductImage { ImageUrl = $"/images/cover-{c + 1}.png" }] });
        }
        await db.SaveChangesAsync();
        // Idempotent demo orders go through stock reservations and legal state transitions.
        var customer = await db.Users.SingleAsync(u => u.NormalizedEmail == "CUSTOMER@BOOKNEST.LOCAL");
        var orderService = services.GetRequiredService<BookNest.Api.Services.OrderService>();
        for (var i = 0; i < 5; i++)
        {
            var key = $"development-demo-order-{i + 1}"; if (await db.Orders.AnyAsync(o => o.UserId == customer.Id && o.IdempotencyKey == key)) continue;
            var p = await db.Products.AsNoTracking().FirstAsync(p => p.SKU == $"BN-DEMO-{i + 1}1");
            var o = await orderService.Create(customer.Id, new DTOs.CheckoutDto("Bạn đọc minh họa", "0900000000", "Địa chỉ minh họa, TP. Hồ Chí Minh", "Đơn seed Development", key, [new DTOs.CartLine(p.Id, 1)]), default);
            if (i < 3) { await orderService.Change(o.Id, customer.Id, false, "Paid", default); await orderService.Change(o.Id, 0, true, "Shipping", default); await orderService.Change(o.Id, 0, true, "Completed", default); o.CompletedAt = DateTime.UtcNow.AddDays(-i); await db.SaveChangesAsync(); }
            if (i == 3) await orderService.Change(o.Id, customer.Id, false, "Failed", default);
            db.ChangeTracker.Clear();
        }
    }

    public static async Task RunAccounts(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<BookNestDb>(); var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        foreach (var (email, name, role, key) in new[] { ("admin@booknest.local", "Quản trị BookNest", "Admin", "AdminPassword"), ("customer@booknest.local", "Bạn đọc BookNest", "Customer", "CustomerPassword") })
        {
            if (await db.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant())) continue;
            var password = config["Seed:" + key]; if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new AppException(400, "Cần cấu hình Seed:" + key + " (ít nhất 8 ký tự) ở cục bộ.");
            var u = new User { Email = email, NormalizedEmail = email.ToUpperInvariant(), FullName = name, Role = role }; u.PasswordHash = hasher.HashPassword(u, password); db.Users.Add(u);
        }
        await db.SaveChangesAsync();
    }
}