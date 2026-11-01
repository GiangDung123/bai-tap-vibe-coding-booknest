# Quyết định công nghệ

Backend target `net8.0`, dùng Entity Framework Core 8.0.22 với provider `Microsoft.EntityFrameworkCore.SqlServer` 8.0.22. Cấu hình dùng `UseSqlServer`; SQL Server version được thương lượng khi kết nối, không cần hardcode version của instance.

Connection string local mặc định dùng Windows Authentication tới server `ADMIN` và database `booknest_db`. Windows account chạy API cần được SQL Server cấp quyền. Có thể đặt named instance qua `Setup-Local.ps1 -ServerName 'ADMIN\SQLEXPRESS'`.

Các migration trong `backend/BookNest.Api/Migrations` dành riêng cho SQL Server. Không dùng các migration này để cập nhật MariaDB/MySQL. Chuyển provider không tự động chuyển dữ liệu; cần quy trình export/import riêng nếu phải giữ database cũ.

SQL-specific locking dùng `UPDLOCK` cho ghi tồn kho/trạng thái và `HOLDLOCK` để giữ khóa danh mục trong transaction. Lỗi unique constraint `2601`/`2627` được ánh xạ thành HTTP 409; deadlock và lock timeout được xử lý như xung đột có thể thử lại.

## Tài liệu tham khảo

```text
EF Core SQL Server provider:
https://learn.microsoft.com/ef/core/providers/sql-server/

EF Core optimistic concurrency:
https://learn.microsoft.com/ef/core/saving/concurrency

SQL Server table hints:
https://learn.microsoft.com/sql/t-sql/queries/hints-transact-sql-table

Gemini models.list/models.get:
https://ai.google.dev/api/models

Gemini generateContent:
https://ai.google.dev/api/generate-content
```
