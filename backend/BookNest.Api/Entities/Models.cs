namespace BookNest.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
    public string PhoneNumber { get; set; } = "";
    public string DefaultShippingAddress { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public long AuthVersion { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SecurityGuard { public int Id { get; set; } }
public class UserAudit
{
    public long Id { get; set; }
    public int ActorId { get; set; }
    public int TargetId { get; set; }
    public string Action { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string SKU { get; set; } = "";
    public string Author { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public long Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductImage> Images { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}
public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = "";
    public int SortOrder { get; set; }
}
public class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = "";
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public string Note { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductNameSnapshot { get; set; } = "";
    public string SKUSnapshot { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}