# Đặc tả yêu cầu phần mềm (SRS) — KiotViet Tool

| | |
|---|---|
| Phiên bản | **0.3.2** (thay thế 0.3.1) |
| Ngày | 01/10/2026 |
| Trạng thái | Chờ chủ sản phẩm duyệt; các mục đánh dấu **⚠️ Cần chốt** phải được trả lời trước khi triển khai phần liên quan |
| Người đọc | Chủ cửa hàng (duyệt nghiệp vụ), developer / AI (triển khai), người kiểm thử |
| Tài liệu đi kèm | *Tài liệu nghiệp vụ dành cho khách hàng* (bản rút gọn, không có chi tiết kỹ thuật) |

### Lịch sử thay đổi

| Phiên bản | Ngày | Nội dung thay đổi |
|---|---|---|
| 0.1 | 28/09/2026 | Bản nháp đầu tiên |
| 0.2 | 29/09/2026 | Kiểm chứng tài liệu KiotViet; chọn mô hình "bảng giá do chủ cửa hàng tạo trên KiotViet, tool đọc thời gian từ bảng giá"; bổ sung trạng thái, khôi phục sau gián đoạn, kiểm tra giờ máy, đơn vị tính / combo / dịch vụ, khoá đăng nhập, sao lưu, danh mục màn hình, thông báo lỗi, kịch bản nghiệm thu |
| 0.2.1 | 30/09/2026 | Chốt Q8: gỡ feature *Quản lý khách hàng*. Trang chủ tạm thời là màn Sản phẩm (SC-05) |
| **0.3** | 01/10/2026 | **Đổi mô hình triển khai theo yêu cầu chủ sản phẩm**: toàn bộ chương trình giảm giá (kể cả thời gian) thiết lập **chỉ trong tool**; tool **sửa trực tiếp giá bán (`basePrice`)** của sản phẩm trên KiotViet khi chương trình bắt đầu và **trả lại giá gốc** khi kết thúc; nhân viên bán hàng không biết có chương trình. Bỏ khái niệm *bảng giá đích*, *trung hoà*, hướng dẫn tạo bảng giá (FR-DEP-00), in hướng dẫn thu ngân (FR-OPS-01). Thêm hẹn giờ chạy nền qua Windows Task Scheduler, lưu và bảo vệ giá gốc, xử lý giá bị sửa tay trong lúc chạy. Kết quả gọi thử API thật lần 1 (Phụ lục A). FR-AUTH-05 đã triển khai (khoá màn hình). |
| **0.3.1** | 01/10/2026 | Đồng bộ với module Triển khai đã code và kiểm thử trên gian hàng thật `luckymart`: A9 ✅ (đổi `basePrice` không ảnh hưởng trường khác), A10 một phần (KiotViet thỉnh thoảng từ chối cả đợt ghi → tool ghi lại từng sản phẩm); BR-06 chỉ xét chương trình đã lên lịch / đang giữ giá (nháp không chặn nhau); huỷ lịch đưa chương trình về Nháp; SC-10 gộp vào SC-09; đánh dấu các phần chưa làm (⏳): sửa ghi chú / loại trừ thêm khi đang chạy, phát hiện giá sửa tay lúc đồng bộ, thông báo Windows khi chạy nền, xuất giá gốc, trả giá khẩn cấp, FR-DEP-11, bảng `PriceOperation`. Đánh dấu ✅ các yêu cầu đã triển khai |
| **0.3.2** | 01/10/2026 | Xác thực 2 lớp qua Telegram: FR-AUTH-08 (mã đăng nhập 6 số gửi qua bot), FR-AUTH-09 (bắt buộc kết nối Telegram sau khi đổi mật khẩu lần đầu, 10 mã dự phòng), FR-AUTH-10 (khoá khi sai mã). Màn SC-14, bảng `TelegramConnection`, §9.1, Q17–Q18 |

Các mục mới hoặc thay đổi so với 0.2.1 được đánh dấu **[v0.3]**.

---

## 1. Giới thiệu

### 1.1 Mục đích
Tài liệu mô tả nghiệp vụ và yêu cầu của **KiotViet Tool**: ứng dụng desktop Windows giúp chủ cửa hàng đang dùng phần mềm bán hàng **KiotViet** thiết lập **chương trình giảm giá có thời hạn** (theo % hoặc theo số tiền VND). Tool tự đổi giá bán của sản phẩm trên KiotViet qua **KiotViet Public API** trong thời gian chương trình và trả lại giá gốc khi chương trình kết thúc. **[v0.3]**

### 1.2 Phạm vi
**Trong phạm vi**
- Đăng nhập tool bằng một tài khoản quản trị duy nhất (đã có).
- Kết nối tool với một cửa hàng KiotViet qua Public API.
- Đồng bộ danh mục nhóm hàng, sản phẩm (và bảng giá, chỉ để cảnh báo) từ KiotViet về máy.
- Tạo, xem trước, áp dụng, dừng chương trình giảm giá theo % hoặc VND, có thời điểm bắt đầu và kết thúc **đặt trong tool**. **[v0.3]**
- Tự động áp giá giảm lúc bắt đầu và trả giá gốc lúc kết thúc, kể cả khi cửa sổ tool đang đóng. **[v0.3]**
- Theo dõi kết quả, đối soát giá trên KiotViet, nhật ký thao tác.
- Sao lưu / khôi phục dữ liệu của tool.

**Ngoài phạm vi** (xem chi tiết mục 12)
- Can thiệp trực tiếp vào màn hình bán hàng của KiotViet.
- Sửa hoá đơn sau khi đã thanh toán, hoặc ghi nhận doanh thu khác với số tiền thực thu.
- Khuyến mại theo hoá đơn (mua X tặng Y, giảm theo tổng đơn, voucher).

### 1.3 Nguyên tắc nghiệp vụ cốt lõi **[v0.3]**
> 1. **Mọi thiết lập giảm giá nằm trong tool.** Chủ cửa hàng không phải tạo hay sửa gì trên KiotViet cho chương trình.
> 2. **Nhân viên bán hàng không cần biết có chương trình.** Nhân viên quét mã vạch như bình thường; màn hình bán hàng hiện **giá đã giảm** vì chính giá bán của sản phẩm đã được tool đổi.
> 3. **Khách được giảm giá thật và trả đúng số tiền đã giảm.** Đơn giá trên màn bán hàng, số tiền khách trả, giá lưu trên hoá đơn và doanh thu KiotViet ghi nhận **luôn khớp nhau**.
> 4. **Giá gốc không bao giờ bị mất.** Tool lưu giá gốc trước khi đổi và luôn trả lại khi chương trình kết thúc hoặc bị dừng.

### 1.4 Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| Chủ cửa hàng / Admin | Người dùng duy nhất của tool |
| Giá bán | Giá chung (`basePrice`) của sản phẩm trên KiotViet — giá màn bán hàng hiện khi quét mã vạch |
| Giá gốc **[v0.3]** | Giá bán của sản phẩm **ngay trước khi tool áp giá giảm**, được tool lưu lại (ảnh chụp giá gốc) để trả lại khi kết thúc |
| Giá giảm | Giá sau khi áp chương trình và làm tròn; tool ghi giá này vào giá bán trong thời gian chương trình |
| Chương trình giảm giá (CTGG) | Một cấu hình giảm giá: loại, giá trị, thời gian, phạm vi sản phẩm |
| Áp giá **[v0.3]** | Tool ghi giá giảm vào giá bán của các sản phẩm thuộc CTGG trên KiotViet |
| Trả giá **[v0.3]** | Tool ghi lại giá gốc vào giá bán khi CTGG kết thúc hoặc bị dừng |
| Tác vụ hẹn giờ **[v0.3]** | Tác vụ Windows Task Scheduler do tool đăng ký để tự chạy áp giá / trả giá đúng giờ khi cửa sổ tool đang đóng |
| Bảng giá (Price book) | Tính năng của KiotViet: danh sách giá riêng có thời hạn. Ở v0.3 tool **không ghi** vào bảng giá, chỉ đọc để cảnh báo xung đột |
| Retailer | Tên gian hàng KiotViet (phần tên miền `<retailer>.kiotviet.vn`) |
| Client ID / Client Secret | Thông tin kết nối API lấy từ KiotViet: *Thiết lập cửa hàng → Thiết lập kết nối API* |
| Hàng đơn vị tính | Một mặt hàng có nhiều đơn vị (lon/thùng). Trên KiotViet mỗi đơn vị là một sản phẩm riêng (`id`, `basePrice` riêng), liên kết qua `masterUnitId`, `conversionValue` |
| Hàng cùng loại (biến thể) | Các sản phẩm khác nhau về thuộc tính (size/màu), liên kết qua `masterProductId` |

