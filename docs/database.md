# Thiết kế database

Database: `booknest_db`. Provider: EF Core 8.0.22 + Microsoft SQL Server provider 8.0.22. Local connection defaults to SQL Server `ADMIN` with Windows Authentication. Migrations under `backend/BookNest.Api/Migrations` target SQL Server only. SQL Server uses Unicode `nvarchar`, decimal(18,2), and UTC `datetime2`; UI/report uses UTC+7. Existing MariaDB data is not transferred by applying these migrations.

## Quan hệ

```text
Users 1 ── n Orders 1 ── n OrderItems n ── 1 Products n ── 1 Categories
Users 1 ── n Reviews n ── 1 Products
Products 1 ── n ProductImages
```

## Bảng / ràng buộc

| Bảng | Thuộc tính chính / quy tắc |
|---|---|
| Users | FullName, Email, NormalizedEmail unique, PasswordHash, Role, CreatedAt |
| Categories | Name, Slug unique, Description, IsActive, CreatedAt, UpdatedAt |
| Products | FK CategoryId, Name, Slug/SKU unique, Author, Publisher, Description, Price, StockQuantity, Version, IsActive/IsFeatured, CreatedAt/UpdatedAt |
| ProductImages | FK ProductId, ImageUrl, SortOrder |
| Orders | OrderCode unique, FK UserId, RecipientName/Phone/ShippingAddress/Note, Subtotal/ShippingFee/TotalAmount, OrderStatus, PaymentStatus, IdempotencyKey, RequestHash, CreatedAt/UpdatedAt/PaidAt/CompletedAt/CancelledAt |
| OrderItems | FK OrderId/ProductId, ProductNameSnapshot, SKUSnapshot, UnitPrice, Quantity, LineTotal |
| Reviews | FK UserId/ProductId, Rating, Comment, CreatedAt/UpdatedAt; unique UserId+ProductId |

- Unique `(UserId, IdempotencyKey)` là cổng chặn retry đồng thời.
- Check StockQuantity >= 0, Price > 0, OrderItem.Quantity > 0, Rating 1..5.
- Version là application-managed long concurrency token, tăng khi Admin sửa, đơn trừ/hoàn tồn, ngừng bán.
- Index FK do EF tạo, thêm Products(IsActive,CategoryId,CreatedAt), Orders(CreatedAt), Orders(OrderStatus,PaymentStatus,CompletedAt).
- DeleteBehavior.Restrict cho FK: không cascade mất đơn. Controller chỉ ẩn danh mục/sản phẩm. Thay ảnh xóa các dòng ProductImages cụ thể, không xóa Product.
- Cột string mặc định `nvarchar(255)`, trường nội dung dài `nvarchar(4000)`. DTO giới hạn chặt hơn ở một số trường (review 1000, địa chỉ/ghi chú 1000).
- Trạng thái là string nội bộ và chuyển bằng service, không có DTO public cho thay trực tiếp trạng thái/tiền/role.

Seed idempotent theo email chuẩn hóa, slug category, SKU demo và idempotency key đơn. Không ghi đè dữ liệu/mật khẩu đã tồn tại. Demo seed không chạy Production.