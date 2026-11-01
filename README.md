# BookNest — Không gian sách của bạn

Ứng dụng thương mại điện tử bán sách với giao diện tiếng Việt, xây dựng bằng **Vue 3, TypeScript, ASP.NET Core 8 và SQL Server**. BookNest có không gian mua sắm dành cho khách hàng, trang quản trị riêng và API lưu trữ dữ liệu thực tế.

> Dự án phục vụ học tập và trình diễn. Sách/ảnh bìa seed là dữ liệu minh họa; thanh toán và vận chuyển được mô phỏng, không phát sinh giao dịch tiền thật.

## Giao diện

### Trang chủ

![Trang chủ BookNest](docs/screenshots/ui-refresh/home-1440.png)

### Đăng nhập

![Đăng nhập BookNest](docs/screenshots/auth-refresh/login-1440-1000.png)

### Quản lý người dùng

![Quản lý người dùng](docs/screenshots/account-management/users-1440.png)

Ảnh quản lý người dùng sử dụng tài khoản thử nghiệm. Thêm ảnh hồ sơ và các kích thước responsive trong `docs/screenshots/`.

## Chức năng

### Khách hàng

- Đăng ký, đăng nhập, đăng xuất; quay lại checkout sau khi đăng nhập.
- Khám phá sách, xem chi tiết, tìm kiếm, lọc/sắp xếp và phân trang phía server.
- Giỏ hàng lưu cục bộ; kiểm tra lại giá và tồn kho từ API.
- Đặt hàng với thông tin người nhận riêng cho từng đơn; idempotency key giúp hạn chế tạo đơn trùng khi thử lại request.
- Mô phỏng thanh toán thành công/thất bại, theo dõi và hủy đơn theo quy tắc nghiệp vụ.
- Viết/sửa đánh giá khi đã hoàn thành mua hàng.
- Cập nhật họ tên, số điện thoại và địa chỉ mặc định; tên trên header thay đổi ngay sau khi lưu.
- Đổi mật khẩu và đăng nhập lại, không mất giỏ hàng.

### Quản trị viên

- Dashboard doanh thu, thống kê và đơn hàng gần đây.
- Quản lý danh mục, sản phẩm, giá, tồn kho, trạng thái hiển thị và ảnh bìa.
- Theo dõi đơn hàng, cập nhật trạng thái theo luồng cho phép.
- Tìm người dùng theo họ tên/email, lọc vai trò/trạng thái và phân trang server.
- Xem chi tiết tài khoản, tổng đơn, tổng chi tiêu từ đơn **Completed + Paid**, đơn gần đây có liên kết sang chi tiết.
- Khóa/mở khóa và chuyển vai trò Customer/Admin với xác nhận và lịch sử thao tác.
- Backend chặn tự khóa/tự đổi vai trò; bảo vệ Admin hoạt động cuối cùng bằng transaction và khóa hàng SQL Server.

Không bổ sung xóa vĩnh viễn người dùng, Admin tạo tài khoản hoặc sửa mật khẩu/thông tin cá nhân thay người khác trong phạm vi hiện tại.

### Hồ sơ và bảo mật phiên đăng nhập

- Customer: `/account/profile`; Admin: `/admin/profile`. Email, vai trò và ngày tham gia chỉ đọc.
- Checkout khởi tạo bằng thông tin mặc định nhưng ưu tiên dữ liệu đã khôi phục, không tự ghi đè nội dung đang nhập. Đơn cũ giữ nguyên thông tin nhận hàng.
- JWT chứa `auth_version`; request xác thực kiểm tra trạng thái, phiên bản và vai trò hiện tại trong database.
- Khóa tài khoản, đổi vai trò hoặc đổi mật khẩu thu hồi token cũ. Mở khóa không khôi phục token đã thu hồi.
- Token thiếu claim phiên bản yêu cầu đăng nhập lại. Đổi mật khẩu giới hạn **5 request/người dùng/phút**.

### Chatbot tư vấn sách

Tích hợp REST Gemini ở backend, cấu hình tùy chọn. Khi chưa cấu hình hoặc dịch vụ không sẵn sàng, ứng dụng hiển thị lỗi phù hợp; các chức năng mua sắm vẫn hoạt động.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Frontend | Vue 3 Composition API, TypeScript, Vite 7 |
| Điều hướng và state | Vue Router 4, Pinia 3 |
| UI | CSS tùy chỉnh, Tailwind CSS 4, Lucide, Chart.js |
| HTTP client | Axios |
| Backend | ASP.NET Core 8 Web API, C# |
| Database | SQL Server, Entity Framework Core 8 |
| Xác thực | JWT Bearer, ASP.NET PasswordHasher |
| API documentation | Swagger / OpenAPI |
| Xử lý ảnh | SkiaSharp |
| Kiểm thử | xUnit, WebApplicationFactory, SQL Server riêng, Playwright + Chrome |

