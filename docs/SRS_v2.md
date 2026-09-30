# Đặc tả yêu cầu phần mềm (SRS) — KiotViet Tool

| | |
|---|---|
| Phiên bản | **0.2.1** (bản bổ sung lần 2, thay thế 0.1) |
| Ngày | 30/09/2026 |
| Trạng thái | Chờ chủ sản phẩm duyệt; các mục đánh dấu **⚠️ Cần chốt** phải được trả lời trước khi triển khai phần liên quan |
| Người đọc | Chủ cửa hàng (duyệt nghiệp vụ), developer / AI (triển khai), người kiểm thử |
| Tài liệu đi kèm | *Tài liệu nghiệp vụ dành cho khách hàng* (bản rút gọn, không có chi tiết kỹ thuật) |

### Lịch sử thay đổi

| Phiên bản | Ngày | Nội dung thay đổi |
|---|---|---|
| 0.1 | 28/09/2026 | Bản nháp đầu tiên |
| 0.2 | 29/09/2026 | Kiểm chứng lại tài liệu KiotViet: xác nhận A2 (API cập nhật chi tiết bảng giá), A3/A5 **không có** trong tài liệu → đổi thiết kế triển khai: bảng giá do chủ cửa hàng tạo, tool đọc thời gian từ bảng giá. Bổ sung: trạng thái *Đang triển khai*, *Cần triển khai lại*; khôi phục sau khi tool tắt giữa chừng; kiểm tra giờ máy; xử lý đơn vị tính / hàng cùng loại / combo / dịch vụ; đồng bộ sản phẩm bị xoá; phạm vi chi nhánh của bảng giá; khoá đăng nhập sai nhiều lần và khôi phục mật khẩu; sao lưu/khôi phục; danh mục màn hình; danh mục thông báo lỗi; kịch bản nghiệm thu; câu hỏi mở Q9–Q13 |
| 0.2.1 | 30/09/2026 | Chốt Q8: đã gỡ feature *Quản lý khách hàng* (giao diện, backend, bảng `Customers` qua migration `RemoveCustomers`). Trang chủ sau đăng nhập tạm thời là màn Sản phẩm (SC-05) |

Các mục mới hoặc thay đổi so với 0.1 được đánh dấu **[Mới v0.2]** hoặc **[Sửa v0.2]**.

---

## 1. Giới thiệu

### 1.1 Mục đích
Tài liệu mô tả đầy đủ nghiệp vụ và yêu cầu của **KiotViet Tool**: một ứng dụng desktop Windows giúp chủ cửa hàng đang dùng phần mềm bán hàng **KiotViet** thiết lập **chương trình giảm giá có thời hạn** (theo % hoặc theo số tiền VND) và đưa giá đã giảm lên KiotViet qua **KiotViet Public API**, để màn hình bán hàng của KiotViet áp dụng.

### 1.2 Phạm vi
**Trong phạm vi**
- Đăng nhập tool bằng một tài khoản quản trị duy nhất (đã có).
- Kết nối tool với một cửa hàng KiotViet qua Public API.
- Đồng bộ danh mục nhóm hàng, sản phẩm, bảng giá từ KiotViet về máy.
- Tạo, xem trước, triển khai, dừng chương trình giảm giá theo % hoặc VND, có ngày giờ bắt đầu và kết thúc.
- Theo dõi kết quả triển khai, đối soát giá trên KiotViet, nhật ký thao tác.
- **[Mới v0.2]** Sao lưu / khôi phục dữ liệu của tool.

**Ngoài phạm vi** (xem chi tiết mục 12)
- Can thiệp trực tiếp vào màn hình bán hàng của KiotViet.
- Sửa hoá đơn sau khi đã thanh toán, hoặc ghi nhận doanh thu khác với số tiền thực thu.
- Khuyến mại theo hoá đơn (mua X tặng Y, giảm theo tổng đơn, voucher).

### 1.3 Nguyên tắc nghiệp vụ cốt lõi
> Khách hàng **được giảm giá thật** và **trả đúng số tiền đã giảm**. Đơn giá trên màn hình bán hàng, số tiền khách cần trả, số tiền khách thanh toán và doanh thu KiotViet ghi nhận **luôn khớp nhau**. Thu ngân **không phải và không được** sửa tay giá; giá giảm được áp **tự động** theo chương trình trong khoảng thời gian đã đặt.

### 1.4 Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| Chủ cửa hàng / Admin | Người dùng duy nhất của tool, là chủ cửa hàng dùng KiotViet |
| Giá gốc | Giá bán chung (`basePrice`) của sản phẩm trên KiotViet tại thời điểm triển khai |
| Giá giảm | Giá sau khi áp chương trình giảm giá và làm tròn |
| Chương trình giảm giá (CTGG) | Một cấu hình giảm giá: loại, giá trị, thời gian, phạm vi sản phẩm |
| Bảng giá (Price book) | Tính năng của KiotViet: danh sách giá riêng cho sản phẩm, có hiệu lực theo `startDate`–`endDate`, có thể giới hạn theo chi nhánh / nhóm khách / người dùng |
| Bảng giá đích | Bảng giá trên KiotViet dành riêng cho một CTGG; tool ghi giá giảm vào đây |
| Triển khai | Tool ghi giá giảm của một CTGG lên bảng giá đích trên KiotViet |
| Trung hoà | **[Mới v0.2]** Ghi lại giá trong bảng giá đích = giá gốc hiện tại, để bảng giá còn hiệu lực cũng không giảm giá |
| Retailer | Tên gian hàng KiotViet (phần tên miền `<retailer>.kiotviet.vn`) |
| Client ID / Client Secret | Thông tin kết nối API lấy từ KiotViet: *Thiết lập cửa hàng → Thiết lập kết nối API* |
| Hàng đơn vị tính | **[Mới v0.2]** Một mặt hàng có nhiều đơn vị (lon/thùng). Trên KiotViet mỗi đơn vị là một sản phẩm riêng (`id`, `basePrice` riêng), liên kết qua `masterUnitId`, `conversionValue` |
| Hàng cùng loại (biến thể) | **[Mới v0.2]** Các sản phẩm khác nhau về thuộc tính (size/màu), liên kết qua `masterProductId` |

