# Quyết định công nghệ / nguồn chính thức

Ngày kiểm tra môi trường: 23/10/2026. Không dùng latest cho mọi package. Pomelo nhánh 8 phù hợp EF Core 8; project target net8.0 dù SDK máy là 9. Explicit server type MariaDB 10.4.32, không MySQL 8 và không AutoDetect kết nối lúc build.

Tài liệu chính thức đã đối chiếu:

```text
Pomelo 8.0.3 / dependencies:
https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql/8.0.3
https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/releases

EF Core application-managed optimistic concurrency:
https://learn.microsoft.com/en-us/ef/core/saving/concurrency

MariaDB InnoDB SELECT FOR UPDATE (cần explicit transaction):
https://mariadb.com/docs/server/reference/sql-statements/data-manipulation/selecting-data/for-update

Gemini models.list/models.get (khả dụng theo key, không đoán tên):
https://ai.google.dev/api/models
Gemini generateContent/systemInstruction/JSON response:
https://ai.google.dev/api/generate-content

Vite 7 / Node yêu cầu:
https://v7.vite.dev/guide/migration
https://vite.dev/guide/

SkiaSharp .NET package và release:
https://www.nuget.org/packages/SkiaSharp/3.119.0
https://github.com/mono/SkiaSharp/blob/main/documentation/docfx/releases/3.119.0.md
```

Pomelo 8.0.3 chấp nhận EF Relational 8.0.13..8.0.999, đã chọn 8.0.22 và pin cả API/tests để tránh test project giải transitive về EF 8.0.13. Build/test thực tế đã chạy với MariaDB XAMPP 10.4.32. Phiên bản MariaDB 10.4 cũ: dùng để đáp ứng môi trường đồ án hiện tại, không tự nâng server/can thiệp dự án khác.

Đã bỏ ImageSharp trong quá trình kiểm tra vì warning bảo mật/license ở các nhánh thử ban đầu; **mã cuối dùng SkiaSharp**, không cần license ImageSharp và không còn reference ImageSharp. Upload dùng codec allowlist và PNG encode mới, không chỉ extension/Content-Type.

Gemini model không hardcode. Script đọc model chính thức theo key; backend get model kiểm tra `supportedGenerationMethods`. Không có key thì không thể kết luận model tài khoản thực sự truy cập được hay quota/rate upstream còn đủ.