Dependency được khai báo trong project files và lockfile frontend. Backend target `net8.0`.

## Cấu trúc dự án

```text
BookNest/
├── BookNest.sln
├── backend/
│   ├── BookNest.Api/
│   │   ├── Controllers/       # REST API
│   │   ├── Data/              # DbContext và seed Development
│   │   ├── DTOs/              # Request/response contracts
│   │   ├── Entities/          # Mô hình dữ liệu
│   │   ├── Infrastructure/    # Xử lý lỗi và hạ tầng
│   │   ├── Migrations/        # EF Core migrations
│   │   ├── Services/          # Đơn hàng, dashboard, AI
│   │   └── wwwroot/           # Ảnh bìa và upload local
│   └── BookNest.Tests/        # Kiểm thử API/nghiệp vụ
├── frontend/src/
│   ├── api/                   # Axios client
│   ├── assets/
│   ├── components/
│   ├── layouts/               # Shop, Admin, Auth
│   ├── router/
│   ├── stores/                # Pinia auth/cart
│   ├── types/
│   ├── utils/
│   └── views/                 # Shop, Auth, Admin, Account
├── scripts/                   # Thiết lập và kiểm thử
└── docs/                      # Thiết kế, checklist, ảnh giao diện
```

## Cài đặt và chạy local

Hướng dẫn sử dụng **PowerShell trên Windows**. Thực hiện từ thư mục gốc của bản clone, trừ khi có chỉ dẫn khác.

### 1. Chuẩn bị môi trường

- .NET SDK 8 và ASP.NET Core Runtime 8.
- Node.js tương thích Vite 7: **20.19+ hoặc 22.12+**, kèm npm.
- SQL Server đang chạy; cấu hình mặc định dùng instance `ADMIN` và Windows Authentication. Tài khoản Windows cần được SQL Server cấp quyền kết nối và tạo database, hoặc database phải được tạo/cấp quyền trước.
- Git để clone/push repository.
- Google Chrome nếu chạy bộ kiểm thử trình duyệt hiện có.

Sau khi clone repository và mở thư mục dự án:

```powershell
dotnet tool restore
dotnet restore .\BookNest.sln
Push-Location .\frontend
npm ci
Pop-Location
```

### 2. Cấu hình local

**Cách A — dùng script Windows:**

```powershell
.\scripts\Setup-Local.ps1
# Thay ADMIN nếu SQL Server hiển thị server name khác:
# .\scripts\Setup-Local.ps1 -ServerName 'ADMIN\SQLEXPRESS'
```

Script cấu hình SQL Server với Windows Authentication, sinh JWT secret và mật khẩu demo ngẫu nhiên, không in secrets ra terminal và từ chối ghi đè cấu hình có sẵn. Windows account chạy API cần có quyền truy cập SQL Server; tên `ADMIN` mặc định có thể đổi nếu máy dùng named instance.

**Cách B — tự cấu hình:**

```powershell
Copy-Item .\backend\BookNest.Api\appsettings.Local.example.json `
          .\backend\BookNest.Api\appsettings.Local.json
```

Mở file vừa tạo và thay các placeholder:

| Khóa | Ý nghĩa |
| --- | --- |
| `ConnectionStrings:BookNest` | Server, database và Windows Authentication của SQL Server |
| `Jwt:Secret` | Chuỗi ngẫu nhiên đủ mạnh, ít nhất 32 byte |
| `Seed:AdminPassword` | Mật khẩu Admin demo, ít nhất 8 ký tự |
| `Seed:CustomerPassword` | Mật khẩu Customer demo, ít nhất 8 ký tự |
| `AI_PROVIDER` | `Gemini` nếu dùng chatbot |
| `AI_API_KEY` | API key backend; có thể để trống khi chưa dùng AI |
| `AI_MODEL` | Tên model mà API key được phép truy cập |

File local được `.gitignore` loại trừ. **Không commit file này hoặc đưa key/JWT secret vào frontend.** Connection string mẫu dùng `TrustServerCertificate=True` cho môi trường local; không sao chép tùy chọn này sang production nếu chưa đánh giá chứng chỉ TLS.

Có thể dùng environment variables (`ConnectionStrings__BookNest`, `Jwt__Secret`, `AI_API_KEY`, `AI_MODEL`) hoặc .NET user-secrets. Trong Development, user-secrets được nạp sau cấu hình local/environment; tránh giữ các giá trị xung đột ở nhiều nơi.

> **Chuyển từ MariaDB/MySQL:** migrations SQL Server tạo schema mới, không chuyển dữ liệu hiện có từ MariaDB. Hãy sao lưu và thực hiện export/import dữ liệu riêng nếu cần giữ dữ liệu cũ; không chạy migrations này trên database MySQL.

### 3. Tạo database và tài khoản

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project .\backend\BookNest.Api -- --migrate
```

