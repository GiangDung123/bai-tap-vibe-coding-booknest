# Checklist nghiệm thu / bằng chứng

Ngày thực hiện: **23/10/2026**. Kết quả chỉ bao gồm những lệnh và luồng thực sự đã chạy, không suy ra test transaction từ build.

## Môi trường / an toàn dữ liệu

- [x] Khảo sát workspace trống, không có repo/hướng dẫn/mã nguồn cần ghi đè.
- [x] Kiểm tra node/npm/git/.NET SDK/runtime.
- [x] Đọc cổng `[mysqld]` my.ini: 3306; kết nối SQL xác nhận 10.4.32-MariaDB.
- [x] Kiểm tra `booknest_db` trước thay đổi: tồn tại, không có bảng.
- [x] Không drop/reset database, không thay root/password/server config, không cài thêm MySQL.
- [x] Migrations thật và seed Development; 5 category/25 product/2 user/5 seed order.
- [x] information_schema xác nhận 7 bảng nghiệp vụ + migrations history đều InnoDB.
- [x] JWT/DB/demo password ngoài mã nguồn, không in giá trị secrets.

## Phạm vi đã triển khai

- [x] Auth register/login/me/logout, normalized email unique, Customer-only register, PasswordHasher.
- [x] JWT signature/issuer/audience/expiry, role/owner backend, route guard và safe redirect frontend.
- [x] Home/category/products/detail/related/reviews từ API/database thật.
- [x] Search tên/author/description, filter kết hợp giá/category, sort/page phía server, URL query/debounce/reset.
- [x] Cart khách với ID/quantity trong localStorage; snapshot giá tham chiếu sessionStorage, giá/tồn authoritative luôn API.
- [x] Checkout transaction, unique idempotency claim/hash, stock lock+conditional update, snapshot giá/tên/SKU.
- [x] Payment Paid/Failed retry, owner-only, order/payment status tách riêng, cancellation stock restore.
- [x] Personal orders/detail/pages và Admin categories/products/orders/dashboard.
- [x] Admin ảnh decode/re-encode allowlist 5MB/20MP, soft hide, stock version conflict.
- [x] Review quyền đã hoàn thành mua, unique/user/product, edit mine.
- [x] Chart revenue Completed+Paid theo CompletedAt và VN date range; zero day; system-wide counts ghi rõ.
- [x] Gemini REST integration, model config validation, limited catalog, verified IDs, no AI writes, unavailable handling.
- [x] Responsive layouts/mobile menus, shared components, loading/empty/error/success, labels/focus, VND/VN time.
- [x] README và tài liệu schema/API/order/concurrency.

## Kết quả chạy

| Kiểm tra | Kết quả / bằng chứng |
|---|---|
| API `dotnet build` | Qua, target net8.0 |
| Frontend `npm run type-check` | Qua |
| Frontend `npm run build` | Qua |
| EF migration trên MariaDB demo | Áp dụng InitialBookNest thật |
| EF migration trên MariaDB test | Qua, database riêng `booknest_test_<GUID>` |
| xUnit nghiệp vụ | **19/19 pass, 0 failed, 0 skipped**, TRX tại `D:\Vibe-coding\Baitap\TestResults\business.trx` |
| Chromium headless / desktop | Home, cart, login redirect, checkout, Failed → retry Paid, Admin dashboard; không pageerror |
| Chromium headless / mobile 390×844 | Home/products/cart/login không tràn ngang; menu mở và điều hướng hoạt động |
| Screenshots | `D:\Vibe-coding\Baitap\docs\screenshots` |
| Catalog HTTP | API trả 25 sản phẩm; Vite phục vụ trang thật |

19 test methods bao phủ nhiều quy tắc (không phải mỗi method chỉ một assertion):

1. QuantityAndFingerprintAreCanonical — gộp/sort/hash, số lượng âm.
2. VietnamRangeConvertsToUtc — ngày VN sang UTC.
3. DuplicateEmailAndAdminAuthorization — duplicate normalized email, Customer bị 403 Admin.
4. OwnershipAndSnapshotAndServerPrice — A/B order isolation, bỏ giá client, giữ snapshot sau đổi giá/ngừng bán.
5. InvalidQuantityAndTransactionRollback — qty 0, dòng sau lỗi không trừ một phần.
6. LastCopyHasOnlyOneBuyer — 2 request cạnh tranh tồn 1, đúng 1 thành công.
7. ConcurrentRetryClaimsKeyOnlyOnce — retry đồng thời cùng key, cùng order, trừ kho một lần; khác payload 409.
8. PaidCannotBePaidAgainOrCancelled — Paid không trả tiền lại, không cancel.
9. CancelRestoresStockExactlyOnce — 2 cancel đồng thời, đúng 1 hoàn tồn.
10. PaymentAndCancellationCannotBothWin — race pay/cancel đúng 1 success.
11. OptimisticVersionDetectsOrderStockChange — EF version phát hiện form tồn cũ.
12. InvalidTransitionsAndRevenueAndReviewEligibility — transitions sai, chỉ completed purchase review, unique review, doanh thu thật.
13. CombinedFilteringAndPagination — search+category+price+page kết hợp và invalid range.
14. MissingAiKeyIsSafeAndCatalogStillWorks — missing key 503, catalog vẫn 200.
15. AdminStockApiRejectsStaleVersionAndPrivilegedPayment — HTTP PUT stock stale 409, không cho Admin patch Paid.
16. UploadDecodesContentAndRejectsDisguisedSvg — SVG giả extension bị chặn, PNG thật trả server-generated URL.
17. HiddenCategoryCannotBePurchased — hidden category không mua, không đổi tồn.
18. ExpiredAndForgedJwtAreRejected — token hết hạn/chữ ký sai bị 401.
19. AiProviderErrorIsSanitized — provider failure handler mô phỏng trả 503 an toàn.

Test host tắt quota rate limiter trong **chính test host** để nhiều fixture không tranh cùng IP quota; không tắt trên API demo. Không kết luận rate limiter chịu tải cao từ các test trên.

## Chưa xác minh / giới hạn còn lại

- [ ] **Gemini live call** với API key thật + model được tài khoản cấp quyền: bị chặn do chưa có key. Đã có script models.list; không dùng response cố định để nhận đã tích hợp thành công.
- [ ] Live upstream timeout/quota/safety filtering/prompt-injection audit: mới test handler lỗi mô phỏng, không phải provider live.
- [ ] Mobile/tablet mọi kích thước, bàn phím/screen reader/WCAG audit đầy đủ: đã smoke desktop và 390px, không phải chứng nhận toàn bộ accessibility.
- [ ] Load test dài hạn, pen-test, triển khai public/HTTPS, backup/monitoring, refresh/revoke JWT, Linux native assets: ngoài bằng chứng hiện tại.
- [ ] Kiểm thử UI tất cả nhánh CRUD/validation/ảnh browser: test HTTP có upload và version, browser hiện là luồng mua chính + dashboard.

Không triển khai chức năng ngoài phạm vi: payment thật, refund, ship thật, coupon, wishlist, social login, microservices. Việc thiếu những mục này là giới hạn có chủ đích, không phải nút giả trong UI.

**Tác động browser smoke:** đã tạo thêm đơn Paid Pending cho demo Customer trong `booknest_db`, giảm tồn tương ứng. Các test cạnh tranh/rollback chỉ chạy database test riêng, database test được giữ để kiểm tra, không tự drop.