### 1.5 Tài liệu tham chiếu
- [Hướng dẫn sử dụng Public API Ngành Bán lẻ — KiotViet](https://www.kiotviet.vn/huong-dan-su-dung-public-api-retail/)
- [KiotViet Public API](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ket-noi-api/public-api/)
- `CLAUDE.md`, `README.md` trong repo (kiến trúc, quy tắc code)

---

## 2. Mô tả tổng quan

### 2.1 Bối cảnh sản phẩm
```
┌──────────────────────────┐    HTTPS (OAuth2)     ┌───────────────────────┐
│ KiotViet Tool (.exe)      │ ───────────────────▶ │ KiotViet Public API    │
│ máy Windows của chủ CH    │ ◀─────────────────── │ public.kiotapi.com     │
│ SQLite cục bộ (app.db)    │                       └──────────┬────────────┘
└──────────────────────────┘                                   │ dữ liệu dùng chung
                                                   ┌───────────▼────────────┐
                                                   │ Màn hình bán hàng       │
                                                   │ KiotViet (thu ngân)     │
                                                   │ áp bảng giá theo ngày   │
                                                   └─────────────────────────┘
```
Tool **không** chạy trên máy bán hàng và **không** tương tác với màn hình bán hàng. Tool chỉ **chuẩn bị dữ liệu giá** trên KiotViet trước; KiotViet áp dụng khi bán.

### 2.2 Chiến lược kỹ thuật được chọn: dùng **Bảng giá** của KiotViet
| Phương án | Mô tả | Đánh giá |
|---|---|---|
| **A. Bảng giá (chọn)** | Tool tính giá giảm, ghi vào một bảng giá KiotViet có `startDate`/`endDate` đúng thời gian CTGG | KiotViet **tự bật/tắt theo ngày**; tool **không cần mở** lúc bắt đầu/kết thúc; **giá gốc không bị động tới** |
| B. Sửa giá gốc | Tool đổi `basePrice` lúc bắt đầu, đổi lại lúc kết thúc | ❌ Loại: nếu máy tắt đúng lúc kết thúc, **giá giảm bị giữ mãi**; có nguy cơ mất giá gốc khi lỗi giữa chừng |
| C. API khuyến mại | Tạo chương trình khuyến mại qua API | ❌ Không có API khuyến mại trong tài liệu Public API |

**[Mới v0.2] Mô hình vận hành đã chọn (do A3, A5 không có API):**
1. Chủ cửa hàng **tạo bảng giá thủ công một lần cho mỗi CTGG** trên KiotViet, đặt ngày giờ bắt đầu/kết thúc và phạm vi áp dụng (chi nhánh…). Tool có màn hình hướng dẫn từng bước.
2. Trong tool, khi chọn bảng giá đích, **thời gian của CTGG được lấy từ bảng giá** (một nguồn sự thật duy nhất, không nhập hai lần nên không thể lệch).
3. Tool chỉ **ghi giá từng sản phẩm** vào bảng giá qua API cập nhật chi tiết bảng giá.
4. Dừng sớm: tool **trung hoà** bảng giá (ghi giá = giá gốc) ngay lập tức, rồi hướng dẫn chủ cửa hàng tắt/đổi ngày kết thúc bảng giá trên KiotViet.
5. Gia hạn: chủ cửa hàng đổi ngày kết thúc trên KiotViet, tool đồng bộ lại và cập nhật CTGG.

Nếu sau khi gọi thử phát hiện có API tạo bảng giá / đặt ngày (A3, A5 đúng), tool sẽ tự làm các bước 1, 4, 5; các yêu cầu có ghi chú *“Nhánh A5”* áp dụng khi đó.

### 2.3 Người dùng
| Vai trò | Mô tả | Quyền |
|---|---|---|
| Admin (chủ cửa hàng) | Người duy nhất dùng tool | Toàn quyền |
| Thu ngân | **Không dùng tool**; bán hàng trên KiotViet như bình thường | — |

### 2.4 Môi trường vận hành
- Windows 10/11 x64, một file `KiotVietTool.exe` tự chứa (không cần cài .NET).
- Cần Internet khi đồng bộ và triển khai; các màn hình xem dữ liệu đã đồng bộ vẫn dùng được khi mất mạng.
- Dữ liệu cục bộ: `%LocalAppData%\KiotVietTool\app.db`; log: `%LocalAppData%\KiotVietTool\logs\`.
- **[Mới v0.2]** Gian hàng KiotViet phải dùng gói dịch vụ **có Public API** (theo trang giá của KiotViet, Public API thuộc gói Cao cấp) — xem Q13.
- **[Mới v0.2]** Mỗi máy chỉ chạy **một** phiên bản tool cùng lúc (khoá single-instance); mở lần hai thì đưa cửa sổ đang chạy lên trước.

### 2.5 Ràng buộc
- **C1.** Chỉ dùng KiotViet Public API chính thức (OAuth2 `client_credentials`, scope `PublicApi.Access`).
- **C2.** Giới hạn API: hàm GET tối đa **5.000 request/giờ** (theo tài liệu KiotViet); tool phải tự điều tiết. **[Mới v0.2]** Tài liệu không nêu giới hạn cho POST/PUT → áp dụng mức thận trọng mặc định (xem FR-KV-04) và điều chỉnh sau khi gọi thử.
- **C3.** Mỗi lần gọi danh sách tối đa **100 bản ghi** (`pageSize` ≤ 100, phân trang bằng `currentItem`).
- **C4.** Một tool kết nối **một** gian hàng (một Retailer) tại một thời điểm.
- **C5.** Múi giờ nghiệp vụ: **Asia/Ho_Chi_Minh (UTC+7)**, **không phụ thuộc** múi giờ cài trên Windows. **[Sửa v0.2]**
- **C6.** **[Mới v0.2]** Token KiotViet có hạn 86.400 giây (24 giờ) theo ví dụ tài liệu; tool không giả định cố định mà đọc `expires_in`.

### 2.6 Giả định và phụ thuộc
| Mã | Giả định | Tình trạng (v0.2) | Nếu sai thì |
|---|---|---|---|
| **A1** | Đọc được danh sách bảng giá và giá từng sản phẩm trong bảng giá | ✅ Có trong tài liệu chính thức: `GET /pricebooks` (kèm tuỳ chọn lấy chi nhánh, nhóm khách, người dùng áp dụng), `GET /pricebooks/{id}`; sản phẩm trả về `priceBooks` | — |
| **A2** | Ghi/cập nhật giá sản phẩm trong bảng giá qua API | ✅ **[Sửa v0.2]** Có trong tài liệu chính thức: *Cập nhật chi tiết bảng giá* `POST /pricebooks/detail`. Còn phải gọi thử để biết cấu trúc body, số dòng tối đa mỗi lần, có tự thêm sản phẩm chưa có trong bảng giá không | Chặn toàn bộ tính năng triển khai |
| **A3** | Tạo mới bảng giá qua API | ❌ **[Sửa v0.2]** Không có trong tài liệu | Đã xử lý: chủ cửa hàng tạo bảng giá thủ công (mục 2.2) |
| **A4** | Màn hình bán hàng **tự áp dụng** bảng giá đang hiệu lực, thu ngân không phải chọn | ⚠️ Chưa xác nhận | Thu ngân phải chọn bảng giá khi bán → cần hướng dẫn vận hành (FR-OPS-01), không tự động hoàn toàn |
| **A5** | Đặt/sửa `startDate`, `endDate`, bật/tắt bảng giá qua API | ❌ **[Sửa v0.2]** Không có trong tài liệu | Đã xử lý: thời gian lấy từ bảng giá; dừng sớm bằng trung hoà (mục 2.2) |
| **A6** | **[Mới v0.2]** Khi bảng giá đang áp dụng mà sản phẩm **không có** trong bảng giá, KiotViet bán theo giá gốc | ⚠️ Chưa xác nhận (có thể phụ thuộc thiết lập bảng giá) | Tool chỉ ghi các sản phẩm thuộc CTGG; phải hướng dẫn cấu hình bảng giá đúng khi tạo |
| **A7** | **[Mới v0.2]** Có thể **gỡ** sản phẩm khỏi bảng giá qua API | ❌ Không thấy trong tài liệu | Loại trừ / dừng một sản phẩm = ghi giá của nó về giá gốc (trung hoà) |
| **A8** | **[Mới v0.2]** Thời gian bảng giá trên KiotViet đặt được tới giờ/phút (không chỉ theo ngày) | ⚠️ Chưa xác nhận | CTGG chỉ đặt theo ngày; giao diện tool ẩn phần giờ |

> **Việc đầu tiên khi bắt đầu triển khai (Giai đoạn 0):** dùng tài khoản thử gọi các API trên, ghi kết quả vào **Phụ lục A** và cập nhật SRS lên v1.0.

---

## 3. Yêu cầu chức năng

Mức ưu tiên: **Must** (bắt buộc cho bản đầu) · **Should** (nên có) · **Could** (có thì tốt).
Mỗi yêu cầu có tiêu chí nghiệm thu (AC) để kiểm thử.

### 3.1 Module AUTH — Đăng nhập
| Mã | Yêu cầu | Ưu tiên |
|---|---|---|
| FR-AUTH-01 | Đăng nhập bằng tên đăng nhập + mật khẩu; sai thì báo chung "Tên đăng nhập hoặc mật khẩu không đúng." | Must ✅ |
| FR-AUTH-02 | Lần chạy đầu tạo tài khoản `admin`/`admin`, **bắt buộc đổi mật khẩu** ở lần đăng nhập đầu | Must ✅ |
| FR-AUTH-03 | Đổi mật khẩu: tối thiểu 8 ký tự, khác mật khẩu cũ, nhập lại khớp | Must ✅ |
| FR-AUTH-04 | Đăng xuất; mọi màn hình khác yêu cầu đã đăng nhập | Must ✅ |
| FR-AUTH-05 | Tự khoá phiên sau N phút không thao tác (N cấu hình, mặc định 30). Khi đang đồng bộ/triển khai thì không khoá giữa chừng | Could |
| FR-AUTH-06 | **[Mới v0.2]** Sai mật khẩu 5 lần liên tiếp → khoá đăng nhập 5 phút, hiện thời gian còn lại; đăng nhập đúng thì đặt lại bộ đếm | Should |
| FR-AUTH-07 | **[Mới v0.2]** Khôi phục mật khẩu: khi đổi mật khẩu lần đầu, tool sinh **mã khôi phục** (hiển thị một lần, khuyên in/ghi lại). Quên mật khẩu → nhập mã khôi phục để đặt mật khẩu mới; mã cũ hết hiệu lực, sinh mã mới (⚠️ Q10) | Should |

### 3.2 Module KV — Kết nối KiotViet
**FR-KV-01 Cấu hình kết nối** (Must)
- Nhập: Retailer, Client ID, Client Secret. Retailer tự bỏ khoảng trắng, chữ hoa; chấp nhận dán cả `abc.kiotviet.vn` và tự tách `abc`. **[Sửa v0.2]**
- Client Secret **được mã hoá** khi lưu (DPAPI, phạm vi người dùng Windows), **không bao giờ** hiện lại dạng rõ, không ghi vào log.
- AC: lưu xong, mở lại màn hình thấy Retailer và Client ID; ô Secret hiển thị "••••••" và chỉ ghi đè khi nhập giá trị mới.

**FR-KV-02 Kiểm tra kết nối** (Must)
- Nút "Kiểm tra kết nối": lấy token, gọi một API đọc nhẹ (ví dụ 1 sản phẩm).
- Kết quả hiển thị: thành công (kèm tên gian hàng/số sản phẩm) hoặc lỗi rõ nghĩa theo **Phụ lục B**.

**FR-KV-03 Quản lý token** (Must)
- Lấy token từ `https://id.kiotviet.vn/connect/token`; lưu trong bộ nhớ, dùng lại tới khi còn ≤ 60 giây trước hạn (`expires_in`).
- Gặp HTTP 401: lấy token mới và **thử lại đúng 1 lần**; vẫn 401 → báo lỗi xác thực, dừng tác vụ.
- Mọi request gửi header `Retailer` và `Authorization: Bearer <token>`.

**FR-KV-04 Điều tiết & thử lại** (Must)
- GET: tự giới hạn ≤ 4.500 request/giờ (chừa biên cho C2), đếm theo cửa sổ trượt 60 phút, **lưu bộ đếm vào DB** để tắt/mở tool không reset. **[Sửa v0.2]**
- POST/PUT: mặc định tối đa 2 request/giây, chạy tuần tự (không song song). **[Mới v0.2]**
- Lỗi tạm thời (429, 5xx, timeout 30 giây): thử lại tối đa 3 lần, chờ tăng dần (1s, 2s, 4s); nếu có header `Retry-After` thì tuân theo.
- Lỗi 4xx khác 401/429: **không thử lại**, ghi nhận lỗi kèm nội dung phản hồi (đã lọc thông tin nhạy cảm).

**FR-KV-05 Trạng thái kết nối** (Should): sidebar hiển thị trạng thái Đã kết nối / Chưa cấu hình / Lỗi, kèm thời điểm đồng bộ gần nhất.

**FR-KV-06 Ngắt kết nối** (Should): xoá cấu hình kết nối và dữ liệu đồng bộ; **không** xoá CTGG và nhật ký. Yêu cầu xác nhận. **[Sửa v0.2]** Không cho ngắt khi còn CTGG *Đã lên lịch* / *Đang chạy* (phải dừng trước), vì sau khi ngắt tool không thể dừng chúng.

**FR-KV-07 Đổi sang gian hàng khác** (Should) **[Mới v0.2]**: nếu nhập Retailer khác với Retailer đã lưu, cảnh báo "Dữ liệu đồng bộ và CTGG của gian hàng cũ sẽ không dùng được với gian hàng mới" và yêu cầu ngắt kết nối (FR-KV-06) trước.

**FR-KV-08 Kiểm tra giờ máy** (Must) **[Mới v0.2]**: mỗi lần gọi API, đọc header `Date` của máy chủ; nếu giờ máy lệch > 2 phút → cảnh báo trên thanh trạng thái "Giờ máy tính đang lệch X phút. Trạng thái chương trình có thể hiển thị sai." Trạng thái CTGG (mục 5) tính theo **giờ máy chủ ước lượng** = giờ máy + độ lệch đo được.

### 3.3 Module SYNC — Đồng bộ danh mục
**FR-SYNC-01 Đồng bộ nhóm hàng** (Must): lấy toàn bộ nhóm hàng (cây cha–con, tối đa 3 cấp theo KiotViet) về máy; nhóm bị xoá trên KiotViet (`removedIds`) thì xoá khỏi máy.

**FR-SYNC-02 Đồng bộ sản phẩm** (Must)
- Lưu: `id`, `code`, `name`, `fullName`, nhóm hàng, `basePrice` (giá gốc), đơn vị tính, `masterUnitId`, `conversionValue`, `masterProductId`, loại hàng (`productType`: combo / thường / dịch vụ), `isActive`, `allowsSale`, `modifiedDate`, danh sách bảng giá sản phẩm đang thuộc (kèm giá trong từng bảng giá). **[Sửa v0.2]**
- Lần đầu: đồng bộ toàn bộ (phân trang 100/lần). Các lần sau: **chỉ lấy thay đổi** từ lần đồng bộ trước (`lastModifiedFrom` = mốc lần trước − 5 phút để chừa sai lệch giờ), kèm `includeRemoveIds=true` để nhận danh sách sản phẩm bị xoá. **[Sửa v0.2]**
- Sản phẩm bị xoá trên KiotViet: đánh dấu *Đã xoá* (không xoá cứng, để giữ lịch sử triển khai).
- Có tiến độ (x/y sản phẩm), cho phép huỷ giữa chừng; huỷ thì dữ liệu cũ giữ nguyên (ghi theo giao dịch, chỉ commit khi xong).
- AC: 10.000 sản phẩm đồng bộ lần đầu ≤ 5 phút khi mạng ổn định (100 trang, trong giới hạn C2).

**FR-SYNC-03 Đồng bộ bảng giá** (Must): danh sách bảng giá (tên, `isActive`, ngày bắt đầu/kết thúc, **chi nhánh / nhóm khách / người dùng áp dụng**) và giá từng sản phẩm trong bảng giá. **[Sửa v0.2]**

**FR-SYNC-04 Đồng bộ trước khi triển khai** (Must): trước mỗi lần triển khai, tool **tự đồng bộ lại giá gốc** của các sản phẩm trong phạm vi và **thông tin bảng giá đích** để tính trên dữ liệu mới nhất.

**FR-SYNC-05 Tra cứu sản phẩm** (Should): màn hình danh sách sản phẩm đã đồng bộ, tìm theo mã/tên (không phân biệt dấu), lọc theo nhóm hàng, loại hàng, trạng thái kinh doanh; cột "Đang giảm giá" cho biết sản phẩm thuộc CTGG nào.

**FR-SYNC-06 Đồng bộ tự động** (Should) **[Mới v0.2]**: khi mở tool và đăng nhập, nếu đã cấu hình kết nối → tự đồng bộ thay đổi (chạy nền). Có nút "Đồng bộ ngay" thủ công. Không đồng bộ định kỳ khi tool đang mở (tránh tốn lượt gọi) trừ khi bật trong Cài đặt.

### 3.4 Module PROMO — Chương trình giảm giá
**FR-PROMO-01 Tạo CTGG** (Must). Trường dữ liệu:

| Trường | Bắt buộc | Quy tắc |
|---|---|---|
| Tên chương trình | Có | 1–100 ký tự, không trùng với CTGG chưa kết thúc (không phân biệt hoa thường, bỏ khoảng trắng đầu cuối) |
| Loại giảm | Có | `%` hoặc `VND` |
| Giá trị giảm | Có | `%`: 0 < giá trị < 100, tối đa 2 chữ số thập phân. `VND`: số nguyên > 0, tối đa 1.000.000.000 |
| Bảng giá đích | Có | Chọn từ bảng giá đã đồng bộ; phải thoả BR-12 **[Sửa v0.2]** |
| Bắt đầu / Kết thúc | Có | **[Sửa v0.2]** Lấy tự động từ bảng giá đích (chỉ đọc). *Nhánh A5:* nhập tay, ngày + giờ (đến phút), UTC+7, kết thúc **sau** bắt đầu |
| Phạm vi áp dụng | Có | Toàn bộ sản phẩm / theo nhóm hàng (gồm nhóm con) / theo danh sách sản phẩm (⚠️ Q2) |
| Loại trừ | Không | Danh sách sản phẩm loại khỏi phạm vi |
| Đơn vị tính áp dụng | Có | **[Mới v0.2]** Tất cả đơn vị (mặc định) / chỉ đơn vị cơ bản — xem BR-08 |
| Làm tròn | Có | Mặc định theo Cài đặt (BR-04) |
| Ghi chú | Không | ≤ 500 ký tự |

CTGG mới tạo ở trạng thái **Nháp**; chưa ảnh hưởng gì tới KiotViet.

**FR-PROMO-02 Xem trước giá** (Must)
- Bảng: mã SP, tên SP (fullName), đơn vị, giá gốc, giá giảm, số tiền giảm, % giảm thực tế, cảnh báo.
- Tổng kết: số SP áp dụng, số SP bị loại (kèm lý do theo BR-05, BR-07, BR-08), tổng số tiền giảm nếu mỗi SP bán 1 đơn vị (tham khảo).
- Có tìm kiếm, lọc "chỉ SP bị loại" / "chỉ SP có cảnh báo", xuất Excel (FR-LOG-03).
- AC: không thể triển khai khi chưa mở xem trước ít nhất một lần sau lần sửa gần nhất.
- **[Mới v0.2]** Cảnh báo (không chặn): % giảm thực tế ≥ 50%; giá giảm < giá vốn (nếu đọc được giá vốn — Could).

**FR-PROMO-03 Sửa CTGG** (Must) **[Sửa v0.2]**
- Nháp: sửa mọi trường.
- Đã lên lịch: sửa mọi trường trừ bảng giá đích. Sau khi lưu, CTGG chuyển **Cần triển khai lại**: trên KiotViet **vẫn là giá của lần triển khai trước**; tool hiển thị cảnh báo đỏ và nếu còn < 24 giờ tới giờ bắt đầu thì hiện hộp nhắc mỗi lần mở tool.
- Đang chạy: chỉ sửa **ghi chú**, **loại trừ thêm sản phẩm** (tool trung hoà các SP đó ngay) và thời gian kết thúc (qua UC-05).
- Đã kết thúc / Đã huỷ: chỉ xem.

**FR-PROMO-04 Nhân bản CTGG** (Should): tạo CTGG Nháp mới từ CTGG có sẵn (xoá bảng giá đích và thời gian để chọn lại).

**FR-PROMO-05 Xoá CTGG** (Must): chỉ xoá được CTGG ở trạng thái Nháp (chưa từng triển khai). CTGG đã triển khai thì dùng **Dừng** (FR-DEP-04).

**FR-PROMO-06 Danh sách CTGG** (Must): cột tên, loại/giá trị, thời gian, bảng giá đích, số SP, trạng thái, cảnh báo; lọc theo trạng thái; sắp xếp theo ngày bắt đầu; mặc định ẩn CTGG kết thúc quá 90 ngày.

**FR-PROMO-07 Kiểm tra xung đột** (Must): khi lưu và khi triển khai, phát hiện sản phẩm thuộc **hai CTGG có thời gian chồng lấn** và xử lý theo BR-06.

### 3.5 Module DEP — Triển khai lên KiotViet
**FR-DEP-00 Hướng dẫn tạo bảng giá** (Must) **[Mới v0.2]**
- Màn hình hướng dẫn từng bước tạo bảng giá trên KiotViet cho CTGG: đặt tên theo quy ước `[TOOL] <tên CTGG>`, đặt thời gian, phạm vi chi nhánh (theo Q6), để trống danh sách sản phẩm.
- Nút "Tôi đã tạo xong → Đồng bộ bảng giá" rồi chọn bảng giá vừa tạo.

**FR-DEP-01 Triển khai** (Must)
1. Kiểm tra kết nối, đồng bộ lại giá gốc và bảng giá đích (FR-SYNC-04), kiểm tra BR-12, tính lại giá giảm, kiểm tra xung đột.
2. Hiển thị hộp xác nhận: tên CTGG, thời gian, bảng giá đích, phạm vi chi nhánh, **số sản phẩm sẽ được ghi giá**, số SP bị loại.
3. Chuyển trạng thái **Đang triển khai**; ghi giá giảm của từng sản phẩm vào **bảng giá đích**. Sản phẩm từng thuộc CTGG nhưng nay bị loại/loại trừ → **trung hoà** (ghi giá = giá gốc hiện tại).
4. Lưu **ảnh chụp giá gốc** (giá gốc tại lúc triển khai) cho từng sản phẩm.
5. Đọc lại bảng giá đích để xác minh; chuyển trạng thái CTGG sang **Đã lên lịch** (chưa tới giờ bắt đầu), **Đang chạy**, hoặc **Lỗi triển khai** (có SP lỗi).
- Bắt đầu đã qua lúc triển khai (bảng giá đã hiệu lực) → cho phép, nhưng xác nhận ghi rõ "Giá giảm có hiệu lực **ngay** khi ghi xong từng sản phẩm". **[Mới v0.2]**
- Kết thúc đã qua → không cho triển khai.
- AC: sau khi triển khai, đọc lại bảng giá trên KiotViet thấy đúng giá giảm của 100% sản phẩm thành công.

**FR-DEP-02 Tiến độ & kết quả** (Must): thanh tiến độ (x/y, thời gian ước tính còn lại); bảng kết quả từng sản phẩm (Thành công / Thất bại + lý do). Có nút **Thử lại các sản phẩm lỗi** và **Huỷ** (dừng sau sản phẩm đang ghi; kết quả đã ghi giữ nguyên, CTGG ở *Lỗi triển khai*).

**FR-DEP-03 Triển khai không trùng lặp** (Must): triển khai lại cùng CTGG chỉ ghi các sản phẩm có giá trên KiotViet khác giá cần ghi, không tạo bản ghi trùng; ngắt giữa chừng rồi chạy lại cho kết quả như chạy một lần.

**FR-DEP-04 Dừng CTGG sớm** (Must) **[Sửa v0.2]**
- Xác nhận: "Dừng chương trình? Khách sẽ trả giá gốc ngay khi dừng."
- Tool **trung hoà** toàn bộ sản phẩm trong bảng giá đích (ghi giá = giá gốc hiện tại), chuyển trạng thái **Đã huỷ**.
- Sau đó hiển thị hướng dẫn: "Vào KiotViet → Bảng giá `<tên>` → đặt ngày kết thúc là hôm nay hoặc ngừng áp dụng" (*Nhánh A5*: tool tự làm).
- Nếu trung hoà lỗi một phần → trạng thái vẫn là *Đang dừng — lỗi*, có nút thử lại, cảnh báo đỏ cho tới khi thành công.
- AC: sau khi dừng, bán hàng trên KiotViet ra giá gốc.

**FR-DEP-05 Phát hiện giá gốc thay đổi** (Should): khi đồng bộ, nếu giá gốc của sản phẩm thuộc CTGG Đã lên lịch/Đang chạy khác ảnh chụp lúc triển khai → cảnh báo trên CTGG, gợi ý "Tính lại & triển khai lại". Nếu giá gốc mới **≤ giá giảm đã ghi** → cảnh báo mức cao (khách đang bị bán **cao hơn hoặc bằng** giá gốc).

**FR-DEP-06 Tự kết thúc** (Must): CTGG có thời gian kết thúc đã qua được hiển thị **Đã kết thúc** (tính theo FR-KV-08, không cần gọi API). Việc ngừng áp giá do **KiotViet tự thực hiện** theo ngày của bảng giá.

**FR-DEP-07 Khôi phục sau gián đoạn** (Must) **[Mới v0.2]**: nếu tool bị tắt / mất điện khi CTGG đang ở *Đang triển khai* hoặc đang dừng, lần mở sau tool phát hiện lần chạy dở dang, đánh dấu *Lỗi triển khai* (hoặc *Đang dừng — lỗi*) và mời chạy tiếp. Chạy tiếp tuân theo FR-DEP-03.

**FR-DEP-08 Phát hiện thay đổi bảng giá đích** (Should) **[Mới v0.2]**: khi đồng bộ, nếu bảng giá đích bị xoá, bị tắt, bị đổi thời gian hoặc chi nhánh trên KiotViet → cập nhật thời gian CTGG theo bảng giá (nếu chỉ đổi thời gian) và ghi nhật ký; bị xoá/tắt → cảnh báo trên CTGG.

### 3.6 Module LOG — Nhật ký & đối soát
**FR-LOG-01 Nhật ký thao tác** (Must): ghi lại đăng nhập (kể cả thất bại), đổi mật khẩu, cấu hình kết nối, đồng bộ, tạo/sửa/xoá/triển khai/dừng CTGG, sao lưu/khôi phục: thời gian, thao tác, kết quả, chi tiết. Không ghi mật khẩu/secret/token. Lưu tối thiểu 12 tháng; màn hình xem có lọc theo thời gian, loại thao tác.

**FR-LOG-02 Đối soát giá** (Should): nút "Đối soát" đọc giá hiện tại trên KiotViet của các sản phẩm trong CTGG, so với giá tool đã ghi; liệt kê sản phẩm **bị sửa tay trên KiotViet**, bị gỡ khỏi bảng giá, hoặc có trong bảng giá nhưng không thuộc CTGG. Có nút "Ghi lại giá đúng" cho các dòng lệch.

**FR-LOG-03 Xuất Excel** (Could): xuất danh sách sản phẩm và giá của một CTGG (xem trước hoặc kết quả triển khai).

**FR-LOG-04 Log kỹ thuật** (Must) **[Mới v0.2]**: file log theo ngày, giữ 30 ngày, tối đa 20 MB/file; ghi request/response API ở mức tóm tắt (URL, mã HTTP, thời gian) — không ghi header `Authorization`, không ghi Client Secret.

### 3.7 Module SET — Cài đặt
**FR-SET-01** (Should): cài đặt mặc định cho CTGG mới: kiểu làm tròn (BR-04), giá sàn (BR-05), đơn vị tính áp dụng (BR-08).

**FR-SET-02 Sao lưu / khôi phục** (Should) **[Mới v0.2]**: nút "Sao lưu" tạo bản sao `app.db` (dùng cơ chế backup của SQLite, không cần tắt tool) ra thư mục người dùng chọn, tên kèm ngày giờ. "Khôi phục" chọn file sao lưu → xác nhận → khởi động lại tool. Tự sao lưu hằng ngày khi mở tool, giữ 7 bản gần nhất. Lưu ý: Client Secret mã hoá theo người dùng Windows, khôi phục sang máy/người dùng khác phải nhập lại Secret.

### 3.8 Module OPS — Hỗ trợ vận hành tại quầy **[Mới v0.2]**
**FR-OPS-01 Hướng dẫn thu ngân** (Should): từ một CTGG, xuất trang A4 (PDF/in) cho thu ngân: tên CTGG, thời gian, và — nếu A4 sai — cách chọn bảng giá trên màn hình bán hàng.

**FR-OPS-02 Bảng giá niêm yết** (Could): in danh sách sản phẩm + giá gốc + giá giảm để dán tại quầy/kệ.

### 3.9 Module RPT — Báo cáo **[Mới v0.2]**
**FR-RPT-01 Hiệu quả chương trình** (Could): sau khi CTGG kết thúc, đọc hoá đơn trong thời gian CTGG từ KiotViet, tổng hợp số lượng bán, doanh thu, tổng tiền đã giảm của các SP thuộc CTGG. Chỉ đọc, không sửa hoá đơn. (Có thể để bản sau.)

### 3.10 Tính năng có sẵn cần quyết định
**FR-OLD-01 Quản lý khách hàng (feature mẫu cũ):** không thuộc nghiệp vụ chính. ✅ **Đã gỡ bỏ** ngày 30/09/2026 (Q8): giao diện, backend và bảng `Customers` (migration `RemoveCustomers`).

---

## 4. Quy tắc nghiệp vụ

**BR-01 Công thức giảm theo %**
`Giá giảm = Giá gốc × (1 − p / 100)`, sau đó làm tròn theo BR-04. Tính bằng số thập phân chính xác (`decimal`), không dùng số thực dấu phẩy động.

**BR-02 Công thức giảm theo VND**
`Giá giảm = Giá gốc − v`, sau đó làm tròn theo BR-04.

**BR-03 Thứ tự xử lý**: kiểm tra loại sản phẩm (BR-08) → tính → làm tròn → kiểm tra giá sàn (BR-05) → kiểm tra không tăng giá (BR-07).

**BR-04 Làm tròn** (⚠️ Q4) — các lựa chọn: không làm tròn; làm tròn **xuống** đến 100đ / 500đ / 1.000đ. Mặc định đề xuất: **làm tròn xuống đến 1.000đ** (có lợi cho khách, giá chẵn dễ thu tiền). Tiền VND luôn là số nguyên; "không làm tròn" vẫn cắt phần lẻ dưới 1đ (làm tròn xuống).

**BR-05 Giá sàn**: giá giảm phải **> 0** (và ≥ giá sàn trong Cài đặt nếu có). Sản phẩm vi phạm bị **loại khỏi CTGG** với lý do "Giá sau giảm không hợp lệ", hiển thị trong xem trước.

**BR-06 Chồng lấn chương trình** (⚠️ Q3). Mặc định đề xuất: **một sản phẩm chỉ thuộc tối đa một CTGG trong cùng một khoảng thời gian**. Khi lưu/triển khai CTGG gây chồng lấn: chặn và liệt kê sản phẩm + CTGG xung đột; người dùng loại trừ các sản phẩm đó hoặc chọn bảng giá có thời gian khác. **[Mới v0.2]** Tool cũng cảnh báo nếu sản phẩm đang nằm trong **bảng giá khác không do tool quản lý** có thời gian chồng lấn (KiotViet có thể áp bảng giá kia).

**BR-07 Không tăng giá**: giá giảm phải **nhỏ hơn** giá gốc; nếu làm tròn khiến giá giảm = giá gốc thì loại sản phẩm với lý do "Mức giảm quá nhỏ sau làm tròn".

**BR-08 Loại sản phẩm** **[Sửa v0.2]** (⚠️ Q5 xác nhận):

| Loại | Xử lý mặc định | Lý do hiển thị khi loại |
|---|---|---|
| Ngừng kinh doanh (`isActive = false`) | Loại | "Ngừng kinh doanh" |
| Không bán trực tiếp (`allowsSale = false`) | Loại | "Không bán trực tiếp" |
| Giá gốc = 0 | Loại | "Chưa có giá bán" |
| Hàng combo (`productType = 1`) | Loại | "Hàng combo" |
| Hàng dịch vụ (`productType = 3`) | Loại | "Hàng dịch vụ" |
| Đã xoá trên KiotViet | Loại | "Không còn trên KiotViet" |
| Hàng nhiều đơn vị tính | Mỗi đơn vị là một SP riêng, tính trên **giá gốc của chính đơn vị đó**. Tuỳ chọn "chỉ đơn vị cơ bản" thì loại các đơn vị quy đổi | "Không phải đơn vị cơ bản" |
| Hàng cùng loại (size/màu) | Mỗi biến thể là một SP riêng, áp bình thường | — |
| Hàng lô/hạn sử dụng, serial/IMEI | Áp bình thường theo mã hàng | — |

Chọn phạm vi theo nhóm hàng / danh sách: khi chọn một sản phẩm có nhiều đơn vị, **tự thêm các đơn vị khác** của nó (có thể bỏ chọn từng đơn vị).

**BR-09 Giá gốc** = `basePrice` trên KiotViet **tại thời điểm triển khai** (không phải lúc tạo CTGG). (⚠️ Q9: xác nhận `basePrice` là giá khách trả, đã gồm thuế, với cửa hàng dùng thuế khấu trừ.)

**BR-10 Thời gian**: mọi mốc giờ hiểu theo UTC+7; hiệu lực tính **từ** thời điểm bắt đầu **đến trước** thời điểm kết thúc. Không cho triển khai CTGG có thời điểm kết thúc đã qua. Nếu A8 sai (bảng giá chỉ theo ngày): bắt đầu = 00:00 ngày bắt đầu, kết thúc = hết ngày kết thúc theo cách KiotViet hiểu (ghi rõ trên giao diện sau khi kiểm chứng).

**BR-11 Tính nhất quán**: tool **không bao giờ** sửa `basePrice` của sản phẩm, không sửa hoá đơn, không tạo hoá đơn.

**BR-12 Bảng giá đích** **[Mới v0.2]**
- Mỗi bảng giá đích chỉ gắn với **một** CTGG (kể cả CTGG đã kết thúc: không tái dùng, để lịch sử rõ ràng).
- Bảng giá đích phải có ngày kết thúc (không nhận bảng giá vô thời hạn) và ngày kết thúc chưa qua.
- Không dùng *Bảng giá chung*.
- Bảng giá đang chứa sẵn sản phẩm không thuộc CTGG → cảnh báo, liệt kê; người dùng xác nhận mới được dùng.
- Phạm vi chi nhánh / nhóm khách / người dùng của bảng giá phải khớp Q6; nếu bảng giá chỉ áp cho một số chi nhánh/nhóm khách → hiển thị rõ trong xác nhận triển khai.

**BR-13 Trung hoà** **[Mới v0.2]**: khi cần "gỡ" giá giảm của một sản phẩm khỏi bảng giá (dừng, loại trừ, bị loại sau khi tính lại), tool ghi giá của sản phẩm đó trong bảng giá = **giá gốc hiện tại** (đồng bộ ngay trước khi ghi). Nếu sau đó chủ cửa hàng đổi giá gốc, bảng giá có thể lệch → đối soát (FR-LOG-02) sẽ báo; khuyến nghị luôn đặt ngày kết thúc bảng giá trên KiotViet khi dừng sớm.

### Ví dụ tính (làm tròn xuống 1.000đ)
| Giá gốc | Chương trình | Tính | Làm tròn | Kết quả |
|---|---|---|---|---|
| 125.000 | Giảm 10% | 112.500 | 112.000 | ✅ 112.000 |
| 49.000 | Giảm 15% | 41.650 | 41.000 | ✅ 41.000 |
| 30.000 | Giảm 20.000đ | 10.000 | 10.000 | ✅ 10.000 |
| 15.000 | Giảm 20.000đ | −5.000 | — | ❌ Loại (BR-05) |
| 1.500 | Giảm 5% | 1.425 | 1.000 | ✅ 1.000 |
| 800 | Giảm 5% | 760 | 0 | ❌ Loại (BR-05) |
| 1.000 | Giảm 5% | 950 | 0 | ❌ Loại (BR-05) |
| 10.500 | Giảm 2% | 10.290 | 10.000 | ✅ 10.000 (giảm thực tế 4,76%) |
| 12.000 (thùng 24 lon: 240.000) | Giảm 10%, tất cả đơn vị | lon 10.800 / thùng 216.000 | 10.000 / 216.000 | ✅ tính riêng từng đơn vị |

---

## 5. Trạng thái chương trình giảm giá **[Sửa v0.2]**

| Trạng thái | Ý nghĩa | Chuyển sang |
|---|---|---|
| **Nháp** | Mới tạo/đang sửa, chưa từng ghi lên KiotViet | Đang triển khai; bị xoá |
| **Đang triển khai** | Tool đang ghi giá | Đã lên lịch / Đang chạy (thành công hết), Lỗi triển khai (có lỗi, bị huỷ, tool tắt giữa chừng) |
| **Đã lên lịch** | Đã triển khai, chưa tới giờ bắt đầu | Đang chạy (tới giờ), Cần triển khai lại (sửa), Đã huỷ (dừng), Đã kết thúc |
| **Cần triển khai lại** | Đã sửa sau khi triển khai; KiotViet **vẫn giữ giá cũ** | Đang triển khai; Đã huỷ (dừng); Đang chạy (tới giờ bắt đầu — vẫn áp giá cũ, cảnh báo đỏ) |
| **Đang chạy** | Trong thời gian hiệu lực | Đã kết thúc (hết giờ), Đã huỷ (dừng), Đang triển khai (tính lại) |
| **Lỗi triển khai** | Triển khai thất bại một phần/toàn bộ | Đang triển khai (thử lại), Đã huỷ |
| **Đã kết thúc** | Quá giờ kết thúc | — |
| **Đã huỷ** | Bị dừng sớm, đã trung hoà xong | — |

Ghi chú: trạng thái hiển thị theo thời gian (Đã lên lịch ↔ Đang chạy ↔ Đã kết thúc) được **tính khi hiển thị** từ thời gian CTGG và giờ máy chủ ước lượng; trạng thái thao tác (Nháp, Đang triển khai, Cần triển khai lại, Lỗi triển khai, Đã huỷ) được **lưu** trong DB.

---

## 6. Luồng nghiệp vụ (use case)

### UC-01 Kết nối KiotViet lần đầu
1. Admin lấy Client ID/Secret trên KiotViet (*Thiết lập cửa hàng → Thiết lập kết nối API*).
2. Vào **Kết nối KiotViet**, nhập Retailer, Client ID, Client Secret → **Kiểm tra kết nối**.
3. Thành công → **Lưu** → tool tự đồng bộ nhóm hàng, sản phẩm, bảng giá.
- *Ngoại lệ:* lỗi xác thực/mạng → thông báo theo Phụ lục B, không lưu.

### UC-02 Tạo và triển khai chương trình giảm giá **[Sửa v0.2]**
1. Trên KiotViet: tạo bảng giá mới theo hướng dẫn FR-DEP-00 (tên, thời gian, chi nhánh).
2. Trong tool: **Đồng bộ ngay** → **Chương trình giảm giá → Tạo mới.**
3. Bước 1 *Thông tin*: tên, loại (%/VND), giá trị, **chọn bảng giá đích** (thời gian tự điền).
4. Bước 2 *Phạm vi*: toàn bộ / nhóm hàng / danh sách sản phẩm; đơn vị tính; chọn loại trừ.
5. Bước 3 *Xem trước*: bảng giá gốc → giá giảm, sản phẩm bị loại kèm lý do, xung đột (BR-06).
6. **Lưu nháp** hoặc **Triển khai** → xác nhận (FR-DEP-01) → tiến độ → kết quả.
- *Ngoại lệ:* một số sản phẩm lỗi → trạng thái **Lỗi triển khai**, nút thử lại (FR-DEP-02).
- *Ngoại lệ:* mất mạng giữa chừng → dừng, giữ kết quả đã ghi; chạy lại an toàn (FR-DEP-03).
- *Ngoại lệ:* tool bị tắt giữa chừng → lần mở sau khôi phục (FR-DEP-07).

### UC-03 Bán hàng trong thời gian chương trình (trên KiotViet, không dùng tool)
1. Thu ngân bán hàng như bình thường trên KiotViet.
2. KiotViet áp giá từ bảng giá đang hiệu lực (⚠️ phụ thuộc A4; nếu A4 sai, thu ngân chọn bảng giá theo hướng dẫn FR-OPS-01).
3. Khách trả **giá đã giảm**; hoá đơn ghi đúng số tiền đã thu.

### UC-04 Dừng chương trình sớm **[Sửa v0.2]**
1. Mở CTGG Đang chạy/Đã lên lịch/Cần triển khai lại → **Dừng chương trình** → xác nhận.
2. Tool trung hoà toàn bộ sản phẩm trên bảng giá đích → trạng thái **Đã huỷ** → ghi nhật ký.
3. Tool hiển thị hướng dẫn đặt ngày kết thúc / ngừng bảng giá trên KiotViet; Admin làm theo.

### UC-05 Gia hạn hoặc rút ngắn chương trình **[Sửa v0.2]**
1. Admin đổi ngày kết thúc của bảng giá đích trên KiotViet.
2. Trong tool bấm **Đồng bộ ngay** → tool cập nhật thời gian CTGG từ bảng giá (FR-DEP-08).
3. Tool kiểm tra xung đột (BR-06) với thời gian mới; có xung đột → cảnh báo, gợi ý loại trừ sản phẩm.
- *Nhánh A5:* sửa ngày kết thúc ngay trong tool, tool cập nhật lên KiotViet.

### UC-06 Giá gốc thay đổi trong lúc chương trình chạy
1. Admin đổi giá gốc trên KiotViet (ví dụ tăng giá nhập).
2. Lần đồng bộ sau, tool cảnh báo trên CTGG (FR-DEP-05).
3. Admin chọn **Tính lại & triển khai lại** → giá giảm cập nhật theo giá gốc mới.

### UC-07 Chương trình tự kết thúc
- Tới giờ kết thúc, KiotViet tự ngừng áp bảng giá; tool (nếu đang mở hoặc lần mở sau) hiển thị **Đã kết thúc**. Không cần thao tác.

### UC-08 Thêm/loại sản phẩm khi chương trình đang chạy **[Mới v0.2]**
1. Loại trừ thêm: mở CTGG Đang chạy → thêm vào danh sách loại trừ → xác nhận → tool trung hoà các SP đó ngay.
2. Thêm sản phẩm mới vào phạm vi (ví dụ SP mới tạo thuộc nhóm hàng đã chọn): sau khi đồng bộ, tool báo "Có N sản phẩm mới thuộc phạm vi chưa được giảm giá" → Admin chọn **Tính lại & triển khai lại**.

### UC-09 Khôi phục mật khẩu **[Mới v0.2]**
1. Màn hình đăng nhập → **Quên mật khẩu** → nhập mã khôi phục.
2. Đúng → đặt mật khẩu mới (FR-AUTH-03) → hiển thị mã khôi phục mới.

---

## 7. Danh mục màn hình **[Mới v0.2]**

| Mã | Màn hình | Nội dung chính | Yêu cầu liên quan |
|---|---|---|---|
| SC-01 | Đăng nhập | Tên đăng nhập, mật khẩu, Quên mật khẩu | FR-AUTH-01, 06, 07 |
| SC-02 | Đổi mật khẩu | Mật khẩu cũ/mới/nhập lại; hiển thị mã khôi phục | FR-AUTH-02, 03, 07 |
| SC-03 | Tổng quan (trang chủ) | Trạng thái kết nối, lần đồng bộ gần nhất, CTGG đang chạy / sắp chạy, cảnh báo cần xử lý | FR-KV-05, FR-DEP-05, 08 |
| SC-04 | Kết nối KiotViet | Retailer, Client ID, Secret, Kiểm tra, Lưu, Ngắt kết nối | FR-KV-01…07 |
| SC-05 | Sản phẩm | Tra cứu, lọc, cột Đang giảm giá, nút Đồng bộ | FR-SYNC-05, 06 |
| SC-06 | Bảng giá | Danh sách bảng giá KiotViet, thời gian, chi nhánh, CTGG gắn kèm; hướng dẫn tạo | FR-SYNC-03, FR-DEP-00 |
| SC-07 | Danh sách CTGG | Lọc trạng thái, tạo mới, nhân bản | FR-PROMO-04, 06 |
| SC-08 | Tạo/Sửa CTGG (3 bước) | Thông tin → Phạm vi → Xem trước | FR-PROMO-01, 02, 03, 07 |
| SC-09 | Chi tiết CTGG | Thông tin, danh sách SP & giá, lịch sử triển khai, nút Triển khai / Dừng / Đối soát / In hướng dẫn | FR-DEP-*, FR-LOG-02, FR-OPS-* |
| SC-10 | Tiến độ triển khai | Thanh tiến độ, kết quả từng SP, Thử lại, Huỷ | FR-DEP-02 |
| SC-11 | Nhật ký | Lọc theo thời gian, loại thao tác | FR-LOG-01 |
| SC-12 | Cài đặt | Mặc định CTGG, sao lưu/khôi phục, tự khoá phiên | FR-SET-*, FR-AUTH-05 |

---

## 8. Yêu cầu dữ liệu (lưu trong SQLite cục bộ) **[Sửa v0.2]**

| Thực thể | Trường chính | Ghi chú |
|---|---|---|
| `UserAccount` | Username, PasswordHash, MustChangePassword, FailedLoginCount, LockedUntil, RecoveryCodeHash | ✅ Đã có; bổ sung 3 trường cuối |
| `KiotVietConnection` | Retailer, ClientId, ClientSecretEncrypted, LastSyncedAt, ServerClockOffsetSec | 1 bản ghi |
| `ApiQuota` | WindowStart, GetCount | Bộ đếm lượt gọi (FR-KV-04) |
| `Category` | KiotVietId, Name, ParentId, IsDeleted | Đồng bộ |
| `Product` | KiotVietId, Code, Name, FullName, CategoryId, BasePrice, Unit, MasterUnitId, ConversionValue, MasterProductId, ProductType, IsActive, AllowsSale, IsDeleted, ModifiedAt | Đồng bộ |
| `PriceBook` | KiotVietId, Name, IsActive, StartDate, EndDate, IsGlobal, BranchIds, CustomerGroupIds, UserIds, IsDeleted | Đồng bộ |
| `PriceBookItem` | PriceBookId, ProductId, Price | Đồng bộ, dùng cho đối soát và xung đột |
| `DiscountProgram` | Name, Type (Percent/Amount), Value, StartAt, EndAt, ScopeType, UnitScope, Rounding, TargetPriceBookId (unique), Status, NeedsRedeploy, Note, CreatedAt, UpdatedAt, RowVersion | |
| `DiscountProgramScope` | ProgramId, CategoryId? / ProductId?, IsExclusion | Phạm vi & loại trừ |
| `DeploymentRun` | ProgramId, Kind (Deploy/Redeploy/Stop/Neutralize), StartedAt, FinishedAt, Result (Running/Success/Partial/Failed/Cancelled), SuccessCount, FailureCount | Mỗi lần triển khai/dừng |
| `DeploymentItem` | RunId, ProductId, BasePriceSnapshot, TargetPrice, Status (Pending/Success/Failed), Error, AttemptCount | Kết quả từng SP; bản ghi `Pending` còn lại = lần chạy dở dang |
| `AppSetting` | Key, Value | Cài đặt |
| `AuditLog` | At, Action, Target, Result, Detail | Nhật ký |

Tiền lưu dạng số nguyên VND (`decimal` không phần thập phân). Thời gian lưu UTC, hiển thị UTC+7. DB bật chế độ WAL; mọi thay đổi trạng thái CTGG ghi trong giao dịch.

---

## 9. Tích hợp KiotViet Public API **[Sửa v0.2]**

| Việc | API | Trạng thái |
|---|---|---|
| Lấy token | `POST https://id.kiotviet.vn/connect/token` (`client_credentials`, scope `PublicApi.Access`) | ✅ Tài liệu chính thức |
| Danh sách nhóm hàng | `GET /categories` (`hierachicalData`, `lastModifiedFrom`, `removedIds`) | ✅ |
| Danh sách / chi tiết sản phẩm | `GET /products` (`includePricebook`, `includeRemoveIds`, `lastModifiedFrom`, `productType`, `isActive`), `GET /products/{id}` | ✅ |
| Danh sách bảng giá | `GET /pricebooks` (`includePriceBookBranch`, `includePriceBookCustomerGroups`, `includePriceBookUsers`) | ✅ Có trong tài liệu, cần gọi thử |
| Chi tiết bảng giá (SP + giá) | `GET /pricebooks/{id}` (phân trang) | ✅ Có trong tài liệu, cần gọi thử |
| Cập nhật giá SP trong bảng giá | `POST /pricebooks/detail` | ✅ Có trong tài liệu; cần gọi thử body, số dòng/lần, giới hạn tốc độ |
| Tạo bảng giá / đặt ngày hiệu lực / gỡ SP khỏi bảng giá | — | ❌ Không có trong tài liệu (A3, A5, A7) |
| Hoá đơn (cho FR-RPT-01) | `GET /invoices` | Could |
| Webhook | `product.update`, `pricebook.update`, `pricebookdetail.update` | Không dùng ở bản đầu (tool desktop không nhận webhook được) |

Base URL: `https://public.kiotapi.com`. Header bắt buộc: `Retailer`, `Authorization: Bearer <token>`.

---

## 10. Yêu cầu phi chức năng

| Mã | Nhóm | Yêu cầu |
|---|---|---|
| NFR-01 | Chính xác | Không bao giờ ghi giá giảm ≥ giá gốc hoặc ≤ 0 lên KiotViet (BR-05, BR-07); mọi lần triển khai đều qua bước tính lại trên giá gốc mới nhất; kiểm tra lại ngay trước khi gửi từng request |
| NFR-02 | Tin cậy | Triển khai lặp lại an toàn (FR-DEP-03); lỗi giữa chừng không làm hỏng dữ liệu đã ghi; tool tắt đột ngột không ảnh hưởng CTGG đang chạy và được khôi phục (FR-DEP-07) |
| NFR-03 | Bảo mật | Client Secret mã hoá DPAPI; mật khẩu và mã khôi phục hash PBKDF2 (≥ 100.000 vòng, salt riêng); không ghi secret/mật khẩu/token vào log; chỉ gọi HTTPS, kiểm tra chứng chỉ |
| NFR-04 | Hiệu năng | Giao diện không bị đơ khi đồng bộ/triển khai (chạy nền, có tiến độ, huỷ được); xem trước 10.000 SP ≤ 2 giây; khởi động tool ≤ 5 giây |
| NFR-05 | Giới hạn API | Tuân thủ C2, C3; tự điều tiết và thử lại theo FR-KV-04 |
| NFR-06 | Dễ dùng | Toàn bộ giao diện tiếng Việt; số tiền định dạng `125.000 ₫`; ngày `dd/MM/yyyy HH:mm`; mọi thao tác ghi lên KiotViet đều có xác nhận và xem trước; thông báo lỗi nói rõ nguyên nhân và cách xử lý (Phụ lục B) |
| NFR-07 | Truy vết | Mọi thao tác thay đổi dữ liệu KiotViet đều có trong nhật ký (FR-LOG-01) |
| NFR-08 | Triển khai | Một file `.exe` self-contained + `appsettings.json`; chạy Windows 10/11 x64; màn hình tối thiểu 1366×768 |
| NFR-09 | Sao lưu | Toàn bộ dữ liệu nằm trong `app.db`; sao lưu/khôi phục theo FR-SET-02 |
| NFR-10 | Nâng cấp | **[Mới v0.2]** Phiên bản mới tự nâng cấp cấu trúc DB (migration) khi mở; tự sao lưu `app.db` trước khi migration |
| NFR-11 | Kiểm thử | **[Mới v0.2]** Quy tắc tính giá (BR-01…BR-08) có unit test phủ toàn bộ ví dụ mục 4; lớp gọi API có thể thay bằng bản giả lập để kiểm thử không cần KiotViet thật |

---

## 11. Kịch bản nghiệm thu (UAT) **[Mới v0.2]**

Thực hiện trên gian hàng KiotViet thử (hoặc gian hàng thật ngoài giờ bán), có ít nhất: 1 SP thường, 1 SP nhiều đơn vị, 1 SP có biến thể, 1 combo, 1 dịch vụ, 1 SP ngừng kinh doanh.

| Mã | Kịch bản | Kết quả mong đợi |
|---|---|---|
| UAT-01 | Kết nối với Client Secret sai | Thông báo "Client ID hoặc Client Secret không đúng.", không lưu |
| UAT-02 | Kết nối đúng, đồng bộ lần đầu | Đủ nhóm hàng, sản phẩm, bảng giá; số lượng khớp KiotViet |
| UAT-03 | Tạo CTGG giảm 10% cho một nhóm hàng, xem trước | Giá khớp BR-01/BR-04; combo, dịch vụ, SP ngừng kinh doanh bị loại đúng lý do |
| UAT-04 | Triển khai CTGG bắt đầu sau 10 phút | Trạng thái Đã lên lịch; bảng giá trên KiotViet có đúng giá; trước giờ bắt đầu bán ra giá gốc |
| UAT-05 | Tới giờ bắt đầu, bán 1 SP trên KiotViet | Hoá đơn ghi giá giảm; khách trả đúng giá giảm; tool hiện Đang chạy |
| UAT-06 | Tắt tool, chờ qua giờ kết thúc, bán lại | Bán ra giá gốc; mở tool thấy Đã kết thúc |
| UAT-07 | Rút mạng giữa lúc triển khai, cắm lại, chạy tiếp | Không trùng lặp; kết quả cuối giống chạy một lần |
| UAT-08 | Tắt tool (End Task) giữa lúc triển khai, mở lại | Tool báo lần chạy dở dang, chạy tiếp thành công |
| UAT-09 | Dừng sớm CTGG đang chạy | Bán ra giá gốc ngay; trạng thái Đã huỷ |
| UAT-10 | Tạo CTGG thứ hai trùng sản phẩm, trùng thời gian | Bị chặn, liệt kê SP và CTGG xung đột |
| UAT-11 | Đổi giá gốc một SP trên KiotViet trong lúc CTGG chạy, đồng bộ | Cảnh báo giá gốc thay đổi; Tính lại & triển khai lại cho giá mới đúng |
| UAT-12 | Sửa tay giá một SP trong bảng giá trên KiotViet, bấm Đối soát | SP đó được liệt kê là lệch; Ghi lại giá đúng sửa được |
| UAT-13 | Sửa CTGG Đã lên lịch rồi không triển khai lại | Trạng thái Cần triển khai lại, cảnh báo đỏ |
| UAT-14 | Đổi ngày kết thúc bảng giá trên KiotViet, đồng bộ | Thời gian CTGG trong tool cập nhật theo |
| UAT-15 | Nhập sai mật khẩu 5 lần; dùng mã khôi phục | Khoá 5 phút; mã khôi phục đặt lại được mật khẩu |
| UAT-16 | Sao lưu, xoá `app.db`, khôi phục | Dữ liệu CTGG, nhật ký đầy đủ; phải nhập lại Secret nếu khác máy |
| UAT-17 | Đặt giờ Windows lệch 10 phút | Cảnh báo lệch giờ; trạng thái CTGG vẫn đúng |

---

## 12. Ngoài phạm vi (bản đầu)
- Can thiệp màn hình bán hàng KiotViet; sửa, tạo hay xoá hoá đơn.
- Ghi nhận doanh thu khác với số tiền thực thu (vi phạm nguyên tắc 1.3).
- Khuyến mại theo hoá đơn: mua X tặng Y, giảm theo tổng đơn, voucher, tích điểm.
- Giảm giá theo nhóm khách hàng; giảm khác nhau cho từng chi nhánh trong cùng một CTGG (có thể bổ sung sau, ⚠️ Q6).
- Giảm giá theo khung giờ lặp lại (ví dụ giờ vàng 17h–19h mỗi ngày).
- Nhiều người dùng, phân quyền.
- Kết nối nhiều gian hàng KiotViet cùng lúc.
- Nhận webhook (cần server có địa chỉ public).
- Tự tạo bảng giá trên KiotViet (không có API — A3).

---

## 13. Vấn đề mở (⚠️ Cần chốt)

| Mã | Câu hỏi | Đề xuất mặc định | Ảnh hưởng |
|---|---|---|---|
| **Q1** | Kiểm chứng A2, A4, A6, A8 và cấu trúc `POST /pricebooks/detail` bằng tài khoản thật | Làm ngay giai đoạn 0 (Phụ lục A) | FR-DEP |
| **Q2** | Phạm vi áp dụng: toàn bộ / nhóm hàng / từng sản phẩm? | Hỗ trợ cả ba + danh sách loại trừ | FR-PROMO-01 |
| **Q3** | Có chạy nhiều CTGG chồng lấn trên cùng sản phẩm không? | Không (BR-06) | BR-06, FR-PROMO-07 |
| **Q4** | Làm tròn thế nào? | Xuống 1.000đ | BR-04 |
| **Q5** | Xác nhận xử lý hàng nhiều đơn vị, biến thể, combo, dịch vụ theo BR-08 | Như bảng BR-08 | BR-08 |
| **Q6** | Cửa hàng có nhiều chi nhánh không? Giảm giá áp mọi chi nhánh hay từng chi nhánh? | Mọi chi nhánh | BR-12, FR-DEP-00 |
| **Q7** | Thu ngân có phải chọn bảng giá khi bán không (A4)? | Xác nhận trên KiotViet thật | UC-03, FR-OPS-01 |
| **Q8** | ~~Gỡ bỏ feature "Quản lý khách hàng" hiện có?~~ ✅ **Đã chốt 30/09/2026:** gỡ bỏ, đã thực hiện | — | FR-OLD-01 |
| **Q9** | **[Mới]** Cửa hàng tính thuế theo phương pháp nào? Giá bán trên KiotViet là giá trước hay sau thuế? | Giảm trên giá khách thực trả (`basePrice` hiện tại) | BR-09 |
| **Q10** | **[Mới]** Cách khôi phục khi quên mật khẩu tool: mã khôi phục có chấp nhận được không? | Mã khôi phục (FR-AUTH-07) | FR-AUTH-07 |
| **Q11** | **[Mới]** Chủ cửa hàng có đồng ý tự tạo bảng giá trên KiotViet cho mỗi CTGG (tool hướng dẫn)? | Có (không có API thay thế) | Mục 2.2 |
| **Q12** | **[Mới]** Có cần báo cáo hiệu quả chương trình (FR-RPT-01) ngay bản đầu? | Để bản sau | FR-RPT-01 |
| **Q13** | **[Mới]** Gian hàng đang dùng gói KiotViet nào, đã bật được *Thiết lập kết nối API* chưa? | Phải có trước giai đoạn 0 | Toàn bộ |

---

## 14. Kế hoạch giai đoạn (đề xuất)
| Giai đoạn | Nội dung | Điều kiện xong |
|---|---|---|
| 0 | Chốt mục 13; kiểm chứng API bằng tài khoản thật, điền Phụ lục A | SRS v1.0 được duyệt |
| 1 | Module KV + SYNC (kèm FR-KV-08, đồng bộ xoá, đơn vị tính) | Đồng bộ đủ sản phẩm/bảng giá của cửa hàng thật; UAT-01, 02 đạt |
| 2 | Module PROMO (tạo, xem trước, xung đột) — chưa ghi lên KiotViet | Xem trước đúng mọi ví dụ mục 4; UAT-03, 10 đạt |
| 3 | Module DEP (triển khai, dừng, thử lại, khôi phục, trung hoà) | UAT-04 → 09, 13, 14 đạt trên cửa hàng thử |
| 4 | Module LOG, SET, OPS, AUTH bổ sung, hoàn thiện | Toàn bộ UAT mục 11 đạt |

---

## Phụ lục A — Biên bản kiểm chứng API (điền ở Giai đoạn 0) **[Mới v0.2]**

| Hạng mục | Cách thử | Kết quả | Ghi chú |
|---|---|---|---|
| A1 — `GET /pricebooks` kèm chi nhánh/nhóm khách | Gọi với tài khoản thử | | |
| A1 — `GET /pricebooks/{id}` phân trang | | | |
| A2 — `POST /pricebooks/detail` 1 SP | Ghi giá 1 SP vào bảng giá thử | | Ghi lại body mẫu |
| A2 — số SP tối đa mỗi request | Thử 10, 50, 100 | | |
| A2 — SP chưa có trong bảng giá | Ghi giá SP chưa có | Tự thêm? Báo lỗi? | |
| A4 — bảng giá tự áp dụng tại quầy | Bán thử trong giờ hiệu lực | | Ảnh chụp màn hình |
| A6 — SP không có trong bảng giá | Bán SP ngoài bảng giá khi bảng giá đang hiệu lực | | |
| A8 — độ chi tiết thời gian bảng giá | Tạo bảng giá đặt giờ/phút | | |
| Giới hạn POST | Gửi liên tục, theo dõi mã 429 | | |

## Phụ lục B — Danh mục thông báo lỗi **[Mới v0.2]**

| Mã | Tình huống | Thông báo hiển thị |
|---|---|---|
| E-KV-01 | Sai Client ID/Secret | "Client ID hoặc Client Secret không đúng." |
| E-KV-02 | Sai Retailer | "Không tìm thấy gian hàng. Kiểm tra lại tên Retailer." |
| E-KV-03 | Mất mạng / timeout | "Không kết nối được KiotViet. Kiểm tra Internet rồi thử lại." |
| E-KV-04 | Vượt giới hạn (429) | "KiotViet đang giới hạn số lượt gọi. Thử lại sau ít phút." |
| E-KV-05 | Gói KiotViet không có API / API bị tắt | "Gian hàng chưa bật kết nối API. Vào KiotViet → Thiết lập cửa hàng → Thiết lập kết nối API." |
| E-KV-06 | Lỗi máy chủ KiotViet (5xx) sau 3 lần thử | "KiotViet đang gặp sự cố. Thử lại sau." |
| E-KV-07 | Giờ máy lệch | "Giờ máy tính đang lệch X phút so với KiotViet. Hãy chỉnh lại giờ Windows." |
| E-PR-01 | Tên CTGG trùng | "Đã có chương trình chưa kết thúc mang tên này." |
| E-PR-02 | Giá trị % ngoài khoảng | "Mức giảm phải lớn hơn 0 và nhỏ hơn 100%." |
| E-PR-03 | Bảng giá đích đã gắn CTGG khác | "Bảng giá này đã dùng cho chương trình '<tên>'. Hãy tạo bảng giá mới trên KiotViet." |
| E-PR-04 | Bảng giá không có ngày kết thúc / đã hết hạn | "Bảng giá cần có ngày kết thúc chưa qua." |
| E-PR-05 | Xung đột chương trình | "N sản phẩm đang thuộc chương trình khác trong cùng thời gian. Xem danh sách." |
| E-DP-01 | Chưa xem trước sau lần sửa cuối | "Hãy xem trước giá trước khi triển khai." |
| E-DP-02 | Một phần SP lỗi | "Đã ghi X/Y sản phẩm. Z sản phẩm lỗi — bấm Thử lại." |
| E-DP-03 | SP không còn trên KiotViet | "Sản phẩm đã bị xoá trên KiotViet." |
| E-AU-01 | Khoá do sai nhiều lần | "Đăng nhập sai quá nhiều lần. Thử lại sau M phút." |