Lệnh áp dụng migration SQL Server rồi kết thúc. Migration hiện tại tạo schema mới cho SQL Server; nó **không chuyển dữ liệu** từ MariaDB/MySQL hoặc schema cũ. Không chạy migration này trên database MySQL.

Tạo hai tài khoản demo mà không thêm sách hay đơn hàng:

```powershell
dotnet run --project .\backend\BookNest.Api -- --seed-accounts
```

Lệnh này chỉ chạy trong Development, tự áp dụng migration và tạo `admin@booknest.local` cùng `customer@booknest.local` từ mật khẩu trong `Seed:AdminPassword` và `Seed:CustomerPassword`. Seed bỏ qua tài khoản đã tồn tại, không đổi mật khẩu cũ.

Nếu muốn thêm dữ liệu trình diễn (danh mục, sách và đơn mẫu), chỉ chạy trên database demo:

```powershell
dotnet run --project .\backend\BookNest.Api -- --seed
```

`--seed` cũng tạo hai tài khoản demo, cùng 5 danh mục, 25 sách minh họa và 5 đơn mẫu; các đơn này có trừ tồn kho. Cả hai chế độ seed đều chỉ chạy trong Development, không xóa dữ liệu và không thay mật khẩu tài khoản đã có.

> Database có dữ liệu hoặc schema từ nguồn khác: sao lưu và kiểm tra tương thích trước khi áp dụng migration. Không reset/drop database để nâng cấp. Migration SQL Server mới không phải migration chuyển đổi dữ liệu từ MariaDB/MySQL.

### 4. Chạy backend

Terminal thứ nhất, tại thư mục gốc:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project .\backend\BookNest.Api --urls http://localhost:5080
```

| Dịch vụ | Địa chỉ local |
| --- | --- |
| API | `http://localhost:5080/api` |
| Health check | `http://localhost:5080/api/health` |
| Swagger (Development) | `http://localhost:5080/swagger` |

### 5. Chạy frontend

Terminal thứ hai, tại thư mục gốc:

```powershell
Set-Location .\frontend
npm run dev
```

Mở **`http://localhost:5173`**. Vite proxy `/api`, `/uploads`, `/images` sang backend cổng 5080. Đổi cổng backend cần sửa proxy tương ứng trong cấu hình Vite.

### Tài khoản demo

Được tạo bằng `--seed-accounts` hoặc `--seed`. Mật khẩu là giá trị local bạn cấu hình; xem `Seed:AdminPassword` và `Seed:CustomerPassword` trong `backend/BookNest.Api/appsettings.Local.json`:

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Admin | `admin@booknest.local` | `Seed.AdminPassword` khi tạo tài khoản |
| Customer | `customer@booknest.local` | `Seed.CustomerPassword` khi tạo tài khoản |

Không commit file local hoặc công khai mật khẩu. Sửa cấu hình seed không đổi mật khẩu tài khoản đã tồn tại; sau khi đổi mật khẩu trong hồ sơ, dùng mật khẩu mới.

## Luồng đơn hàng

1. Khách thêm sách vào giỏ → checkout → đăng nhập nếu cần → quay lại checkout.
2. Backend kiểm tra giá/tồn, tạo **Pending + Unpaid** và trừ tồn trong transaction.
3. Customer mô phỏng thanh toán; có thể thử lại khi thất bại.
4. Với đơn Paid, Admin chuyển **Pending → Shipping → Completed**.
5. Đơn Pending chưa Paid có thể hủy; hoàn tồn theo quy tắc và không hoàn trùng.
6. Customer đủ điều kiện đánh giá sau khi hoàn thành mua. Dashboard tính doanh thu từ đơn **Completed + Paid**.

## API chính

Swagger cung cấp request/response và chức năng thử API. Các nhóm endpoint tiêu biểu:

