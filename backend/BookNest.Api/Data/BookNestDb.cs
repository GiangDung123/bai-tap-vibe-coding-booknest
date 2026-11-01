using BookNest.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Api.Data;
public class BookNestDb(DbContextOptions<BookNestDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SecurityGuard> SecurityGuards => Set<SecurityGuard>();
    public DbSet<UserAudit> UserAudits => Set<UserAudit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Entity<SecurityGuard>().HasData(new SecurityGuard { Id = 1 });
        b.Entity<User>().HasIndex(x => new { x.Role, x.IsActive });
        b.Entity<User>().Property(x => x.IsActive).HasDefaultValue(true);
        b.Entity<User>().Property(x => x.AuthVersion).HasDefaultValue(1L);
        b.Entity<UserAudit>().HasIndex(x => new { x.TargetId, x.CreatedAt });
        b.Entity<UserAudit>().HasOne<User>().WithMany().HasForeignKey(x => x.ActorId);
        b.Entity<UserAudit>().HasOne<User>().WithMany().HasForeignKey(x => x.TargetId);
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.SKU).IsUnique();
        b.Entity<Product>().HasIndex(x => new { x.IsActive, x.CategoryId, x.CreatedAt });
        b.Entity<Product>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        b.Entity<Order>().HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
        b.Entity<Order>().HasIndex(x => new { x.OrderStatus, x.PaymentStatus, x.CompletedAt });
        b.Entity<Order>().HasIndex(x => x.CreatedAt);
        b.Entity<Review>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        b.Entity<Product>().ToTable(t => { t.HasCheckConstraint("CK_Product_Stock", "StockQuantity >= 0"); t.HasCheckConstraint("CK_Product_Price", "Price > 0"); });
        b.Entity<OrderItem>().ToTable(t => t.HasCheckConstraint("CK_Item_Quantity", "Quantity > 0"));
        b.Entity<Review>().ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "Rating BETWEEN 1 AND 5"));
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var p in entity.GetProperties())
            {
                if (p.ClrType == typeof(string)) p.SetMaxLength(p.Name is "Description" or "Comment" or "ShippingAddress" or "Note" ? 4000 : p.Name == "DefaultShippingAddress" ? 1000 : p.Name == "PhoneNumber" ? 40 : 255);
                if (p.ClrType == typeof(decimal)) { p.SetPrecision(18); p.SetScale(2); }
            }
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}