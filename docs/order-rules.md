# Đơn hàng, thanh toán và concurrency

Nguồn nghiệp vụ: `D:\Vibe-coding\Baitap\backend\BookNest.Api\Services\OrderService.cs`.

## Tạo đơn

1. UserId chỉ đọc từ JWT. Validation nhận giỏ 1..100 dòng, product ID > 0, lượng nguyên 1..1000; gộp ID và sort tăng dần. Cấm lượng gộp >1000.
2. Canonical fingerprint gồm người nhận/phone/address/note đã trim + các dòng đã gộp/sort. SHA256 ở đây **chỉ hash request**, không phải hash password.
3. Tra đơn theo user+key, nếu đã có so hash: cùng trả lại đơn, khác 409.
4. Transaction explicit **ReadCommitted**. Insert Order để claim unique `(UserId,IdempotencyKey)` **trước** thay đổi tồn. Request retry đồng thời chờ unique index; sau commit nhận duplicate và tải lại order. Nếu rollback request đầu, request sau có thể insert tiếp.
5. Theo ProductId tăng dần: đọc category với `LOCK IN SHARE MODE`, kiểm tra hoạt động; đọc product `FOR UPDATE`, kiểm tra CategoryId không đổi, active và đủ tồn. Không đơn thuần đọc stock rồi ghi thiếu bảo vệ.
6. Conditional UPDATE kiểm tra ID + Version + Stock >= qty + Active, trừ Stock, tăng Version. Mỗi dòng phải update đúng 1 record. Category lock ngăn thao tác ẩn category cắt ngang checkout.
7. Lấy giá/name/SKU từ bản product đã khóa, ghi snapshot OrderItems, tính decimal tổng server; ShippingFee=0.
8. Save order/items và commit; exception dispose transaction rollback toàn bộ, gồm cả claim key và các stock trước. Deadlock/lock timeout báo 409, retry nguyên request/cùng key.

Khóa sản phẩm theo ID giúp giảm deadlock; vẫn phải chấp nhận/rollback deadlock và retry chứ không tuyên bố mọi lịch xen kẽ đều không deadlock. Các update Admin tương tác qua version/row locks.

## Trạng thái tách biệt

```text
Tạo: Pending + Unpaid, trừ kho
Thanh toán giả lập: Pending + (Unpaid hoặc Failed) → Pending + (Paid hoặc Failed)
Paid: set PaidAt UTC, không thanh toán lại, không hủy
Pending + Paid → Shipping + Paid → Completed + Paid (set CompletedAt UTC)
Pending + chưa Paid → Cancelled (set CancelledAt UTC, hoàn tồn)
```

Completed/Cancelled cuối. Không chuyển ngược. Không hoàn tiền. Không nhận amount từ client. Chỉ owner được thanh toán; Admin không có endpoint giả làm khách thanh toán.

## Thanh toán/hủy cạnh tranh

Mỗi thay đổi trạng thái mở transaction, khóa Order `FOR UPDATE` rồi mới kiểm tra state/quyền. Hai thao tác cùng đơn được tuần tự hóa: nếu pay thắng thì cancel gặp Paid → 409; nếu cancel thắng thì pay gặp Cancelled → 409. Hủy lần 2 không qua điều kiện Pending nên không hoàn tồn lần 2. Khi hoàn tồn update `Stock += quantity`, tăng Version, trong cùng transaction.

## Admin sửa tồn

Form giữ Version đã đọc. PUT so Version với DB, EF dùng `IsConcurrencyToken` để đưa version cũ vào WHERE khi SaveChanges, sau đó tăng Version. Dù order thay stock ngay giữa bước so và SaveChanges, UPDATE bị từ chối bằng DbUpdateConcurrencyException → 409. UI yêu cầu tải lại bản mới nhất, không tự retry bằng tồn form cũ.

## Frontend idempotency

Vô hiệu nút khi gửi. Lưu key/payload canonical trong sessionStorage trước gửi; mạng lỗi giữ nguyên. Reload phục hồi thông tin, không tự sinh key mới. Nếu người dùng thay nội dung khi request cũ chưa rõ kết quả, yêu cầu kiểm tra đơn thay vì âm thầm tạo đơn mới. Success mới xóa pending payload và sản phẩm đã đặt khỏi giỏ. Client totals chỉ preview; server không tin.

## Revenue / ngày

Chỉ `Completed && Paid`, nhóm ngày `CompletedAt+7h`. UTC from = VN ngày bắt đầu 00h trừ 7h, end exclusive = VN ngày kết thúc+1 trừ 7h. Điền đầy đủ ngày với 0. Số order kỳ dựa CreatedAt; Customer/activeProducts toàn hệ thống và UI ghi rõ.