| Nhóm | Endpoint |
| --- | --- |
| Xác thực | `/api/auth/register`, `/api/auth/login`, `/api/auth/me` |
| Catalog | `/api/products`, `/api/categories` |
| Đơn hàng | `/api/orders` |
| Hồ sơ | `GET /api/account/profile`, `PUT /api/account/profile` |
| Đổi mật khẩu | `POST /api/account/change-password` |
| Người dùng Admin | `GET /api/admin/users`, `GET /api/admin/users/{id}` |
| Trạng thái/vai trò | `PATCH /api/admin/users/{id}/status`, `PATCH /api/admin/users/{id}/role` |
| Quản trị khác | `/api/admin/dashboard`, `/api/admin/products`, `/api/admin/categories`, `/api/admin/orders` |

API quản trị yêu cầu Admin và phiên xác thực hợp lệ trong database. API hồ sơ lấy UserId từ danh tính xác thực, không nhận UserId tùy ý. Response không trả PasswordHash hoặc phiên bản xác thực nội bộ.

## Cấu hình Gemini — tùy chọn

1. Đặt `AI_API_KEY` ở backend qua file local, environment hoặc user-secrets.
2. Lấy danh sách model được tài khoản cho phép:

   ```powershell
   .\scripts\List-AiModels.ps1
   ```

3. Điền đúng tên model hỗ trợ `generateContent` vào `AI_MODEL`, rồi khởi động lại backend.

Không đặt key trong biến `VITE_*`, source frontend hoặc repository. Context AI chỉ dùng catalog, không gửi hồ sơ, email, địa chỉ hoặc lịch sử đơn hàng. **Chưa xác minh cuộc gọi Gemini thật bằng API key trong đợt kiểm thử được ghi nhận.** Nội dung AI cần được người dùng kiểm tra lại.

## Build và kiểm thử

### Build

Dừng API trước khi build trên Windows nếu gặp lỗi khóa DLL/executable.

```powershell
dotnet build .\BookNest.sln
Push-Location .\frontend
npm run type-check
npm run build
Pop-Location
```

### Kiểm thử API với SQL Server riêng

```powershell
.\scripts\Test-SqlServer.ps1
```

Script đọc kết nối local, tạo database `booknest_test_<GUID>` riêng cho từng lượt, áp dụng migrations và chạy xUnit. Test host từ chối tên không đúng prefix. Script yêu cầu kết nối local chứa `Database=booknest_db;`; Windows account cần quyền tạo database test.

- Không dùng EF InMemory thay cho kiểm thử transaction SQL Server.
- Không khóa/đổi quyền/đổi mật khẩu tài khoản thật để kiểm thử.
- Database test được giữ lại, không tự DROP; chỉ dọn thủ công sau khi xác nhận đúng tên.
- Báo cáo TRX: `TestResults/business.trx`.
- Không xem `dotnet test` thiếu cấu hình database riêng là một lượt kiểm thử SQL Server thành công.

### Kiểm thử trình duyệt

Script hiện dùng Playwright trong `.local` và Chrome tại `C:\Program Files\Google\Chrome\Application\chrome.exe`. Cài dependency riêng, không cần thêm vào frontend:

```powershell
npm install --prefix .\.local playwright@1.56.1 --no-audit --no-fund
```

Với frontend/backend đang chạy, các lượt hồi quy sau không tạo đơn hoặc sửa dữ liệu server:

```powershell
node .\scripts\auth-regression.cjs
node .\scripts\ui-regression.cjs
node .\scripts\catalog-regression.cjs
node .\scripts\catalog-regression.cjs --fixtures
```

`ui-regression.cjs` dùng tài khoản demo đã seed và mật khẩu trong cấu hình local. Auth regression dùng fixture lỗi/loading; catalog `--fixtures` chặn dữ liệu ở trình duyệt, không phải xác nhận API thật.

Để kiểm tra hồ sơ, người dùng và đổi mật khẩu qua trình duyệt với **database thử nghiệm thật**, để Vite chạy tại cổng 5173 rồi chạy:

```powershell
$env:BOOKNEST_BROWSER_TEST = '1'
try {
    .\scripts\Test-SqlServer.ps1
} finally {
    Remove-Item Env:BOOKNEST_BROWSER_TEST -ErrorAction SilentlyContinue
}
```

Harness tạo tài khoản test, chạy API riêng tại cổng **5081**, gọi `account-regression.cjs`, rồi dừng API thử nghiệm. Cổng 5081 phải trống. Không gọi trực tiếp script này bằng token tài khoản thật.

> **Cảnh báo:** `scripts/browser-smoke.cjs` cũ có tạo đơn, mô phỏng thanh toán và thay đổi tồn kho. Không dùng cho kiểm thử read-only hoặc trên database đang sử dụng thực tế.

