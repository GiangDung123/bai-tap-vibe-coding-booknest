# Danh sách API

Base Development `http://localhost:5080/api`, Swagger tại `http://localhost:5080/swagger`. JSON camelCase. JWT Bearer. Body không nhận entity/navigation. Lỗi ProblemDetails, validation errors theo field. Trang `items,page,pageSize,totalItems,totalPages`; pageSize được clamp 1..50, page tối thiểu 1.

## Auth

| Method | Path | Body / quyền |
|---|---|---|
| POST | /auth/register | fullName,email,password,confirmPassword; public, chỉ tạo Customer |
| POST | /auth/login | email,password; public |
| GET | /auth/me | JWT |

Register trả 201 `{token,expiresAt,user}`; login 200 cùng cấu trúc. `user` chỉ id/fullName/email/role. Token 120 phút. Login/register rate limit 12/phút/IP. Email trim + uppercase cho kiểm tra unique.

## Public catalog

| GET | Query / response |
|---|---|
| /categories | Danh mục hoạt động, mảng |
| /products | search,categoryId,minPrice,maxPrice,sort(newest/priceAsc/priceDesc),featured,page,pageSize; server pagination |
| /products/{id} | Thông tin sách, ảnh, điểm trung bình, reviewCount |
| /products/{id}/reviews | page,pageSize |

Chi tiết sách đã ẩn vẫn trả metadata `isActive=false` để giỏ cũ đối chiếu, không thể đặt mua. Listing loại sản phẩm/danh mục ẩn. ProductView không trả entity; admin nhận thêm Version qua cùng DTO.

## Đơn và đánh giá

Tất cả cần JWT; `/orders/{id}` luôn owner, admin dùng namespace riêng.

| Method | Path | Body / chi tiết |
|---|---|---|
| POST | /orders | recipientName,recipientPhone,shippingAddress,note,idempotencyKey,items:[{productId,quantity}] |
| GET | /orders/my | page,pageSize |
| GET | /orders/{id} | Chỉ chủ đơn; snapshot giá/tên/SKU |
| POST | /orders/{id}/cancel | Không body; chỉ Pending chưa Paid |
| POST | /orders/{id}/simulate-payment | `{success:true/false}`; không nhận amount |
| POST | /products/{id}/reviews | `{rating,comment}`; Customer có Completed chứa sản phẩm |
| PUT | /products/{id}/reviews/me | `{rating,comment}`; sửa review chính mình |

Create order trả 201 và Location. Retry cùng key + nội dung canonical trả cùng order; khác nội dung 409. Chưa có API public sửa order price/status. Không có API thay role.

## Admin (role Admin)

| Method | Path | Query / body |
|---|---|---|
| GET | /admin/categories | search,page,pageSize |
| POST | /admin/categories | name,slug,description,isActive |
| PUT | /admin/categories/{id} | Như POST |
| DELETE | /admin/categories/{id} | Ẩn, không hard delete |
| GET | /admin/products | search,categoryId,isActive,page,pageSize |
| GET | /admin/products/{id} | DTO chi tiết kèm version |
| POST | /admin/products | name,slug,sku,categoryId,author,publisher,description,price,stockQuantity,isActive,isFeatured,version,images:[url] |
| PUT | /admin/products/{id} | Như POST, version bắt buộc đúng bản đang sửa |
| DELETE | /admin/products/{id} | Ngừng bán, tăng version |
| POST | /admin/uploads/images | multipart/form-data, field file, ≤5 MB; JPEG/PNG/WebP giải mã thật |
| GET | /admin/orders | search,orderStatus,paymentStatus,from,to,page,pageSize |
| GET | /admin/orders/{id} | Chi tiết toàn hệ thống |
| PATCH | /admin/orders/{id}/status | `{status:"Shipping"/"Completed"/"Cancelled"}` |
| GET | /admin/dashboard | from,to theo yyyy-MM-dd, tối đa 366 ngày |

Upload trả `{imageUrl}` local server, luôn tên ngẫu nhiên PNG sau re-encode, không lưu SVG/executable/client path. Ảnh tối đa 20MP. Cập nhật sản phẩm chỉ nhận URL upload hợp lệ hoặc bìa demo local.

## AI

POST `/chat`, body `{message}` (2..1000 ký tự), public, 8/phút/IP. Thành công `{answer,products}` đã xác minh ID; lỗi/missing config 503 “Trợ lý hiện chưa sẵn sàng”. Không trả lỗi/key upstream; không gọi tools đặt hàng/database write.

## Status

- 200/201/204 thành công; 400 validation; 401 token sai/hết hạn; 403 role/điều kiện đánh giá; 404 resource/đơn không thuộc user; 409 unique/concurrency/tồn/state/idempotency conflict; 413 request quá lớn; 429 rate limit; 503 AI unavailable; 500 thông báo chung không stacktrace.
- HTTP request không tin cậy chỉ được bind DTO; các field dư như Role, totalAmount, PaidAt không được dùng.