### 1.5 Tài liệu tham chiếu
- [Hướng dẫn sử dụng Public API Ngành Bán lẻ — KiotViet](https://www.kiotviet.vn/huong-dan-su-dung-public-api-retail/)
- [KiotViet Public API](https://www.kiotviet.vn/huong-dan-su-dung-kiotviet/retail-ket-noi-api/public-api/)
- `CLAUDE.md`, `README.md` trong repo (kiến trúc, quy tắc code)

---

## 2. Mô tả tổng quan

### 2.1 Bối cảnh sản phẩm **[v0.3]**
```
┌───────────────────────────┐  HTTPS (OAuth2)   ┌────────────────────────┐
│ KiotViet Tool (.exe)       │ ────────────────▶ │ KiotViet Public API     │
│ máy Windows của chủ CH     │ ◀──────────────── │ public.kiotapi.com      │
│ SQLite cục bộ (app.db)     │  đổi giá bán       └───────────┬────────────┘
│ + tác vụ Task Scheduler    │  (basePrice)                   │ dữ liệu dùng chung
└───────────────────────────┘                    ┌───────────▼────────────┐
                                                  │ Màn hình bán hàng       │
                                                  │ KiotViet (thu ngân)     │
                                                  │ quét mã → hiện giá bán  │
                                                  └─────────────────────────┘
```
Tool **không** chạy trên máy bán hàng và **không** tương tác với màn hình bán hàng. Tool chỉ đổi giá bán của sản phẩm trên KiotViet; màn bán hàng hiện giá đó như mọi ngày.

### 2.2 Chiến lược kỹ thuật được chọn: **sửa giá bán** **[v0.3]**

| Phương án | Mô tả | Đánh giá |
|---|---|---|
| **A. Sửa giá bán (chọn)** | Lúc bắt đầu, tool lưu giá gốc rồi ghi giá giảm vào `basePrice`; lúc kết thúc, tool ghi lại giá gốc | ✅ Mọi thiết lập trong tool; màn bán hàng chắc chắn hiện giá giảm; nhân viên không cần biết. ⚠️ Tool phải chạy được vào giờ bắt đầu / kết thúc (giải quyết bằng tác vụ hẹn giờ + chạy bù, FR-DEP-08); giá gốc phải được bảo vệ (FR-DEP-09) |
| B. Bảng giá KiotViet (phương án của v0.2) | Ghi giá giảm vào bảng giá có ngày hiệu lực | ❌ Loại: không có API tạo bảng giá hay đặt ngày (chủ cửa hàng phải thao tác trên KiotViet cho mỗi chương trình); chưa chắc màn bán hàng tự áp bảng giá mà thu ngân không phải chọn |
| C. API khuyến mại | Tạo chương trình khuyến mại của KiotViet qua API | ❌ Loại: Public API không có |

**Mô hình vận hành:**
1. Chủ cửa hàng tạo CTGG trong tool: mức giảm, phạm vi sản phẩm, **thời điểm bắt đầu** (mặc định *ngay khi xác nhận*, hoặc hẹn giờ — ⚠️ Q14) và **thời điểm kết thúc**.
2. Tới giờ bắt đầu, tool đồng bộ giá bán mới nhất, **lưu giá gốc** từng sản phẩm, tính giá giảm và **ghi vào giá bán** trên KiotViet.
3. Trong thời gian chương trình, nhân viên bán hàng như bình thường; hoá đơn ghi giá đã giảm.
4. Tới giờ kết thúc (hoặc khi chủ cửa hàng bấm *Dừng*), tool **ghi lại giá gốc**.
5. Nếu lúc đó cửa sổ tool đang đóng, **tác vụ Windows Task Scheduler** tự mở tool ở chế độ nền để làm bước 2 / 4. Nếu máy tắt, tác vụ chạy bù ngay khi người dùng đăng nhập lại; mỗi lần mở tool cũng tự chạy bù việc quá hạn.

### 2.3 Người dùng
| Vai trò | Mô tả | Quyền |
|---|---|---|
| Admin (chủ cửa hàng) | Người duy nhất dùng tool | Toàn quyền |
| Thu ngân | **Không dùng tool** và không cần biết có chương trình; bán hàng trên KiotViet như bình thường | — |

### 2.4 Môi trường vận hành
- Windows 10/11 x64, một file `KiotVietTool.exe` tự chứa (không cần cài .NET).
- Cần Internet khi đồng bộ, áp giá, trả giá; các màn hình xem dữ liệu đã đồng bộ vẫn dùng được khi mất mạng.
- Dữ liệu cục bộ: `%LocalAppData%\KiotVietTool\app.db`; log: `%LocalAppData%\KiotVietTool\logs\`.
- Gian hàng KiotViet phải dùng gói dịch vụ **có Public API** (Q13).
- Mỗi máy chỉ chạy **một** phiên bản tool cùng lúc (khoá single-instance); mở lần hai thì đưa cửa sổ đang chạy lên trước.
- **[v0.3]** Máy cài tool nên **bật và có người dùng Windows đăng nhập** vào giờ bắt đầu / kết thúc chương trình. Nếu không, giá chỉ được đổi khi máy được bật lại (⚠️ Q15).

### 2.5 Ràng buộc
- **C1.** Chỉ dùng KiotViet Public API chính thức (OAuth2 `client_credentials`, scope `PublicApi.Access`).
- **C2.** Giới hạn API: GET tối đa **5.000 request/giờ** (theo tài liệu KiotViet); tool tự điều tiết. Tài liệu không nêu giới hạn cho POST/PUT → áp dụng mức thận trọng mặc định (FR-KV-04), điều chỉnh sau khi gọi thử.
- **C3.** Mỗi lần gọi danh sách tối đa **100 bản ghi** (`pageSize` ≤ 100, phân trang bằng `currentItem`).
- **C4.** Một tool kết nối **một** gian hàng (một Retailer) tại một thời điểm.
- **C5.** Múi giờ nghiệp vụ: **Asia/Ho_Chi_Minh (UTC+7)**, không phụ thuộc múi giờ cài trên Windows.
- **C6.** Token KiotViet có hạn theo `expires_in` (thực tế 86.400 giây); tool không giả định cố định.
- **C7. [v0.3]** `basePrice` dùng chung cho **mọi chi nhánh**: một CTGG áp cho mọi chi nhánh; không giảm riêng theo chi nhánh.
- **C8. [v0.3]** Tool là ứng dụng desktop, không có server chạy 24/7: độ đúng giờ của việc áp / trả giá phụ thuộc máy cài tool đang bật (C8 được giảm thiểu bởi FR-DEP-07, FR-DEP-08).

### 2.6 Giả định và phụ thuộc **[v0.3]**
| Mã | Giả định | Tình trạng | Nếu sai thì |
|---|---|---|---|
| **A1** | Đọc được nhóm hàng, sản phẩm, bảng giá | ✅ Đã gọi thử trên gian hàng thật (Phụ lục A) | — |
| **A9** | `PUT /products/{id}` (hoặc `PUT /listupdatedproducts`) đổi được `basePrice` **mà không xoá / đổi các trường khác** khi chỉ gửi `id` + `basePrice` | ✅ **Đã kiểm chứng 01/10/2026** trên gian hàng thật: chỉ `basePrice` và `modifiedDate` đổi, 25 trường còn lại (tên, mã vạch, nhóm, tồn kho, bảng giá, ảnh…) giữ nguyên (Phụ lục A) | — |
| **A10** | Số sản phẩm tối đa mỗi lần gọi `PUT /listupdatedproducts` và giới hạn tốc độ PUT | ⚠️ Một phần: đợt 2–5 sản phẩm chạy được; **KiotViet thỉnh thoảng (≈10–30% số đợt) trả 420 `KvValidateProductException` "Danh sách sản phẩm cập nhật rỗng" và không ghi đợt đó** — không phụ thuộc khoảng cách giữa các lần gọi. Chưa gặp 429 sau ~150 lần ghi ở 2 request/giây. Cỡ đợt tối đa chưa đo (gian hàng chỉ có 20 SP) | Tool ghi lại từng sản phẩm của đợt bị từ chối (`PUT /products/{id}`), rồi đọc lại để xác minh. Cỡ đợt mặc định 20 (`KiotViet:PriceUpdateBatchSize`) |
| **A11** | Màn bán hàng hiện giá bán mới **ngay** sau khi đổi (không giữ bộ nhớ đệm cũ lâu) | ⚠️ Chưa kiểm chứng | Hướng dẫn thu ngân tải lại màn bán hàng khi bắt đầu ca; ghi rõ độ trễ trong UAT |
| **A12** | Đổi `basePrice` không làm hỏng bảng giá khác của cửa hàng (bảng giá tính theo công thức từ giá chung sẽ đổi theo) | ⚠️ Chưa kiểm chứng | Cảnh báo khi sản phẩm nằm trong bảng giá khác đang hiệu lực (BR-06) |
| **A13** | Màn bán hàng dùng giá chung khi thu ngân không chọn bảng giá khác | ⚠️ Chưa kiểm chứng | Sản phẩm nằm trong bảng giá khác đang được chọn sẽ không hiện giá giảm → cảnh báo BR-06 |
| **A14** | Windows Task Scheduler (người dùng thường, không quyền admin) tạo được tác vụ chạy `KiotVietTool.exe --run-scheduled` vào giờ định sẵn, có tuỳ chọn chạy bù khi lỡ giờ | ⚠️ Đã code (tạo tác vụ bằng file XML qua `schtasks /Create /XML`), XML đã kiểm tra; **chưa chạy thử trên Windows thật** | Chỉ dựa vào chạy bù khi mở tool + bộ đếm mỗi phút khi tool đang mở + cảnh báo |
| ~~A2–A8~~ | Các giả định về ghi / tạo / đặt ngày bảng giá của v0.2 | Không còn dùng ở v0.3 | — |

> **Giai đoạn 0:** A9 đã kiểm chứng; còn **A11** (màn bán hàng nhận giá mới ngay), **A13** và **A14** (Task Scheduler trên Windows thật) — xem Phụ lục A.

---

## 3. Yêu cầu chức năng

Mức ưu tiên: **Must** (bắt buộc cho bản đầu) · **Should** (nên có) · **Could** (có thì tốt). ✅ = đã triển khai · ⏳ = chưa triển khai.

### 3.1 Module AUTH — Đăng nhập
| Mã | Yêu cầu | Ưu tiên |
|---|---|---|
| FR-AUTH-01 | Đăng nhập bằng tên đăng nhập + mật khẩu; sai thì báo chung "Tên đăng nhập hoặc mật khẩu không đúng." | Must ✅ |
| FR-AUTH-02 | Lần chạy đầu tạo tài khoản `admin`/`admin`, **bắt buộc đổi mật khẩu** ở lần đăng nhập đầu | Must ✅ |
| FR-AUTH-03 | Đổi mật khẩu: tối thiểu 8 ký tự, khác mật khẩu cũ, nhập lại khớp | Must ✅ |
| FR-AUTH-04 | Đăng xuất; mọi màn hình khác yêu cầu đã đăng nhập | Must ✅ |
| FR-AUTH-05 | **[v0.3]** Sau N phút không thao tác (mặc định 30, `Auth:IdleLockMinutes`, 0 = tắt) tool **khoá màn hình**: màn đang làm và dữ liệu nhập dở được giữ, nhập lại mật khẩu để mở; cảnh báo đếm ngược trước khi khoá (mặc định 60 giây); không khoá khi đang đồng bộ / áp giá / trả giá; nhập sai 5 lần thì đăng xuất. **Áp giá / trả giá theo lịch vẫn chạy khi đang khoá** | Should ✅ |
| FR-AUTH-06 | Sai mật khẩu 5 lần liên tiếp → khoá đăng nhập 5 phút, hiện thời gian còn lại; đăng nhập đúng thì đặt lại bộ đếm | Should |
| FR-AUTH-08 | **[v0.3.2]** Đăng nhập 2 lớp: mật khẩu đúng → bot Telegram gửi **mã 6 số** (ngẫu nhiên mật mã học, hết hạn sau 5 phút, tối đa 5 lần nhập cho mỗi mã, gửi lại sau 60 giây). Chỉ khi nhập đúng mã mới vào tool. Không gửi được mã (mạng chặn Telegram…) → báo lỗi và gợi ý **Gửi lại mã** hoặc **dùng mã dự phòng**. Mở khoá màn hình (FR-AUTH-05) chỉ cần mật khẩu, không cần mã | Must ✅ |
| FR-AUTH-09 | **[v0.3.2]** Bắt buộc kết nối Telegram: sau khi đổi mật khẩu lần đầu (FR-AUTH-02), tool chỉ cho dùng màn **Kết nối Telegram** (SC-14) cho tới khi xong. Chủ cửa hàng **tự tạo bot** qua @BotFather → dán Bot Token → bấm Bắt đầu trong bot → tool nhận ra cuộc trò chuyện → gửi mã xác nhận → nhập đúng mã → tool hiện **10 mã dự phòng** (16 ký tự, dùng một lần, chỉ hiện một lần, lưu dạng băm). Token mã hoá DPAPI. Kết nối lại (đổi bot / tài khoản Telegram) ở màn Telegram, tạo bộ mã dự phòng mới | Must ✅ |
| FR-AUTH-10 | **[v0.3.2]** Sai mã đăng nhập hoặc mã dự phòng **10 lần** (cộng dồn qua các lần đăng nhập) → khoá đăng nhập 15 phút; nhập đúng thì đặt lại bộ đếm | Must ✅ |
| FR-AUTH-07 | Khôi phục mật khẩu: khi đổi mật khẩu lần đầu, tool sinh **mã khôi phục** (hiển thị một lần). Quên mật khẩu → nhập mã khôi phục để đặt mật khẩu mới; mã cũ hết hiệu lực, sinh mã mới (⚠️ Q10) | Should |

### 3.2 Module KV — Kết nối KiotViet
**FR-KV-01 Cấu hình kết nối** (Must ✅)
- Nhập: Retailer, Client ID, Client Secret. Retailer tự bỏ khoảng trắng, chữ hoa; chấp nhận dán cả `abc.kiotviet.vn` và tự tách `abc`.
- Client Secret **được mã hoá** khi lưu (DPAPI, phạm vi người dùng Windows), **không bao giờ** hiện lại dạng rõ, không ghi vào log.
- AC: lưu xong, mở lại màn hình thấy Retailer và Client ID; ô Secret để trống và chỉ ghi đè khi nhập giá trị mới.

**FR-KV-02 Kiểm tra kết nối** (Must ✅): lấy token, gọi một API đọc nhẹ; hiển thị thành công (kèm số sản phẩm) hoặc lỗi theo **Phụ lục B**.

**FR-KV-03 Quản lý token** (Must ✅)
- Lấy token từ `https://id.kiotviet.vn/connect/token`; lưu trong bộ nhớ, dùng lại tới khi còn ≤ 60 giây trước hạn.
- Gặp HTTP 401: lấy token mới và **thử lại đúng 1 lần**; vẫn 401 → báo lỗi xác thực, dừng tác vụ.
- Mọi request gửi header `Retailer` và `Authorization: Bearer <token>`.

**FR-KV-04 Điều tiết & thử lại** (Must)
- GET: tự giới hạn ≤ 4.500 request/giờ (cửa sổ trượt 60 phút) ✅; lưu bộ đếm vào DB để tắt/mở tool không reset (chưa làm).
- PUT/POST: mặc định tối đa 2 request/giây (`KiotViet:MinWriteIntervalMs` = 500), chạy tuần tự (không song song) ✅.
- Lỗi tạm thời (429, 5xx, timeout 30 giây): thử lại tối đa 3 lần, chờ tăng dần (1s, 2s, 4s); tuân theo `Retry-After` nếu có ✅.
- Lỗi 4xx khác 401/429: không thử lại, ghi nhận lỗi kèm nội dung phản hồi (đã lọc thông tin nhạy cảm) ✅.

**FR-KV-05 Trạng thái kết nối** (Should): sidebar hiển thị Đã kết nối / Chưa cấu hình / Lỗi, kèm thời điểm đồng bộ gần nhất.

**FR-KV-06 Ngắt kết nối** (Should ✅): xoá cấu hình kết nối và dữ liệu đồng bộ; **không** xoá CTGG và nhật ký. Yêu cầu xác nhận. **[v0.3]** Không cho ngắt khi còn CTGG *Đang chạy* hoặc *chưa trả giá xong* (tool cần kết nối để trả giá gốc).

**FR-KV-07 Đổi sang gian hàng khác** (Should ✅): nhập Retailer khác Retailer đã lưu → yêu cầu ngắt kết nối (FR-KV-06) trước.

**FR-KV-08 Kiểm tra giờ máy** (Must): mỗi lần gọi API, đọc header `Date` của máy chủ; lệch > 2 phút → cảnh báo "Giờ máy tính đang lệch X phút. Chương trình giảm giá có thể bắt đầu / kết thúc sai giờ." **[v0.3]** Giờ bắt đầu / kết thúc được so với **giờ máy chủ ước lượng** = giờ máy + độ lệch đo được.

### 3.3 Module SYNC — Đồng bộ danh mục
**FR-SYNC-01 Đồng bộ nhóm hàng** (Must ✅): lấy toàn bộ nhóm hàng (cây cha–con); nhóm không còn trên KiotViet thì xoá khỏi máy.

**FR-SYNC-02 Đồng bộ sản phẩm** (Must ✅)
- Lưu: `id`, `code`, `name`, `fullName`, nhóm hàng, `basePrice`, đơn vị tính, `masterUnitId`, `conversionValue`, `masterProductId`, loại hàng (combo / thường / dịch vụ), `isActive`, `allowsSale`, `modifiedDate`.
- Lần đầu: đồng bộ toàn bộ (100/lần). Các lần sau: chỉ lấy thay đổi (`lastModifiedFrom` = mốc lần trước − 5 phút), kèm `includeRemoveIds=true`.
- Sản phẩm bị xoá trên KiotViet: đánh dấu *Đã xoá* (không xoá cứng).
- Có tiến độ, cho phép huỷ giữa chừng; huỷ thì dữ liệu cũ giữ nguyên (ghi theo giao dịch).
- **[v0.3]** Trong thời gian CTGG đang chạy, `basePrice` đồng bộ về là **giá giảm** do tool ghi. Tool không dùng giá này làm giá gốc (BR-09); nếu nó khác giá giảm tool đã ghi → sản phẩm bị sửa tay (FR-DEP-05).
- AC: 10.000 sản phẩm đồng bộ lần đầu ≤ 5 phút khi mạng ổn định.

**FR-SYNC-03 Đồng bộ bảng giá** (Should ✅, **[v0.3]** hạ từ Must): danh sách bảng giá và giá từng sản phẩm trong bảng giá, **chỉ dùng để cảnh báo** sản phẩm đang nằm trong bảng giá khác (BR-06).

**FR-SYNC-04 Đồng bộ trước khi áp giá** (Must): ngay trước khi áp giá, tool **đọc lại giá bán hiện tại** của các sản phẩm trong phạm vi để lưu giá gốc chính xác.

**FR-SYNC-05 Tra cứu sản phẩm** (Should ✅): danh sách sản phẩm đã đồng bộ, tìm theo mã/tên (không phân biệt dấu), lọc theo nhóm hàng, loại hàng, trạng thái kinh doanh; cột "Đang giảm giá" cho biết sản phẩm đang được CTGG nào giảm và giá giảm; cột Giá bán hiện **giá gốc** trong lúc giảm ✅.

**FR-SYNC-06 Đồng bộ tự động** (Should): khi mở tool và đăng nhập, nếu đã cấu hình kết nối → tự đồng bộ thay đổi (chạy nền). Có nút "Đồng bộ ngay" thủ công ✅.

### 3.4 Module PROMO — Chương trình giảm giá
**FR-PROMO-01 Tạo CTGG** (Must) **[v0.3]**. Trường dữ liệu:

| Trường | Bắt buộc | Quy tắc |
|---|---|---|
| Tên chương trình | Có | 1–100 ký tự, không trùng với CTGG chưa kết thúc (không phân biệt hoa thường, bỏ khoảng trắng đầu cuối) |
| Loại giảm | Có | `%` hoặc `VND` |
| Giá trị giảm | Có | `%`: 0 < giá trị < 100, tối đa 2 chữ số thập phân. `VND`: số nguyên > 0, tối đa 1.000.000.000 |
| Bắt đầu | Có | **Ngay khi xác nhận** (mặc định) hoặc **hẹn giờ**: ngày + giờ (đến phút), UTC+7, không ở quá khứ (⚠️ Q14) |
| Kết thúc | Có | Ngày + giờ (đến phút), UTC+7, **sau** thời điểm bắt đầu ít nhất 5 phút; tối đa 366 ngày. Có nút nhanh: +1 ngày, +3 ngày, +7 ngày, hết tháng |
| Phạm vi áp dụng | Có | Toàn bộ sản phẩm / theo nhóm hàng (gồm nhóm con) / theo danh sách sản phẩm (⚠️ Q2) |
| Loại trừ | Không | Danh sách sản phẩm loại khỏi phạm vi |
| Đơn vị tính áp dụng | Có | Tất cả đơn vị (mặc định) / chỉ đơn vị cơ bản — BR-08 |
| Làm tròn | Có | Mặc định theo Cài đặt (BR-04) |
| Ghi chú | Không | ≤ 500 ký tự |

CTGG mới tạo ở trạng thái **Nháp**; chưa ảnh hưởng gì tới KiotViet. ~~Bảng giá đích~~ (bỏ ở v0.3).

**FR-PROMO-02 Xem trước giá** (Must)
- Bảng: mã SP, tên SP (fullName), đơn vị, giá gốc (giá bán hiện tại), giá giảm, số tiền giảm, % giảm thực tế, cảnh báo.
- Tổng kết: số SP áp dụng, số SP bị loại (kèm lý do BR-05, BR-07, BR-08), tổng số tiền giảm nếu mỗi SP bán 1 đơn vị (tham khảo).
- Tìm kiếm, lọc "chỉ SP bị loại" / "chỉ SP có cảnh báo", xuất Excel (FR-LOG-03).
- AC: không áp giá khi chưa xem trước: nút *Lưu và áp dụng* chỉ có ở bước Xem trước; nút *Áp dụng* ở màn chi tiết tính lại xem trước và hiện số sản phẩm sẽ đổi giá trong hộp xác nhận. ✅
- Cảnh báo (không chặn): % giảm thực tế ≥ 50%; sản phẩm đang nằm trong bảng giá khác có hiệu lực trùng thời gian (BR-06); giá giảm < giá vốn (nếu đọc được giá vốn — Could).

**FR-PROMO-03 Sửa CTGG** (Must) **[v0.3]**
- Nháp, Đã lên lịch: sửa mọi trường; với Đã lên lịch, tool cập nhật lại tác vụ hẹn giờ. ✅
- Đang chạy / Lỗi áp giá: đổi **thời điểm kết thúc** (gia hạn / rút ngắn — UC-05) ✅. ⏳ Sửa **ghi chú** và **loại trừ thêm sản phẩm** (tool trả giá gốc cho các sản phẩm đó ngay) chưa làm. Muốn đổi mức giảm hoặc phạm vi → *Dừng* rồi tạo chương trình mới (hoặc *Nhân bản*).
- Đã kết thúc / Đã dừng: chỉ xem. ✅

**FR-PROMO-04 Nhân bản CTGG** (Should ✅): mở form tạo CTGG mới từ CTGG có sẵn (tên "Bản sao của …", bắt đầu *Ngay khi áp dụng*, giữ thời lượng cũ); chỉ lưu khi bấm Lưu.

**FR-PROMO-05 Xoá CTGG** (Must ✅): chỉ xoá được CTGG Nháp hoặc Đã lên lịch (chưa từng ghi giá lên KiotViet). CTGG đã áp giá thì dùng **Dừng** (FR-DEP-04).

**FR-PROMO-06 Danh sách CTGG** (Must ✅): cột tên + phạm vi, mức giảm, thời gian, trạng thái (kể cả "Quá hạn — chưa trả giá"), nút Chi tiết / Sửa / Nhân bản / Xoá; lọc theo trạng thái; sắp xếp theo thời điểm bắt đầu; mặc định ẩn CTGG kết thúc quá 90 ngày.

**FR-PROMO-07 Kiểm tra xung đột** (Must ✅): khi lưu, khi áp dụng / lên lịch và khi đổi giờ kết thúc, phát hiện sản phẩm thuộc **hai CTGG có thời gian chồng lấn** và xử lý theo BR-06.

### 3.5 Module DEP — Áp giá / trả giá trên KiotViet **[v0.3 — viết lại, v0.3.1 đã triển khai]**
**FR-DEP-01 Xác nhận chương trình** (Must ✅)
1. Chủ cửa hàng bấm **Áp dụng** (màn chi tiết CTGG Nháp) hoặc **Lưu và áp dụng** (bước Xem trước) → tool tính lại xem trước và kiểm tra xung đột.
2. Hộp xác nhận "Áp dụng chương trình giảm giá?": **số sản phẩm sẽ đổi giá**, số SP bị loại giữ nguyên giá, thời điểm đổi giá (ngay / lúc <giờ bắt đầu>) và thời điểm tool tự trả giá gốc.
3. Bắt đầu ngay (hoặc giờ hẹn đã qua) → áp giá (FR-DEP-02). Hẹn giờ trong tương lai → trạng thái **Đã lên lịch**, đăng ký tác vụ hẹn giờ (FR-DEP-08); chưa gọi ghi giá nào.

**FR-DEP-02 Áp giá** (Must ✅)
1. Lần áp đầu tiên: đồng bộ danh mục (FR-SYNC-04), tính phạm vi + loại trừ, rồi **đọc giá bán hiện tại trực tiếp từ KiotViet** làm **giá gốc** và tính giá giảm từ đó; ghi danh sách giá gốc vào DB **trước** khi gửi giá nào.
2. Tự sao lưu `app.db` (FR-DEP-09); **nếu sao lưu lỗi thì không áp giá** (E-DP-07).
3. Chuyển **Đang áp giá**; đọc lại giá trên KiotViet ngay trước khi ghi: đã bằng giá giảm → coi như xong; bằng giá gốc → ghi; khác cả hai → lỗi sản phẩm đó ("giá vừa đổi"), không ghi.
4. Ghi theo đợt (`PUT /listupdatedproducts`, mặc định 20 SP/đợt, tối đa 2 request/giây); đợt bị từ chối → ghi lại từng sản phẩm; lưu kết quả sau mỗi đợt.
5. Đọc lại toàn bộ giá để xác minh. Xong hết → **Đang chạy**; có lỗi / bị huỷ / mất mạng → **Lỗi áp giá** (sản phẩm đã ghi vẫn giữ giá giảm, nút *Thử lại*).
- AC: sau khi áp giá, giá bán trên KiotViet của 100% sản phẩm thành công = giá giảm ✅ (đã kiểm thử); quét mã vạch trên màn bán hàng thấy giá giảm, hoá đơn ghi đúng giá giảm (⚠️ chủ cửa hàng kiểm tra trên màn bán hàng — A11).

**FR-DEP-03 Không trùng lặp, không giảm chồng** (Must ✅): chạy lại (thử lại, sau khi huỷ, mất mạng, tắt tool) cho kết quả như chạy một lần. Giá gốc của một sản phẩm **chỉ lưu một lần** cho mỗi CTGG; giá giảm luôn tính từ giá gốc đã lưu (BR-12).

**FR-DEP-04 Dừng CTGG sớm** (Must ✅)
- Xác nhận "Dừng chương trình? Giá bán trên KiotViet sẽ trở về giá gốc ngay."
- Tool trả giá (FR-DEP-06, bước 2) cho mọi sản phẩm còn giữ giá giảm → **Đã dừng**; xoá tác vụ hẹn giờ.
- Trả giá lỗi một phần → **Lỗi trả giá**, cảnh báo đỏ, nút *Thử lại*; tool cũng tự thử lại mỗi phút / mỗi lần chạy nền.
- CTGG *Đã lên lịch*: nút đổi thành **Huỷ lịch** → chương trình trở về **Nháp** (sửa / xoá được), không gọi KiotViet.

**FR-DEP-05 Giá bị sửa tay trong lúc chạy** (Must)
- ✅ Khi trả giá (kết thúc / dừng), nếu giá bán trên KiotViet **khác giá giảm tool đã ghi** → sản phẩm được đánh dấu "Giá bị sửa trên KiotViet", tool **không ghi đè**; chương trình vẫn kết thúc / dừng bình thường.
- ✅ Màn chi tiết hiện cảnh báo vàng với hai nút áp cho cả loạt: *Giữ giá hiện tại* hoặc *Trả về giá gốc*. ⏳ Chọn từng dòng chưa có.
- ⏳ Phát hiện ngay khi đồng bộ (trước lúc trả giá) chưa làm — dùng FR-LOG-02 khi có.

**FR-DEP-06 Tự kết thúc — trả giá** (Must ✅)
1. Tới giờ kết thúc (tool đang mở: bộ đếm mỗi phút; tool đóng: tác vụ hẹn giờ; máy tắt: chạy bù — FR-DEP-07), chuyển **Đang trả giá**.
2. Với từng sản phẩm: giá trên KiotViet = giá giảm → ghi lại **giá gốc**; = giá gốc → đã xong; khác cả hai → FR-DEP-05. Ghi theo đợt như FR-DEP-02, rồi đọc lại xác minh.
3. Xong hết → **Đã kết thúc**; lỗi → **Lỗi trả giá**.
- AC: sau giờ kết thúc (+ tối đa ~1 phút khi tool đang mở), giá bán trở về giá gốc ✅ (đã kiểm thử).

**FR-DEP-07 Chạy bù & khôi phục sau gián đoạn** (Must ✅)
- Khi tool mở, ngay sau khi khởi động và **mỗi phút**, tool chạy các việc tới hạn: CTGG *Đã lên lịch* tới giờ bắt đầu; *Đang chạy* / *Lỗi áp giá* đã quá giờ kết thúc; CTGG bị bỏ dở ở *Đang áp giá* (tiếp tục áp, nếu chưa hết giờ) hoặc *Đang trả giá* / *Lỗi trả giá* (tiếp tục trả). *Lỗi áp giá* chưa hết giờ **không** tự thử lại (chờ chủ cửa hàng bấm *Thử lại*).
- CTGG quá giờ kết thúc mà còn giữ giá giảm hiển thị **Quá hạn — chưa trả giá** (đỏ) trên danh sách và màn chi tiết tới khi trả xong.
- Lỡ cả giờ bắt đầu lẫn giờ kết thúc (máy tắt suốt chương trình): không áp giá, chuyển **Đã kết thúc**, ghi log.

**FR-DEP-08 Hẹn giờ chạy nền** (Must ✅, ⚠️ chưa chạy thử trên Windows thật — A14)
- Khi CTGG được lên lịch / bắt đầu chạy / đổi giờ kết thúc, tool tạo hoặc cập nhật tác vụ **Windows Task Scheduler** `KiotVietTool\Program-<id>-Start` / `-End` (dưới tài khoản Windows đang đăng nhập, không cần quyền admin, `StartWhenAvailable` = chạy bù khi lỡ giờ, tự chạy lại tối đa 12 lần mỗi 5 phút nếu còn việc lỗi). Kết thúc / dừng / huỷ lịch / xoá → xoá tác vụ. Không tạo được tác vụ → E-DP-06 (chương trình vẫn chạy, dựa vào chạy bù).
- `KiotVietTool.exe --run-scheduled`: nếu tool đang mở → báo cửa sổ đang chạy làm việc tới hạn ngay; nếu không → chạy ẩn **không cần đăng nhập tool**, làm việc tới hạn (FR-DEP-07), ghi log, thoát với mã 0 (xong) hoặc 1 (còn việc lỗi, để Task Scheduler chạy lại). ⏳ Thông báo Windows khi xong / lỗi chưa làm (chỉ ghi log).
- Trên macOS (môi trường dev) không có Task Scheduler: chỉ ghi log.

**FR-DEP-09 Bảo vệ giá gốc** (Must)
- ✅ Tự sao lưu `app.db` ngay trước mỗi lần áp giá (`VACUUM INTO`, thư mục `backups` cạnh `app.db`, giữ 20 bản gần nhất); sao lưu lỗi → không áp giá.
- ✅ Giá gốc được ghi vào DB **trước** khi gửi giá giảm lên KiotViet (ghi theo giao dịch).
- ⏳ Nút *Xuất danh sách giá gốc* (Excel/CSV) — chưa làm (giá gốc vẫn xem được ở màn chi tiết).
- ⏳ Lệnh khẩn cấp *Trả giá gốc cho tất cả chương trình đang chạy* — chưa làm (chưa có màn Cài đặt).

**FR-DEP-10 Tiến độ & kết quả** (Must ✅): ngay trên màn chi tiết CTGG (SC-09): thanh tiến độ "<bước>: x/y sản phẩm", nút **Huỷ** (dừng sau đợt đang ghi; phần đã ghi giữ nguyên; bấm *Thử lại* để tiếp tục); bảng từng sản phẩm: giá gốc, giá giảm, trạng thái (Đang giảm giá / Đã trả giá gốc / Lỗi đổi giá / Giá bị sửa trên KiotViet / Giữ giá KiotViet / Chưa trả được giá) + lý do lỗi; nút **Thử lại**. ⏳ Thời gian ước tính còn lại chưa có.

**FR-DEP-11 Sản phẩm mới thuộc phạm vi** (Should ⏳): sản phẩm mới tạo trên KiotViet trong lúc CTGG chạy mà thuộc nhóm hàng của CTGG → sau khi đồng bộ, tool báo "Có N sản phẩm mới thuộc phạm vi chưa được giảm giá" và cho **Áp giá cho sản phẩm mới**.

### 3.6 Module LOG — Nhật ký & đối soát
**FR-LOG-01 Nhật ký thao tác** (Must): ghi đăng nhập (kể cả thất bại), khoá / mở khoá màn hình, đổi mật khẩu, cấu hình kết nối, đồng bộ, tạo/sửa/xoá/áp giá/trả giá/dừng CTGG, chạy nền theo lịch, sao lưu/khôi phục: thời gian, thao tác, kết quả, chi tiết. Không ghi mật khẩu/secret/token. Lưu tối thiểu 12 tháng; màn hình xem có lọc theo thời gian, loại thao tác.

**FR-LOG-02 Đối soát giá** (Should) **[v0.3]**: nút "Đối soát" đọc giá bán hiện tại trên KiotViet của các sản phẩm trong CTGG: *Đang chạy* → so với giá giảm đã ghi; *Đã kết thúc / Đã dừng* → so với giá gốc (kiểm tra đã trả đúng). Liệt kê dòng lệch, có nút "Ghi lại giá đúng".

**FR-LOG-03 Xuất Excel** (Could): xuất danh sách sản phẩm, giá gốc, giá giảm của một CTGG.

**FR-LOG-04 Log kỹ thuật** (Must ✅): file log theo ngày, giữ 30 ngày, tối đa 20 MB/file (chưa giới hạn dung lượng); ghi request API mức tóm tắt (URL, mã HTTP, thời gian) — không ghi header `Authorization`, không ghi Client Secret.

### 3.7 Module SET — Cài đặt
**FR-SET-01** (Should): cài đặt mặc định cho CTGG mới: kiểu làm tròn (BR-04), giá sàn (BR-05), đơn vị tính áp dụng (BR-08).

**FR-SET-02 Sao lưu / khôi phục** (Should): nút "Sao lưu" tạo bản sao `app.db` (cơ chế backup của SQLite, không cần tắt tool) ra thư mục người dùng chọn. "Khôi phục" chọn file → xác nhận → khởi động lại tool. Tự sao lưu hằng ngày khi mở tool, giữ 7 bản gần nhất. Client Secret mã hoá theo người dùng Windows: khôi phục sang máy/người dùng khác phải nhập lại Secret. **[v0.3]** Không cho khôi phục bản sao lưu cũ hơn lần áp giá gần nhất của CTGG đang chạy mà không cảnh báo (sẽ mất giá gốc).

### 3.8 Module OPS — Hỗ trợ vận hành **[v0.3]**
~~FR-OPS-01 Hướng dẫn thu ngân~~ — bỏ: thu ngân không cần biết có chương trình.

**FR-OPS-02 Bảng giá niêm yết** (Could): in danh sách sản phẩm + giá gốc + giá giảm để dán tại quầy/kệ (khi chủ cửa hàng muốn quảng bá chương trình).

### 3.9 Module RPT — Báo cáo
**FR-RPT-01 Hiệu quả chương trình** (Could): sau khi CTGG kết thúc, đọc hoá đơn trong thời gian CTGG, tổng hợp số lượng bán, doanh thu, tổng tiền đã giảm (giá gốc − giá bán trên hoá đơn) của các SP thuộc CTGG. Chỉ đọc. (Có thể để bản sau.)

### 3.10 Tính năng có sẵn cần quyết định
**FR-OLD-01 Quản lý khách hàng:** ✅ Đã gỡ bỏ ngày 30/09/2026 (Q8).

---

## 4. Quy tắc nghiệp vụ

**BR-01 Công thức giảm theo %**: `Giá giảm = Giá gốc × (1 − p / 100)`, sau đó làm tròn theo BR-04. Tính bằng `decimal`, không dùng số thực dấu phẩy động.

**BR-02 Công thức giảm theo VND**: `Giá giảm = Giá gốc − v`, sau đó làm tròn theo BR-04.

**BR-03 Thứ tự xử lý**: kiểm tra loại sản phẩm (BR-08) → tính → làm tròn → kiểm tra giá sàn (BR-05) → kiểm tra không tăng giá (BR-07).

**BR-04 Làm tròn** (⚠️ Q4): không làm tròn; làm tròn **xuống** đến 100đ / 500đ / 1.000đ. Mặc định đề xuất: **xuống 1.000đ**. Tiền VND luôn là số nguyên; "không làm tròn" vẫn cắt phần lẻ dưới 1đ.

**BR-05 Giá sàn**: giá giảm phải **> 0** (và ≥ giá sàn trong Cài đặt nếu có). Vi phạm → loại khỏi CTGG, lý do "Giá sau giảm không hợp lệ".

**BR-06 Chồng lấn chương trình** (⚠️ Q3) **[v0.3.1]**: **một sản phẩm chỉ thuộc tối đa một CTGG trong cùng một khoảng thời gian**, xét với các CTGG **đã lên lịch hoặc đang giữ giá giảm** (Đã lên lịch, Đang áp giá, Đang chạy, Lỗi áp giá, Đang trả giá, Lỗi trả giá). **Các bản nháp không chặn nhau**: chương trình nào áp dụng trước được trước, chương trình sau bị chặn lúc lưu / áp dụng / đổi giờ kết thúc, kèm danh sách SP và CTGG xung đột. Cảnh báo (không chặn) nếu sản phẩm đang nằm trong **bảng giá KiotViet** khác có hiệu lực trùng thời gian (A13).

**BR-07 Không tăng giá**: giá giảm phải **nhỏ hơn** giá gốc; nếu làm tròn khiến giá giảm = giá gốc thì loại với lý do "Mức giảm quá nhỏ sau làm tròn". (Với làm tròn xuống, điều này chỉ là chốt an toàn.)

**BR-08 Loại sản phẩm** (⚠️ Q5):

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

Chọn phạm vi theo danh sách: khi chọn một sản phẩm có nhiều đơn vị, **tự thêm các đơn vị khác** của nó (có thể loại trừ từng đơn vị).

**BR-09 Giá gốc** **[v0.3]** = giá bán (`basePrice`) đọc từ KiotViet **ngay trước khi áp giá** cho sản phẩm đó, được lưu lại và không đổi trong suốt CTGG. (⚠️ Q9: xác nhận `basePrice` là giá khách trả, đã gồm thuế.)

**BR-10 Thời gian** **[v0.3]**: mọi mốc giờ hiểu theo UTC+7, chính xác đến phút, so theo giờ máy chủ ước lượng (FR-KV-08). Giá giảm có hiệu lực từ lúc tool áp giá xong (≥ giờ bắt đầu) đến lúc tool trả giá xong (≥ giờ kết thúc). Khi máy đang bật, độ trễ mục tiêu ≤ 2 phút; khi máy tắt, việc được chạy bù (FR-DEP-07).

**BR-11 Tính nhất quán** **[v0.3]**: tool **chỉ sửa `basePrice`** của các sản phẩm thuộc CTGG, **chỉ trong thời gian CTGG**, và **luôn trả lại giá gốc** khi CTGG kết thúc hoặc bị dừng. Tool không sửa bất kỳ trường nào khác của sản phẩm (A9), không sửa / tạo / xoá hoá đơn.

**BR-12 Không giảm chồng** **[v0.3]**: giá giảm luôn tính từ **giá gốc đã lưu**; không bao giờ tính từ giá bán hiện tại của một sản phẩm đang được giảm giá.

**BR-13 Trả giá** **[v0.3]**: chỉ ghi lại giá gốc khi giá bán hiện tại **đúng bằng** giá giảm tool đã ghi; nếu khác → xử lý theo FR-DEP-05, không ghi đè.

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

**Ví dụ vòng đời giá** **[v0.3]**: sản phẩm giá bán 125.000, CTGG giảm 10% từ 01/10 08:00 đến 04/10 08:00 → 08:00 01/10 tool lưu giá gốc 125.000, ghi giá bán 112.000; thu ngân quét mã thấy 112.000, hoá đơn ghi 112.000; 08:00 04/10 tool thấy giá bán vẫn 112.000 → ghi lại 125.000.

---

## 5. Trạng thái chương trình giảm giá **[v0.3]**

| Trạng thái | Ý nghĩa | Chuyển sang |
|---|---|---|
| **Nháp** | Mới tạo / đang sửa, chưa xác nhận | Đã lên lịch, Đang áp giá; bị xoá |
| **Đã lên lịch** | Đã xác nhận, chưa tới giờ bắt đầu; **chưa ghi gì** lên KiotViet | Đang áp giá (tới giờ), Nháp (sửa lại), bị xoá / huỷ lịch |
| **Đang áp giá** | Tool đang ghi giá giảm | Đang chạy, Lỗi áp giá (có lỗi / bị huỷ / tắt giữa chừng) |
| **Đang chạy** | Giá giảm đang có hiệu lực trên KiotViet | Đang trả giá (hết giờ hoặc dừng) |
| **Lỗi áp giá** | Một phần sản phẩm chưa áp được | Đang áp giá (thử lại), Đang trả giá (dừng) |
| **Đang trả giá** | Tool đang ghi lại giá gốc | Đã kết thúc / Đã dừng, Lỗi trả giá |
| **Lỗi trả giá** | Một phần sản phẩm chưa trả được giá gốc (cảnh báo đỏ) | Đang trả giá (thử lại) |
| **Đã kết thúc** | Hết giờ, đã trả giá gốc xong | — |
| **Đã dừng** | Dừng sớm, đã trả giá gốc xong | — |

Trạng thái được **lưu** trong DB (do tool thực hiện, không suy ra từ đồng hồ). Nhãn phụ tính khi hiển thị: **Quá hạn — chưa trả giá** (Đang chạy / Lỗi trả giá mà đã qua giờ kết thúc), **Có giá bị sửa tay** (FR-DEP-05).

---

## 6. Luồng nghiệp vụ (use case) **[v0.3]**

### UC-01 Kết nối KiotViet lần đầu
1. Admin lấy Client ID/Secret trên KiotViet (*Thiết lập cửa hàng → Thiết lập kết nối API*).
2. Vào **Kết nối KiotViet**, nhập Retailer, Client ID, Client Secret → **Kiểm tra kết nối**.
3. Thành công → **Lưu** → tool tự đồng bộ nhóm hàng, sản phẩm, bảng giá.
- *Ngoại lệ:* lỗi xác thực/mạng → thông báo theo Phụ lục B, không lưu.

### UC-02 Tạo và áp dụng chương trình giảm giá
1. **Chương trình giảm giá → Tạo mới.**
2. Bước 1 *Thông tin*: tên, loại (%/VND), giá trị, **bắt đầu (ngay / hẹn giờ)**, **kết thúc**, làm tròn.
3. Bước 2 *Phạm vi*: toàn bộ / nhóm hàng / danh sách sản phẩm; đơn vị tính; loại trừ.
4. Bước 3 *Xem trước*: giá gốc → giá giảm, sản phẩm bị loại kèm lý do, xung đột (BR-06).
5. **Lưu nháp** hoặc **Áp dụng** → xác nhận (FR-DEP-01) → tiến độ → kết quả.
- *Ngoại lệ:* một số sản phẩm lỗi → **Lỗi áp giá**, nút *Thử lại*.
- *Ngoại lệ:* mất mạng / tắt tool giữa chừng → lần sau chạy tiếp an toàn (FR-DEP-03, FR-DEP-07).

### UC-03 Bán hàng trong thời gian chương trình (trên KiotViet, không dùng tool)
1. Thu ngân quét mã vạch như bình thường.
2. Màn bán hàng hiện **giá đã giảm** (là giá bán hiện tại của sản phẩm).
3. Khách trả giá đã giảm; hoá đơn lưu đúng giá đã giảm.

### UC-04 Dừng chương trình sớm
1. Mở CTGG Đang chạy / Lỗi áp giá → **Dừng chương trình** → xác nhận.
2. Tool trả giá gốc (FR-DEP-04) → **Đã dừng** → ghi nhật ký.

### UC-05 Gia hạn hoặc rút ngắn chương trình
1. Mở CTGG Đang chạy → sửa **thời điểm kết thúc** → lưu.
2. Tool kiểm tra xung đột (BR-06) với thời gian mới, cập nhật tác vụ hẹn giờ. Rút ngắn về quá khứ = dừng ngay (UC-04).

### UC-06 Chủ cửa hàng sửa giá trên KiotViet trong lúc chương trình chạy
1. Admin đổi giá bán một sản phẩm trên KiotViet (ví dụ tăng giá nhập).
2. Lần đồng bộ sau (hoặc lúc trả giá), tool phát hiện giá khác giá giảm đã ghi → cảnh báo "giá đã bị sửa trên KiotViet" (FR-DEP-05).
3. Khi kết thúc, tool không ghi đè sản phẩm đó; Admin chọn giữ giá hiện tại hoặc trả về giá gốc đã lưu.

### UC-07 Chương trình tự kết thúc
- Tới giờ kết thúc: nếu tool đang mở, tool tự trả giá; nếu tool đóng, tác vụ hẹn giờ mở tool ở chế độ nền để trả giá rồi tự thoát (FR-DEP-08). Không cần thao tác.

### UC-08 Máy tắt vào giờ kết thúc
1. Giờ kết thúc qua đi khi máy đang tắt → giá giảm tiếp tục có hiệu lực trên KiotViet.
2. Người dùng bật máy và đăng nhập Windows → tác vụ hẹn giờ chạy bù, trả giá gốc; hoặc khi mở tool, tool chạy bù (FR-DEP-07).
3. Tool ghi nhật ký thời gian trễ và hiện thông báo.

### UC-09 Thêm / loại sản phẩm khi chương trình đang chạy
1. Loại trừ thêm: mở CTGG Đang chạy → thêm vào danh sách loại trừ → xác nhận → tool trả giá gốc cho các SP đó ngay.
2. Sản phẩm mới thuộc phạm vi: sau khi đồng bộ, tool báo "Có N sản phẩm mới thuộc phạm vi chưa được giảm giá" → **Áp giá cho sản phẩm mới** (FR-DEP-11).

### UC-10 Khôi phục mật khẩu
1. Màn hình đăng nhập → **Quên mật khẩu** → nhập mã khôi phục.
2. Đúng → đặt mật khẩu mới (FR-AUTH-03) → hiển thị mã khôi phục mới.

---

## 7. Danh mục màn hình

| Mã | Màn hình | Nội dung chính | Yêu cầu liên quan |
|---|---|---|---|
| SC-01 | Đăng nhập | Tên đăng nhập, mật khẩu, Quên mật khẩu; bước 2: mã Telegram / mã dự phòng, Gửi lại mã | FR-AUTH-01, 06, 07, 08, 10 |
| SC-02 | Đổi mật khẩu | Mật khẩu cũ/mới/nhập lại; hiển thị mã khôi phục | FR-AUTH-02, 03, 07 |
| SC-03 | Tổng quan (trang chủ) | Trạng thái kết nối, lần đồng bộ gần nhất, CTGG đang chạy / sắp chạy, **CTGG quá hạn chưa trả giá, giá bị sửa tay** | FR-KV-05, FR-DEP-05, 07 |
| SC-04 | Kết nối KiotViet | Retailer, Client ID, Secret, Kiểm tra, Lưu, Ngắt kết nối | FR-KV-01…07 ✅ |
| SC-05 | Sản phẩm | Tra cứu, lọc, cột Đang giảm giá (giá gốc / giá giảm), nút Đồng bộ | FR-SYNC-05, 06 |
| ~~SC-06~~ | ~~Bảng giá~~ | Bỏ ở v0.3 (bảng giá chỉ dùng cho cảnh báo trong xem trước) | — |
| SC-07 | Danh sách CTGG | Lọc trạng thái, tạo mới, nhân bản, cảnh báo quá hạn | FR-PROMO-04, 06 |
| SC-08 | Tạo/Sửa CTGG (3 bước) | Thông tin (có thời gian) → Phạm vi → Xem trước | FR-PROMO-01, 02, 03, 07 |
| SC-09 | Chi tiết CTGG ✅ | Thông tin, đổi giờ kết thúc, danh sách SP (giá gốc, giá giảm, trạng thái, lỗi), tiến độ + Huỷ, xử lý giá sửa tay, nút Sửa / Áp dụng / Huỷ lịch / Dừng / Thử lại. ⏳ Lịch sử áp/trả giá, Đối soát, Xuất giá gốc | FR-DEP-*, FR-LOG-02 |
| ~~SC-10~~ | ~~Tiến độ áp giá / trả giá~~ | Gộp vào SC-09 ở v0.3.1 | FR-DEP-10 |
| SC-11 | Nhật ký | Lọc theo thời gian, loại thao tác | FR-LOG-01 |
| SC-12 | Cài đặt | Mặc định CTGG, sao lưu/khôi phục, tự khoá, trả giá gốc khẩn cấp | FR-SET-*, FR-AUTH-05, FR-DEP-09 |
| SC-13 | Màn khoá | Nhập mật khẩu để mở khoá, Đăng xuất | FR-AUTH-05 ✅ |
| SC-14 | Kết nối Telegram ✅ | Bắt buộc lần đầu (toàn màn hình, 4 bước: Bot → Bắt đầu chat → Xác nhận → Mã dự phòng); sau đó là trang Telegram: bot, người nhận, số mã dự phòng còn lại, Kết nối lại | FR-AUTH-08, 09 |

---

## 8. Yêu cầu dữ liệu (SQLite cục bộ) **[v0.3]**

| Thực thể | Trường chính | Ghi chú |
|---|---|---|
| `UserAccount` | Username, PasswordHash, MustChangePassword, FailedLoginCount, LockedUntil, RecoveryCodeHash, **RecoveryCodeHashes** (mã dự phòng Telegram, băm), **FailedOtpCount**, **OtpLockedUntilUtc** | ✅ PasswordHash, MustChangePassword và 3 trường Telegram; chưa làm FailedLoginCount, LockedUntil, RecoveryCodeHash |
| `TelegramConnection` | EncryptedBotToken, BotUsername, ChatId, ChatTitle, ConnectedAtUtc | ✅ **[v0.3.2]** Một dòng duy nhất. Mã đăng nhập đang chờ chỉ giữ trong bộ nhớ, không lưu DB |
| `KiotVietConnection` | Retailer, ClientId, ClientSecretEncrypted, LastSyncedAt, ProductsSyncedFrom, ServerClockOffsetSec | ✅ trừ ServerClockOffsetSec |
| `ApiQuota` | WindowStart, GetCount | Bộ đếm lượt gọi (FR-KV-04) |
| `Category`, `Product` | như v0.2 | ✅ Đồng bộ |
| `PriceBook`, `PriceBookItem` | như v0.2 | ✅ Đồng bộ, chỉ dùng cảnh báo BR-06 |
| `DiscountProgram` | Name, Type, Value, Rounding, StartMode (Immediately / Scheduled), StartAtUtc (giờ hẹn; với *ngay khi áp dụng* = lúc áp giá), EndAtUtc, Scope, CategoryIds, ProductIds, ExcludedProductIds, UnitScope, Note, Status, IsStopRequested, FinishedAtUtc, CreatedAt, UpdatedAt | ✅ |
| `ProgramProductPrice` | ProgramId, ProductId, ProductCode, ProductName (chụp lại để xem cả khi SP bị xoá), **OriginalPrice** (giá gốc), DiscountedPrice, State (Pending / Applied / Failed / Restored / ChangedManually / Kept), AppliedAt, RestoredAt, LastError | ✅ Một dòng / sản phẩm / CTGG; là nguồn sự thật để trả giá |
| `PriceOperation` | ProgramId, Kind (Apply / Restore / ApplyNew), Trigger (User / Scheduled / CatchUp), StartedAt, FinishedAt, Result, SuccessCount, FailureCount | ⏳ Chưa làm (lịch sử từng lần chạy hiện chỉ có trong file log); làm cùng FR-LOG-01 |
| `AppSetting` | Key, Value | Cài đặt |
| `AuditLog` | At, Action, Target, Result, Detail | Nhật ký |

Tiền lưu dạng số nguyên VND. Thời gian lưu UTC, hiển thị UTC+7. DB bật WAL; mọi thay đổi trạng thái CTGG và giá gốc ghi trong giao dịch. Tác vụ Task Scheduler không lưu trong DB (dựng lại từ các CTGG khi cần).

---

## 9. Tích hợp KiotViet Public API **[v0.3]**

| Việc | API | Trạng thái |
|---|---|---|
| Lấy token | `POST https://id.kiotviet.vn/connect/token` (`client_credentials`, scope `PublicApi.Access`) | ✅ Đã gọi thật |
| Danh sách nhóm hàng | `GET /categories` | ✅ Đã gọi thật |
| Danh sách / chi tiết sản phẩm | `GET /products` (`includeRemoveIds`, `lastModifiedFrom`), `GET /products/{id}` | ✅ Danh sách đã gọi thật; chi tiết chưa |
| Danh sách / chi tiết bảng giá | `GET /pricebooks`, `GET /pricebooks/{id}` | ✅ Danh sách đã gọi thật (gian hàng chưa có bảng giá); chi tiết chưa |
| **Đổi giá bán một sản phẩm** | `PUT /products/{id}` body `{"basePrice": …}` | ✅ Đã gọi thật: chỉ đổi `basePrice`; dùng khi cả đợt bị từ chối |
| **Đổi giá bán nhiều sản phẩm** | `PUT /listupdatedproducts` body `{"listProducts":[{"id":…,"basePrice":…}]}` | ✅ Đã gọi thật; ⚠️ thỉnh thoảng trả 420 "Danh sách sản phẩm cập nhật rỗng" và không ghi (A10) |
| Hoá đơn (FR-RPT-01) | `GET /invoices` | Could |
| ~~Cập nhật chi tiết bảng giá~~ | ~~`POST /pricebooks/detail`~~ | Không dùng ở v0.3 |
| Webhook | `product.update` … | Không dùng (tool desktop không nhận webhook) |

Base URL: `https://public.kiotapi.com`. Header bắt buộc: `Retailer`, `Authorization: Bearer <token>`.

### 9.1 Telegram Bot API **[v0.3.2]**

Gọi `https://api.telegram.org/bot<token>/<method>` (đổi được bằng `Telegram:ApiBaseUrl`, đi qua proxy nếu có `Telegram:ProxyUrl`): `getMe` (kiểm tra token), `getUpdates` (tìm tin `/start` mới nhất trong chat riêng), `sendMessage` (gửi mã). Token nằm trong URL nên **không bao giờ ghi URL vào log**. Bot không được dùng webhook (409). Lỗi 401/404 → token sai; 403 → người dùng chặn bot / chưa bấm Bắt đầu; 429 → chờ `retry_after`.

---

## 10. Yêu cầu phi chức năng

| Mã | Nhóm | Yêu cầu |
|---|---|---|
| NFR-01 | Chính xác | Không bao giờ ghi giá giảm ≥ giá gốc hoặc ≤ 0 (BR-05, BR-07); giá giảm luôn tính từ giá gốc đã lưu (BR-12); kiểm tra lại ngay trước khi gửi từng request |
| NFR-02 | Tin cậy **[v0.3]** | Áp / trả giá lặp lại an toàn (FR-DEP-03); lỗi giữa chừng không làm hỏng dữ liệu đã ghi; tool / máy tắt đột ngột không làm mất giá gốc và việc dở dang được chạy tiếp (FR-DEP-07); không CTGG nào bị bỏ quên ở trạng thái đã hết giờ mà chưa trả giá mà không có cảnh báo |
| NFR-03 | Bảo mật | Client Secret mã hoá DPAPI; mật khẩu và mã khôi phục hash PBKDF2 (≥ 100.000 vòng, salt riêng); không ghi secret/mật khẩu/token vào log; chỉ gọi HTTPS, kiểm tra chứng chỉ |
| NFR-04 | Hiệu năng | Giao diện không đơ khi đồng bộ / áp giá (chạy nền, có tiến độ, huỷ được); xem trước 10.000 SP ≤ 2 giây; khởi động tool ≤ 5 giây; chạy nền theo lịch không hiện cửa sổ |
| NFR-05 | Giới hạn API | Tuân thủ C2, C3; tự điều tiết và thử lại theo FR-KV-04 |
| NFR-06 | Dễ dùng | Toàn bộ giao diện tiếng Việt; tiền `125.000 ₫`; ngày `dd/MM/yyyy HH:mm`; mọi thao tác đổi giá trên KiotViet đều có xem trước và xác nhận; thông báo lỗi nói rõ nguyên nhân và cách xử lý (Phụ lục B) |
| NFR-07 | Truy vết | Mọi thay đổi giá trên KiotViet đều có trong nhật ký (FR-LOG-01) kèm giá cũ / giá mới |
| NFR-08 | Triển khai | Một file `.exe` self-contained + `appsettings.json`; Windows 10/11 x64; màn hình tối thiểu 1366×768 |
| NFR-09 | Sao lưu | Toàn bộ dữ liệu nằm trong `app.db`; sao lưu theo FR-SET-02 và trước mỗi lần áp giá (FR-DEP-09) |
| NFR-10 | Nâng cấp | Phiên bản mới tự nâng cấp cấu trúc DB (migration) khi mở; tự sao lưu `app.db` trước khi migration |
| NFR-11 | Kiểm thử | Quy tắc tính giá (BR-01…BR-08, BR-12, BR-13) có bộ kiểm tra tự động phủ toàn bộ ví dụ mục 4; lớp gọi API thay được bằng bản giả lập (⚠️ repo hiện không có test project — xem CLAUDE.md) |
| NFR-12 | Đúng giờ **[v0.3]** | Khi máy bật và người dùng Windows đã đăng nhập: áp / trả giá bắt đầu trong vòng 2 phút sau mốc giờ, kể cả khi cửa sổ tool đang đóng |

---

## 11. Kịch bản nghiệm thu (UAT) **[v0.3]**

Thực hiện trên gian hàng KiotViet thử (hoặc gian hàng thật ngoài giờ bán), có ít nhất: 1 SP thường, 1 SP nhiều đơn vị, 1 SP có biến thể, 1 combo, 1 dịch vụ, 1 SP ngừng kinh doanh.

| Mã | Kịch bản | Kết quả mong đợi |
|---|---|---|
| UAT-01 | Kết nối với Client Secret sai | "Client ID hoặc Client Secret không đúng.", không lưu |
| UAT-02 | Kết nối đúng, đồng bộ lần đầu | Đủ nhóm hàng, sản phẩm; số lượng khớp KiotViet |
| UAT-03 | Tạo CTGG giảm 10% cho một nhóm hàng, xem trước | Giá khớp BR-01/BR-04; combo, dịch vụ, SP ngừng kinh doanh bị loại đúng lý do |
| UAT-04 | Áp dụng ngay CTGG giảm 10%, kết thúc sau 15 phút | Giá bán trên KiotViet giảm đúng; **các trường khác của sản phẩm không đổi** (A9); trạng thái Đang chạy |
| UAT-05 | Quét mã vạch sản phẩm trên màn bán hàng, thanh toán | Hiện giá đã giảm; hoá đơn lưu giá đã giảm; thu ngân không thấy dấu hiệu khuyến mãi |
| UAT-06 | Đóng tool, chờ qua giờ kết thúc | Tác vụ hẹn giờ tự trả giá gốc trong ≤ 2 phút; quét mã thấy giá gốc; tool hiện Đã kết thúc |
| UAT-07 | Tắt máy trước giờ kết thúc, bật lại sau giờ kết thúc | Sau khi đăng nhập Windows, giá gốc được trả; nhật ký ghi thời gian trễ |
| UAT-08 | Hẹn giờ bắt đầu sau 10 phút, đóng tool | Trước giờ: giá gốc; sau giờ ≤ 2 phút: giá giảm |
| UAT-09 | Rút mạng giữa lúc áp giá, cắm lại, thử lại | Không giảm chồng, kết quả cuối giống chạy một lần |
| UAT-10 | End Task tool giữa lúc áp giá, mở lại | Tool chạy tiếp phần dở dang; không mất giá gốc |
| UAT-11 | Dừng sớm CTGG đang chạy | Giá gốc trở lại ngay; trạng thái Đã dừng |
| UAT-12 | Tạo CTGG thứ hai trùng sản phẩm, trùng thời gian | Bị chặn, liệt kê SP và CTGG xung đột |
| UAT-13 | Sửa tay giá một SP trên KiotViet trong lúc CTGG chạy, rồi để CTGG kết thúc | Tool không ghi đè SP đó, cảnh báo; chọn "trả về giá gốc" thì ghi đúng |
| UAT-14 | Gia hạn CTGG đang chạy thêm 1 ngày | Tác vụ hẹn giờ cập nhật; giá chỉ trả khi hết giờ mới |
| UAT-15 | Hàng nhiều đơn vị: giảm 10% cả lon và thùng | Mỗi đơn vị giảm trên giá của chính nó; trả giá đúng từng đơn vị |
| UAT-16 | Nhập sai mật khẩu 5 lần; dùng mã khôi phục | Khoá 5 phút; mã khôi phục đặt lại được mật khẩu |
| UAT-17 | Sao lưu, xoá `app.db`, khôi phục | Dữ liệu CTGG, giá gốc, nhật ký đầy đủ; phải nhập lại Secret nếu khác máy |
| UAT-18 | Đặt giờ Windows lệch 10 phút | Cảnh báo lệch giờ; áp / trả giá theo giờ máy chủ |
| UAT-19 | Để tool 30 phút không thao tác trong lúc CTGG đang chạy và tới giờ kết thúc | Tool khoá màn hình nhưng vẫn trả giá đúng giờ |

---

## 12. Ngoài phạm vi (bản đầu)
- Can thiệp màn hình bán hàng KiotViet; sửa, tạo hay xoá hoá đơn.
- Ghi nhận doanh thu khác với số tiền thực thu.
- Khuyến mại theo hoá đơn: mua X tặng Y, giảm theo tổng đơn, voucher, tích điểm.
- **[v0.3]** Giảm giá khác nhau theo chi nhánh hoặc theo nhóm khách hàng (`basePrice` dùng chung — C7).
- Giảm giá theo khung giờ lặp lại (ví dụ giờ vàng 17h–19h mỗi ngày).
- Nhiều người dùng, phân quyền.
- Kết nối nhiều gian hàng KiotViet cùng lúc.
- Nhận webhook (cần server có địa chỉ public).
- **[v0.3]** Chạy tool như dịch vụ Windows 24/7 không cần người dùng đăng nhập (xem Q15).

---

## 13. Vấn đề mở (⚠️ Cần chốt)

| Mã | Câu hỏi | Đề xuất mặc định | Ảnh hưởng |
|---|---|---|---|
| **Q1** | **[v0.3.1]** A9 ✅. Còn: A11 (màn bán hàng nhận giá mới ngay, hoá đơn đúng giá giảm), A14 (Task Scheduler trên Windows thật) | Chủ cửa hàng thử trên màn bán hàng và trên Windows | FR-DEP |
| **Q2** | Phạm vi áp dụng: toàn bộ / nhóm hàng / từng sản phẩm? | Hỗ trợ cả ba + loại trừ | FR-PROMO-01 |
| **Q3** | Có chạy nhiều CTGG chồng lấn trên cùng sản phẩm không? | Không (BR-06) | BR-06 |
| **Q4** | Làm tròn thế nào? | Xuống 1.000đ | BR-04 |
| **Q5** | Xác nhận xử lý hàng nhiều đơn vị, biến thể, combo, dịch vụ theo BR-08 | Như bảng BR-08 | BR-08 |
| ~~Q6~~ | ~~Giảm theo từng chi nhánh?~~ | ✅ Chốt v0.3: áp mọi chi nhánh (C7) | — |
| ~~Q7~~ | ~~Thu ngân có phải chọn bảng giá?~~ | ✅ Không còn liên quan (v0.3 không dùng bảng giá) | — |
| ~~Q8~~ | ~~Gỡ feature Quản lý khách hàng?~~ | ✅ Đã gỡ 30/09/2026 | — |
| **Q9** | Giá bán trên KiotViet là giá trước hay sau thuế? | Giảm trên giá khách thực trả (`basePrice` hiện tại) | BR-09 |
| **Q10** | Khôi phục mật khẩu tool bằng mã khôi phục có chấp nhận được không? | Mã khôi phục | FR-AUTH-07 |
| ~~Q11~~ | ~~Chủ cửa hàng tự tạo bảng giá cho mỗi CTGG?~~ | ✅ Chốt v0.3: không — mọi thiết lập trong tool | — |
| **Q12** | Có cần báo cáo hiệu quả chương trình ngay bản đầu? | Để bản sau | FR-RPT-01 |
| **Q13** | Gian hàng đang dùng gói KiotViet nào? | ✅ Đã kết nối được API trên gian hàng `luckymart` (01/10/2026) | — |
| **Q14** | **[v0.3]** Thời điểm bắt đầu: luôn *ngay khi xác nhận* hay cần thêm *hẹn giờ*? | Có cả hai, mặc định *ngay* | FR-PROMO-01, FR-DEP-08 |
| **Q15** | **[v0.3]** Máy cài tool là máy nào, có bật và đăng nhập Windows vào giờ bắt đầu / kết thúc không? | Máy Windows chủ cửa hàng dùng hằng ngày; chấp nhận trả giá trễ khi máy tắt (có cảnh báo) | FR-DEP-07, 08, NFR-12 |
| **Q17** | **[v0.3.2]** Mạng cửa hàng có chặn Telegram không? Nếu có, dùng proxy nào? | Không chặn (máy dev gọi được). Nếu bị chặn: cấu hình `Telegram:ProxyUrl`, trong lúc chờ dùng mã dự phòng | FR-AUTH-08 |
| **Q18** | **[v0.3.2]** Mất cả điện thoại Telegram lẫn mã dự phòng thì vào lại tool thế nào? | Hiện chưa có đường khôi phục: phải xoá `app.db` (mất chương trình đã tạo) hoặc nhờ kỹ thuật sửa DB. Cân nhắc làm cùng FR-AUTH-07 | FR-AUTH-09 |
| **Q16** | **[v0.3]** Cửa hàng có đang dùng bảng giá KiotViet khác (giá sỉ, theo nhóm khách…) không? Có bảng giá nào tính theo công thức từ giá chung không? | Có thì cảnh báo trong xem trước (BR-06, A12, A13) | BR-06 |

---

## 14. Kế hoạch giai đoạn **[v0.3]**
| Giai đoạn | Nội dung | Điều kiện xong |
|---|---|---|
| 0 | Chốt mục 13; kiểm chứng A9–A11 trên sản phẩm test, điền Phụ lục A | SRS v1.0 được duyệt |
| 1 | Module KV + SYNC | ✅ Đã có; còn FR-KV-08, FR-SYNC-06, bộ đếm quota trong DB |
| 2 | Module PROMO (tạo, xem trước, xung đột) | ✅ Đã có dạng nháp theo mô hình v0.2 → **sửa theo v0.3**: bỏ bảng giá đích, thêm thời gian bắt đầu / kết thúc |
| 3 | Module DEP: áp giá, trả giá, dừng, thử lại, chạy bù, hẹn giờ Task Scheduler, bảo vệ giá gốc | ✅ Đã code và kiểm thử tự động trên `luckymart` (01/10/2026). Còn: UAT trên màn bán hàng (UAT-05) và trên Windows thật (UAT-06, 07, 08); các mục ⏳ ở 3.5 |
| 4 | Module LOG, SET, AUTH bổ sung, SC-03, hoàn thiện | Toàn bộ UAT mục 11 đạt |

---

## Phụ lục A — Biên bản kiểm chứng API **[v0.3.1]**

| Hạng mục | Cách thử | Kết quả | Ghi chú |
|---|---|---|---|
| Token OAuth | Kiểm tra kết nối trong tool | ✅ 200, ~250 ms (01/10/2026, gian hàng `luckymart`) | |
| `GET /categories`, `/products`, `/pricebooks` | Đồng bộ trong tool | ✅ 200, 30–250 ms; 10 nhóm hàng (7 nhóm con), 20 sản phẩm, 0 bảng giá | |
| `lastModifiedFrom` | Đồng bộ lần 2 | ✅ Được chấp nhận định dạng `yyyy-MM-ddTHH:mm:ss` giờ VN | |
| Giờ trả về là UTC+7 không có offset | So `modifiedDate` với giờ sửa sản phẩm | ✅ Đúng | Giờ máy chủ KiotViet khớp giờ chuẩn |
| Trường loại hàng trong danh sách sản phẩm | Đồng bộ | ✅ Có trường; mới thấy hàng thường (2), chưa có combo / dịch vụ | Tạo 1 combo, 1 dịch vụ để thử |
| `removeId` khi xoá sản phẩm | Xoá 1 SP rồi đồng bộ | Chưa thử | |
| **A9 — `PUT /products/{id}` chỉ gửi `basePrice`** | HH0001: đọc toàn bộ SP → PUT 4.400 → đọc lại → PUT 4.500 | ✅ 200 (~350 ms); chỉ `basePrice` và `modifiedDate` đổi; 25 trường khác (tên, mã vạch, nhóm, tồn kho, bảng giá, ảnh…) giữ nguyên | Body tối thiểu `{"basePrice": 4400}` |
| **A9 — `PUT /listupdatedproducts`** | HH0001 + HH0002 −200đ rồi trả lại | ✅ 200 "Cập nhật danh sách sản phẩm thành công"; chỉ `basePrice` đổi | Body `{"listProducts":[{"id":…,"basePrice":…}]}` |
| A10 — đợt nhiều sản phẩm | 8 vòng × 4 đợt × 5 SP; kiểm thử tự động ~150 lần ghi | ⚠️ ≈10–30% số đợt trả **420 `KvValidateProductException` "Danh sách sản phẩm cập nhật rỗng"** và **không ghi**; ngẫu nhiên, không phụ thuộc khoảng cách gọi (0 / 500 / 1.000 ms). Ghi từng SP (`PUT /products/{id}`) chưa lần nào lỗi. Chưa gặp 429 ở 2 request/giây | Tool tự ghi lại từng SP + đọc lại xác minh. Cỡ đợt tối đa chưa đo |
| A11 — màn bán hàng nhận giá mới | Áp giá rồi quét mã trên màn bán hàng đang mở | Chưa thử | **Chủ cửa hàng làm**: ghi độ trễ; có cần tải lại màn bán hàng không; hoá đơn có đúng giá giảm |
| A13 — thu ngân không chọn bảng giá | Bán thử khi có bảng giá khác | Không cần (gian hàng không dùng bảng giá — Q16) | |
| A14 — Task Scheduler không quyền admin | Áp dụng CTGG ngắn, đóng tool, chờ giờ kết thúc; tắt máy qua giờ kết thúc rồi bật lại | ⚠️ XML tác vụ đã kiểm tra; chế độ `--run-scheduled` đã chạy thử trên macOS (trả giá đúng, thoát mã 0); **chưa thử trên Windows thật** | UAT-06, UAT-07 |

## Phụ lục B — Danh mục thông báo lỗi

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
| E-PR-03 **[v0.3]** | Thời gian không hợp lệ | "Thời điểm kết thúc phải sau thời điểm bắt đầu ít nhất 5 phút." / "Thời điểm bắt đầu không được ở quá khứ." |
| ~~E-PR-04~~ | ~~Bảng giá không có ngày kết thúc~~ | Bỏ ở v0.3 |
| E-PR-05 | Xung đột chương trình | "N sản phẩm đang thuộc chương trình khác trong cùng thời gian. Xem danh sách." |
| E-DP-01 | Chưa xem trước sau lần sửa cuối | "Hãy xem trước giá trước khi áp dụng." |
| E-DP-02 | Một phần SP lỗi | "Đã đổi giá X/Y sản phẩm. Z sản phẩm lỗi — bấm Thử lại." |
| E-DP-03 | SP không còn trên KiotViet | "Sản phẩm đã bị xoá trên KiotViet." |
| E-DP-04 **[v0.3]** | Giá bị sửa tay trong lúc chạy | "Giá của N sản phẩm đã bị sửa trên KiotViet trong lúc chạy chương trình. Chọn giữ giá hiện tại hoặc trả về giá gốc." |
| E-DP-05 **[v0.3]** | Quá giờ kết thúc chưa trả giá | "Chương trình '<tên>' đã hết giờ lúc <giờ> nhưng chưa trả giá gốc. Tool đang trả giá…" |
| E-DP-07 **[v0.3.1]** | Sao lưu trước khi áp giá lỗi | "Không sao lưu được dữ liệu trước khi áp giá nên tool chưa đổi giá nào. Kiểm tra dung lượng ổ đĩa rồi thử lại." |
| E-DP-08 **[v0.3.1]** | Đang áp / trả giá chương trình khác | "Tool đang áp giá hoặc trả giá cho một chương trình khác. Hãy thử lại sau ít phút." |
| E-DP-06 **[v0.3]** | Không tạo được tác vụ hẹn giờ | "Không đăng ký được lịch tự động với Windows. Hãy để tool mở vào giờ kết thúc hoặc bấm Dừng thủ công." |
| E-AU-01 | Khoá do sai nhiều lần | "Đăng nhập sai quá nhiều lần. Thử lại sau M phút." |