### Kết quả đã ghi nhận

- **26/26 test qua** là kết quả lịch sử trên MariaDB trước khi chuyển provider; chưa xác nhận trên SQL Server. Cần chạy `.\scripts\Test-SqlServer.ps1` để kiểm tra tích hợp trên SQL Server.
- Backend build, frontend type-check và production build thành công.
- Kiểm tra phân quyền/ownership, chặn trường đặc quyền, lọc/phân trang, audit và tổng chi tiêu.
- Kiểm tra đồng thời thao tác Admin, thu hồi JWT sau khóa/đổi quyền/đổi mật khẩu, mở khóa không khôi phục token cũ.
- Kiểm tra header cập nhật sau lưu hồ sơ, checkout giữ dữ liệu, đổi mật khẩu giữ giỏ và rate limit trả 429.
- Hồ sơ/quản lý người dùng ở 1440/768/390px; bảng cuộn độc lập, không tràn ngang toàn trang.
- Hồi quy auth, storefront/Admin và catalog qua các viewport tương ứng; fixture/API thật được ghi riêng trong báo cáo.

Chi tiết trong `docs/account-management.md`, `docs/ui-refresh.md` và `docs/screenshots/`. Kết quả này không thay thế audit bảo mật hoặc khả năng truy cập đầy đủ.

## Bảo mật và giới hạn

- Mật khẩu băm bằng PasswordHasher; DTO tách riêng; backend kiểm tra quyền, ownership và whitelist vai trò.
- Audit quyền/trạng thái không chứa mật khẩu, hash hoặc token. Không bật HTTP body logging chứa mật khẩu hoặc EF sensitive-data logging.
- Access token trong Pinia/sessionStorage vẫn có rủi ro khi XSS. Chưa có refresh token hoặc session bằng HttpOnly cookie.
- Checkout lưu payload chờ xác nhận trong sessionStorage để retry an toàn, có thông tin người nhận. Không lưu mật khẩu trong URL/storage.
- Chưa có xác minh email, quên mật khẩu, đổi email, upload avatar, social login, mã giảm giá hoặc wishlist.
- Chưa có thanh toán/vận chuyển/hoàn tiền thật, tự hết hạn đơn Pending, tìm kiếm full-text hoặc vector database.
- Chưa kiểm thử thiết bị di động vật lý, password manager thực tế, screen reader đầy đủ, pen-test hoặc tải đa API instance.
- Upload local dùng SkiaSharp; Linux cần kiểm tra native assets tương ứng. Cần backup database và thư mục upload khi triển khai.
- Production cần HTTPS, secrets an toàn, DB user quyền tối thiểu, cấu hình host/CORS và reverse proxy. Build frontend không có Vite dev proxy: cần phục vụ `/api`, `/uploads`, `/images` qua reverse proxy, hoặc cấu hình API public và đường dẫn ảnh phù hợp. `VITE_API_BASE_URL` chỉ chứa URL public, không chứa secrets.

## Tài liệu bổ sung

| File | Nội dung |
| --- | --- |
| `docs/account-management.md` | Hồ sơ/người dùng, JWT revocation, migration và kiểm thử |
| `docs/ui-refresh.md` | Nâng cấp UI và bằng chứng responsive |
| `docs/database.md` | Thiết kế database |
| `docs/order-rules.md` | Đơn hàng, thanh toán và tồn kho |
| `docs/api.md` | Ghi chú API; đối chiếu Swagger khi chạy |
| `docs/technology.md` | Ghi chú công nghệ |
| `docs/acceptance-checklist.md` | Checklist nghiệm thu |

Một số tài liệu lịch sử mô tả theo từng mốc phát triển; phần hồ sơ/người dùng mới được ghi riêng trong `docs/account-management.md`.

## Trước khi đẩy lên GitHub

- Kiểm tra `git status` và nội dung staged trước khi push.
- Không đưa `appsettings.Local.json`, API key, connection string có mật khẩu hoặc token vào Git, kể cả ảnh chụp và lịch sử commit.
- `.gitignore` đã loại trừ cấu hình local, `.local`, log, `node_modules`, build output, `TestResults` và thư mục upload.
- Rà lại ảnh/tài liệu muốn công khai; chỉ dùng dữ liệu demo/thử nghiệm.
- Nếu secret từng được commit, cần thu hồi/thay secret và xử lý lịch sử Git; thêm `.gitignore` không xóa được secret khỏi lịch sử.
