# Test case kiểm thử thủ công — KiotViet Tool (theo SRS v0.3.2)

| | |
|---|---|
| Phạm vi | Đăng nhập & tự khoá, Kết nối KiotViet, Đồng bộ, Sản phẩm, Chương trình giảm giá (tạo / xem trước / lưu nháp), **Triển khai: áp giá / trả giá lên KiotViet (mục 8)** |
| Lưu ý | Mục 1–7 **không đổi giá** trên KiotViet. Mục 8 **đổi giá thật** trên gian hàng — chỉ làm trên gian hàng thử (có backup Excel). Cuối mục 8 kiểm tra mọi giá đã về giá gốc |
| Gian hàng | `luckymart` (20 sản phẩm, 10 nhóm hàng tại ngày 01/10/2026) |
| Cách ghi kết quả | Cột **KQ**: `Đạt` / `Lỗi` / `Bỏ qua`; mô tả lỗi ở cột **Ghi chú** (kèm ảnh chụp nếu được) |

---

## 0. Chuẩn bị

| Bước | Việc làm |
|---|---|
| 0.1 | Sao lưu database hiện tại: copy file `~/Library/Application Support/KiotVietTool/app.db` (macOS) hoặc `%LocalAppData%\KiotVietTool\app.db` (Windows) ra chỗ khác |
| 0.2 | Chạy tool: `dotnet run --project src/Desktop` (macOS). Lần mở đầu tiên tool tự nâng cấp database (2 migration mới) |
| 0.3 | Để test nhanh tính năng tự khoá (mục 1), sửa `appsettings.json` cạnh file chạy (`artifacts/bin/KiotVietTool.Desktop/debug/appsettings.json` khi chạy dev): `"IdleLockMinutes": 1`, `"IdleWarningSeconds": 20`. **Trả lại 30 / 60 sau khi test xong** |
| 0.4 | (Không bắt buộc, cho mục 5.3) Trên KiotViet tạo thêm: 1 **combo**, 1 **hàng dịch vụ**, 1 hàng **ngừng kinh doanh**, 1 hàng **nhiều đơn vị** (lon + thùng 24 lon), 1 hàng **không bán trực tiếp** |

**Dữ liệu tham chiếu** (giá gốc hiện tại trên `luckymart`):

| Mã | Sản phẩm | Giá gốc | Giảm 10% (làm tròn xuống 1.000đ) |
|---|---|---|---|
| HH0001 | Mì Hảo Hảo tôm chua cay gói 75g (Gói) | 4.500 | 4.000 |
| HH0002 | Thùng 24 lon nước ngọt Coca Cola 320ml (Thùng) | 234.000 | 210.000 |
| HH0003 | Lốc 6 lon nước ngọt Coca Cola Zero 320ml (Lốc) | 61.000 | 54.000 |
| HH0004 | Lốc 6 chai nước ngọt Coca Cola giảm đường 1.5L (Lốc) | 119.000 | 107.000 |
| HH0005 | Thùng 24 lon nước ngọt Sprite chanh bạc hà 320ml (Thùng) | 236.000 | 212.000 |
| HH0006 | Lốc 6 lon nước ngọt Sprite chanh bạc hà 320ml (Lốc) | 59.000 | 53.000 |

Tổng tiền giảm khi "giảm 10% toàn bộ" (mỗi SP 1 đơn vị): **274.000 ₫**.

---

## 1. Đăng nhập & tự khoá (FR-AUTH)

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-AUTH-01 | Đăng nhập sai | Nhập đúng tên, sai mật khẩu → Đăng nhập | Báo "Tên đăng nhập hoặc mật khẩu không đúng.", ô mật khẩu bị xoá | | |
| TC-AUTH-02 | Đăng nhập đúng | Nhập đúng tài khoản | Vào màn **Sản phẩm**, sidebar hiện tên tài khoản | | |
| TC-AUTH-03 | Cảnh báo trước khi khoá | (Cấu hình 0.3) Không chạm chuột/phím ~40 giây | Banner vàng trên cùng: "Tool sẽ tự khoá sau N giây…", N đếm ngược | | |
| TC-AUTH-04 | Huỷ đếm ngược | Khi banner đang hiện, di chuột | Banner biến mất, không khoá | | |
| TC-AUTH-05 | Tự khoá giữ dữ liệu dở | Mở Tạo chương trình, gõ tên "Thử khoá", để yên tới khi khoá | Hiện màn **Phiên làm việc đã khoá**, không bấm được gì phía sau | | |
| TC-AUTH-06 | Mở khoá sai | Nhập sai mật khẩu → Mở khoá | Báo "Mật khẩu không đúng. Còn 4 lần thử…" | | |
| TC-AUTH-07 | Mở khoá đúng | Nhập đúng mật khẩu | Quay lại đúng màn Tạo chương trình, tên "Thử khoá" vẫn còn | | |
| TC-AUTH-08 | Sai 5 lần | Khoá lại, nhập sai 5 lần | Bị đăng xuất về màn Đăng nhập | | |
| TC-AUTH-09 | Không khoá khi đang đồng bộ | Bấm Đồng bộ ngay rồi để yên | Không khoá trong lúc đồng bộ (đồng bộ nhanh nên khó thấy — ghi Bỏ qua nếu không quan sát được) | | |

---

## 1b. Mã đăng nhập Telegram (FR-AUTH-08…10, SRS v0.3.2)

Cần: điện thoại có Telegram. Làm theo thứ tự. Với máy đã dùng bản cũ (chưa có Telegram), đăng nhập xong tool sẽ tự chuyển sang màn kết nối Telegram.

| Mã | Tên | Các bước | Kết quả mong đợi | Kết quả | Ghi chú |
|---|---|---|---|---|---|
| TC-TG-01 | Bắt buộc kết nối | Đăng nhập (lần đầu thì đổi mật khẩu trước) | Màn **Kết nối Telegram để tiếp tục**, không có sidebar, chỉ có nút Đăng xuất | | |
| TC-TG-02 | Token sai | Dán `123:abc` → Kiểm tra bot | Báo "Bot Token không đúng hoặc đã bị thu hồi…" | | |
| TC-TG-03 | Token đúng | Trong Telegram mở @BotFather → /newbot → dán token → Kiểm tra bot | Sang bước 2, hiện `t.me/<tên bot>` | | |
| TC-TG-04 | Chưa bấm Start | Bấm **Tôi đã bấm Bắt đầu** khi chưa mở bot | Báo "Chưa thấy tin /start nào…" | | |
| TC-TG-05 | Nhận mã xác nhận | Mở bot trên điện thoại → Bắt đầu → quay lại bấm nút | Sang bước 3, hiện tên Telegram của bạn; điện thoại nhận "Mã xác nhận kết nối…" | | |
| TC-TG-06 | Sai mã xác nhận | Nhập 000000 → Xác nhận | "Mã không đúng. Còn 4 lần thử." | | |
| TC-TG-07 | Mã dự phòng | Nhập đúng mã | Bước 4: 10 mã dạng XXXX-XXXX-XXXX-XXXX; nút Hoàn tất mờ tới khi tích "Tôi đã lưu…". **Chép lại 2 mã** để dùng ở TC-TG-12 | | |
| TC-TG-08 | Hoàn tất | Tích ô → Hoàn tất | Vào màn Sản phẩm, có sidebar; toast "Đã kết nối Telegram…" | | |
| TC-TG-09 | Trang Telegram | Sidebar → Kết nối Telegram | Hiện bot, người nhận, "10/10 mã", thời gian kết nối | | |
| TC-TG-10 | Đăng nhập có mã | Đăng xuất → đăng nhập | Bước "Nhập mã xác thực"; điện thoại nhận "Mã đăng nhập…"; nhập đúng → vào tool | | |
| TC-TG-11 | Gửi lại mã | Đăng nhập → bấm Gửi lại mã ngay | Báo chờ N giây; sau 60 giây bấm lại thì nhận mã mới, mã cũ không dùng được | | |
| TC-TG-12 | Dùng mã dự phòng | Đăng nhập → Dùng mã dự phòng → nhập 1 mã đã chép | Vào tool, toast "Đã dùng 1 mã dự phòng, còn 9 mã…". Thử lại cùng mã ở lần sau → bị từ chối | | |
| TC-TG-13 | Sai 5 lần | Nhập sai mã 5 lần | Về bước mật khẩu, báo "Sai mã 5 lần…" | | |
| TC-TG-14 | Khoá 15 phút | Tiếp tục nhập sai tới tổng 10 lần | "Đăng nhập tạm khoá… 15 phút"; nhập đúng mật khẩu vẫn bị từ chối tới hết giờ | | Có thể ghi Bỏ qua |
| TC-TG-15 | Mất mạng | Tắt Wi-Fi → đăng nhập | Vẫn sang bước mã, thông báo "Không gửi được mã qua Telegram… dùng mã dự phòng"; mã dự phòng vẫn dùng được | | |
| TC-TG-16 | Khoá màn hình | Để tool tự khoá (FR-AUTH-05) | Mở khoá chỉ cần mật khẩu, không gửi mã | | |
| TC-TG-17 | Kết nối lại | Trang Telegram → Kết nối lại → làm lại 4 bước | Bộ mã dự phòng mới (mã cũ hết hiệu lực), trang Telegram hiện 10/10 | | |

---

## 2. Kết nối KiotViet (FR-KV)

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-KV-01 | Xem kết nối đã lưu | Sidebar → Kết nối KiotViet | Trạng thái **Đã kết nối**, Retailer `luckymart`, Client ID hiện, ô Secret trống với gợi ý "(để trống nếu không đổi)", thời điểm đồng bộ gần nhất | | |
| TC-KV-02 | Kiểm tra kết nối với secret đã lưu | Để trống Secret → Kiểm tra kết nối | "Kết nối thành công. Gian hàng có 20 sản phẩm." (số SP theo thực tế) | | |
| TC-KV-03 | Secret sai | Nhập Secret sai → Kiểm tra kết nối | "Client ID hoặc Client Secret không đúng." | | |
| TC-KV-04 | Retailer sai | Đổi Retailer thành `luckymart-sai` → Kiểm tra kết nối | Báo không tìm thấy / không truy cập được gian hàng | | Sau đó trả lại `luckymart` |
| TC-KV-05 | Dán cả tên miền | Retailer nhập ` https://LuckyMart.kiotviet.vn/man ` → Kiểm tra kết nối | Thành công (tool tự tách `luckymart`) | | |
| TC-KV-06 | Đổi gian hàng bị chặn | Nhập Retailer khác hẳn (tài khoản khác) → Lưu và đồng bộ | Báo phải ngắt kết nối trước khi đổi gian hàng | | Bỏ qua nếu không có gian hàng thứ hai |
| TC-KV-07 | Đồng bộ thủ công | Bấm Đồng bộ ngay | Thông báo xanh "Đã đồng bộ: … sản phẩm thay đổi…", thời điểm đồng bộ gần nhất cập nhật | | |

> Không chạy **Ngắt kết nối** trên dữ liệu thật trừ khi làm TC-REG-02.

---

## 3. Sản phẩm (FR-SYNC-05)

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-PRD-01 | Danh sách | Mở Sản phẩm | 20 sản phẩm, chân bảng "1–20 / 20 sản phẩm", giá dạng `4.500 ₫` | | |
| TC-PRD-02 | Tìm không dấu | Gõ `mi hao hao` | Ra HH0001 | | |
| TC-PRD-03 | Lọc nhóm cha | Chọn nhóm cha có nhóm con (ví dụ "Gia vị") | Ra cả sản phẩm của nhóm con | | |
| TC-PRD-04 | Không có kết quả | Gõ `zzz` | Hiện "Không tìm thấy sản phẩm" + nút Xoá bộ lọc; bấm nút thì danh sách trở lại | | |
| TC-PRD-05 | Đồng bộ thấy thay đổi | Trên KiotViet sửa giá 1 SP (ví dụ HH0001 → 5.000) → trong tool bấm Đồng bộ ngay | Giá HH0001 trong tool thành 5.000 ₫ | | **Nhớ trả giá về 4.500** trên KiotViet trước khi làm mục 4–6 |

---

## 4. Tạo chương trình — Bước 1 *Thông tin* (FR-PROMO-01)

Mở: sidebar → **Chương trình giảm giá** → **Tạo chương trình**.

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-PR1-01 | Giá trị mặc định | Mở form mới | Loại: Giảm theo %; Làm tròn: xuống 1.000 ₫; Bắt đầu: **Ngay khi áp dụng**; Kết thúc: **hôm nay + 3 ngày**, giờ làm tròn lên 5 phút; dòng "Thời lượng: 3 ngày"; hộp vàng giải thích tool đổi giá bán và trả giá gốc | | |
| TC-PR1-02 | Thiếu tên | Để trống tên, mức giảm 10 → Tiếp tục | Báo "Tên chương trình không được để trống." | | |
| TC-PR1-03 | Thiếu mức giảm | Tên "A", mức giảm trống → Tiếp tục | Báo "Nhập mức giảm %, ví dụ 10 hoặc 12,5." | | |
| TC-PR1-04 | % ngoài khoảng | Mức giảm `0`, rồi `100`, rồi `150` → Tiếp tục mỗi lần | Cả 3 lần: "Mức giảm phải lớn hơn 0 và nhỏ hơn 100%." | | |
| TC-PR1-05 | % quá 2 số lẻ | Mức giảm `12,345` | "Mức giảm % tối đa 2 chữ số thập phân." | | |
| TC-PR1-06 | % có số lẻ hợp lệ | Mức giảm `12,5` (hoặc `12.5`) | Qua bước 2 | | |
| TC-PR1-07 | Giảm theo tiền | Chọn Giảm theo số tiền, nhập `20.000` | Hậu tố đổi thành ₫, gợi ý "Số tiền giảm trên mỗi sản phẩm…"; qua bước 2 | | |
| TC-PR1-08 | Tiền quá lớn | Nhập `2.000.000.000` | "Số tiền giảm phải lớn hơn 0 và không quá 1.000.000.000 ₫." | | |
| TC-PR1-09 | Nút thời lượng nhanh | Bấm **+1 ngày**, **+7 ngày** | Ngày/giờ kết thúc đổi tương ứng; dòng thời lượng "1 ngày" / "7 ngày" | | |
| TC-PR1-10 | Kết thúc quá sớm | Đặt kết thúc = bây giờ + 2 phút → Tiếp tục | "Thời điểm kết thúc phải sau thời điểm bắt đầu ít nhất 5 phút." | | |
| TC-PR1-11 | Hẹn giờ | Chọn **Hẹn giờ** | Hiện ô chọn ngày + giờ bắt đầu (mặc định giờ tròn kế tiếp) | | |
| TC-PR1-12 | Hẹn giờ ở quá khứ | Hẹn giờ, chọn ngày hôm qua → Tiếp tục | "Thời điểm bắt đầu không được ở quá khứ." | | |
| TC-PR1-13 | Kết thúc trước bắt đầu | Hẹn bắt đầu ngày mai 08:00, kết thúc hôm nay → Tiếp tục | "Thời điểm kết thúc phải sau thời điểm bắt đầu ít nhất 5 phút." | | |
| TC-PR1-14 | Quá 366 ngày | Kết thúc sau 2 năm → Tiếp tục | "Chương trình kéo dài tối đa 366 ngày." | | |
| TC-PR1-15 | +N ngày khi hẹn giờ | Hẹn bắt đầu ngày mai 08:00 → bấm +3 ngày | Kết thúc = ngày mai + 3 ngày, 08:00 | | |

---

## 5. Tạo chương trình — Bước 2 *Phạm vi* và Bước 3 *Xem trước* (FR-PROMO-01, 02; BR-01…08)

Dùng tên khác nhau cho mỗi case (tool chặn trùng tên với chương trình đã lưu).

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-PR2-01 | **Ví dụ của chủ cửa hàng: giảm 10% toàn bộ, 3 ngày** | Tên "Giảm 10% toàn bộ", 10%, mặc định thời gian → Tiếp tục → Toàn bộ sản phẩm → Tiếp tục | Bước 3: **Được giảm giá 20**, **Bị loại 0**, **Tổng tiền giảm 274.000 ₫**; dòng "Thời gian: ngay khi áp dụng → <ngày giờ kết thúc>"; giá từng SP khớp bảng tham chiếu mục 0 | | |
| TC-PR2-02 | Cột giảm | Xem dòng HH0001 | Giá gốc 4.500 ₫, giá giảm 4.000 ₫, dòng phụ "−500 ₫ · 11,11%" | | |
| TC-PR2-03 | Làm tròn khác | Quay lại bước 1, làm tròn **Không làm tròn** → xem trước | HH0001 → 4.050 ₫; HH0002 → 210.600 ₫ | | |
| TC-PR2-04 | Giảm theo tiền, SP rẻ bị loại | 5.000 ₫, không làm tròn, toàn bộ | 19 được giảm, **1 bị loại**: HH0001 lý do "Giá sau giảm không hợp lệ" | | |
| TC-PR2-05 | Lọc Bị loại | Ở TC-PR2-04, bộ lọc **Bị loại** | Chỉ còn HH0001 | | |
| TC-PR2-06 | Cảnh báo giảm sâu | 60%, toàn bộ | Hộp vàng "20 sản phẩm được giảm từ 50% trở lên…"; bộ lọc **Có cảnh báo** ra 20 dòng, ghi chú "Giảm từ 50% trở lên" | | |
| TC-PR2-07 | Tìm trong xem trước | Gõ `sprite` | Chỉ còn các dòng Sprite | | |
| TC-PR2-08 | Theo nhóm hàng (gồm nhóm con) | Phạm vi **Theo nhóm hàng**, tick nhóm cha "Gia vị" (không tick nhóm con) | Bước 3 ra đủ sản phẩm của "Gia vị" **và các nhóm con** (10 SP tại 01/10) | | |
| TC-PR2-09 | Nhóm hàng chưa chọn | Theo nhóm hàng, không tick gì → Tiếp tục | "Chọn ít nhất một nhóm hàng." | | |
| TC-PR2-10 | Theo danh sách sản phẩm | **Theo danh sách sản phẩm**, ô tìm gõ `coca` → bấm **Thêm** 2 SP | 2 chip xuất hiện; kết quả tìm không còn 2 SP đó; bước 3 đúng 2 SP | | |
| TC-PR2-11 | Bỏ chọn sản phẩm | Bấm × trên 1 chip | Chip biến mất | | |
| TC-PR2-12 | Danh sách trống | Theo danh sách sản phẩm, không chọn gì → Tiếp tục | "Chọn ít nhất một sản phẩm." | | |
| TC-PR2-13 | Loại trừ thủ công | Toàn bộ sản phẩm, ô **Loại trừ** thêm HH0002 | Bước 3: 19 được giảm, HH0002 bị loại "Loại trừ thủ công" | | |
| TC-PR2-14 | Quay lại giữ dữ liệu | Ở bước 3 bấm Quay lại 2 lần | Bước 1, 2 giữ nguyên mọi thứ đã nhập | | |

### 5.3 Loại sản phẩm đặc biệt (chỉ khi đã làm bước chuẩn bị 0.4, rồi Đồng bộ ngay)

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-PR3-01 | Combo | Giảm 10% toàn bộ → xem dòng combo | Bị loại "Hàng combo" | | |
| TC-PR3-02 | Dịch vụ | như trên | Bị loại "Hàng dịch vụ" | | |
| TC-PR3-03 | Ngừng kinh doanh | như trên | Bị loại "Ngừng kinh doanh" | | |
| TC-PR3-04 | Không bán trực tiếp | như trên | Bị loại "Không bán trực tiếp" | | |
| TC-PR3-05 | Nhiều đơn vị — tất cả đơn vị | Giảm 10% toàn bộ | Lon và thùng **đều** được giảm, mỗi đơn vị tính trên giá của chính nó | | |
| TC-PR3-06 | Chỉ đơn vị cơ bản | Tick "Chỉ giảm giá đơn vị cơ bản" | Thùng bị loại "Không phải đơn vị cơ bản", lon vẫn giảm | | |
| TC-PR3-07 | Chọn 1 đơn vị tự kéo đơn vị khác | Theo danh sách sản phẩm, chỉ thêm **lon** | Bước 3 có cả lon và thùng | | |

---

## 6. Lưu nháp, danh sách, sửa / nhân bản / xoá, xung đột (FR-PROMO-03…07)

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-PR4-01 | Lưu nháp | Làm TC-PR2-01 → **Lưu nháp** | Thông báo xanh "Đã lưu chương trình nháp."; về danh sách | | |
| TC-PR4-02 | Hiển thị danh sách | Xem dòng vừa lưu | Tên; "Toàn bộ sản phẩm"; cột Giảm "10%"; Thời gian "Từ lúc áp dụng / Đến <ngày giờ>"; trạng thái **Nháp**; có nút Sửa, Nhân bản, Xoá | | |
| TC-PR4-03 | Trùng tên | Tạo chương trình mới tên " giảm 10% TOÀN BỘ " (khác hoa thường, có khoảng trắng) → tới bước 3 → Lưu nháp | Báo "Đã có chương trình chưa kết thúc mang tên này." | | |
| TC-PR4-04 | Nháp không chặn nhau | Tạo "Flash sale": hẹn bắt đầu **ngày mai**, kết thúc ngày kia, theo danh sách SP: HH0001, HH0002 → bước 3 → Lưu nháp | **Không** báo xung đột, lưu được (vì "Giảm 10% toàn bộ" vẫn là nháp). Xung đột thật được kiểm tra ở TC-DEP-05 | | |
| TC-PR4-05 | Cùng sản phẩm, khác thời gian | Tạo "Flash sale tháng sau": hẹn bắt đầu **sau** ngày kết thúc của "Giảm 10% toàn bộ" (ví dụ hôm nay + 5 ngày), kết thúc + 6 ngày, theo danh sách SP: HH0001, HH0002 → Lưu nháp | Không báo xung đột, lưu được; danh sách hiện "Từ <ngày giờ hẹn>" | | |
| TC-PR4-06 | Sửa nháp | Danh sách → Sửa "Giảm 10% toàn bộ" | Tiêu đề "Sửa chương trình giảm giá"; mọi trường nạp đúng (tên, 10%, làm tròn, bắt đầu ngay, ngày giờ kết thúc, phạm vi) | | |
| TC-PR4-07 | Sửa và lưu | Đổi mức giảm thành 15 → Tiếp tục → Tiếp tục → Lưu nháp | "Đã lưu thay đổi."; danh sách hiện 15% | | |
| TC-PR4-08 | Sửa chương trình hẹn giờ | Sửa chương trình hẹn giờ ở TC-PR4-05 | Ô Hẹn giờ được chọn, ngày giờ bắt đầu nạp đúng | | |
| TC-PR4-09 | Nhân bản | Danh sách → Nhân bản "Giảm 10% toàn bộ" | Mở form tạo mới, tên "Bản sao của …", bắt đầu **Ngay khi áp dụng**, kết thúc = bây giờ + thời lượng cũ (≈3 ngày); chưa lưu cho tới khi bấm Lưu nháp | | |
| TC-PR4-10 | Lọc trạng thái | Bộ lọc **Đã lên lịch** | Không còn dòng nào (mọi chương trình đều là Nháp), hiện "Không có chương trình phù hợp" + Xoá bộ lọc | | |
| TC-PR4-11 | Xoá — huỷ | Xoá một chương trình → bấm Huỷ ở hộp xác nhận | Không xoá | | |
| TC-PR4-12 | Xoá — đồng ý | Xoá → **Xoá chương trình** | Thông báo "Đã xoá chương trình.", dòng biến mất | | |
| TC-PR4-13 | Danh sách trống | Xoá hết chương trình | Hiện "Chưa có chương trình giảm giá" + nút Tạo chương trình | | |
| TC-PR4-14 | Lưu bền | Tạo 1 nháp → đóng tool → mở lại | Nháp vẫn còn, thời gian đúng (giờ Việt Nam) | | |
| TC-PR4-15 | KiotViet không đổi | Sau toàn bộ mục 4–6 (chưa bấm Áp dụng), mở KiotViet xem giá HH0001…HH0006 | **Giá không đổi** so với bảng tham chiếu | | |

---

## 7. Hồi quy & giao diện

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-REG-01 | Cửa sổ nhỏ nhất | Thu cửa sổ về nhỏ nhất (960×600), mở lần lượt Sản phẩm, Kết nối, Danh sách CTGG, 3 bước Tạo CTGG | Không mất nút / cột quan trọng; nội dung dài cuộn được | | |
| TC-REG-02 | Ngắt kết nối giữ chương trình | (Chỉ khi chấp nhận đồng bộ lại từ đầu) Có ≥1 nháp → Kết nối KiotViet → Ngắt kết nối → xác nhận | Kết nối và dữ liệu đồng bộ bị xoá; **chương trình nháp vẫn còn** trong danh sách. Sau đó nhập lại Secret và Lưu và đồng bộ | | |
| TC-REG-03 | Mở app lần 2 | Đang mở tool, chạy lệnh mở thêm lần nữa | Không mở cửa sổ mới; cửa sổ cũ được đưa lên trước | | |
| TC-REG-04 | Đổi mật khẩu | Menu tài khoản → Đổi mật khẩu, đổi rồi đăng xuất, đăng nhập bằng mật khẩu mới | Đăng nhập được bằng mật khẩu mới | | |

---

## 8. Triển khai — áp giá / trả giá trên KiotViet (FR-DEP, SRS v0.3.1)

> Các case này **đổi giá thật**. Làm lần lượt; trước khi bắt đầu, xoá hết chương trình nháp ở mục 6 (hoặc dùng tên khác).
> "Màn bán hàng" = trang bán hàng KiotViet (`<retailer>.kiotviet.vn/man` hoặc app bán hàng).

| ID | Mục đích | Các bước | Kết quả mong đợi | KQ | Ghi chú |
|---|---|---|---|---|---|
| TC-DEP-01 | **Ví dụ của chủ cửa hàng: áp ngay giảm 10% toàn bộ 3 ngày** | Tạo "Giảm 10% toàn bộ", 10%, bắt đầu ngay, kết thúc +3 ngày → bước 3 → **Lưu và áp dụng** | Mở màn chi tiết, hộp "Áp dụng chương trình giảm giá?" ghi 20 sản phẩm, giờ trả giá gốc. Bấm **Áp dụng** → thanh tiến độ → thông báo "Đã đổi giá 20 sản phẩm…"; trạng thái **Đang chạy**; bảng 20 dòng "Đang giảm giá" | | |
| TC-DEP-02 | Giá trên KiotViet | Mở KiotViet → Hàng hoá, xem HH0001…HH0006 | Giá bán = cột "Giảm 10%" ở bảng tham chiếu mục 0 (HH0001 4.000, HH0002 210.000…); các thông tin khác của hàng hoá không đổi | | |
| TC-DEP-03 | **Bán hàng: quét mã ra giá giảm (A11)** | Trên màn bán hàng: thêm HH0002 vào đơn (quét mã hoặc tìm), thanh toán | Đơn giá **210.000 ₫**; hoá đơn lưu 210.000 ₫; không thấy dấu hiệu khuyến mãi. Ghi chú: màn bán hàng đang mở có cần tải lại mới thấy giá mới không, trễ bao lâu | | |
| TC-DEP-04 | Màn Sản phẩm | Tool → Sản phẩm | Cột Giá bán hiện giá gốc (234.000 ₫), cột Đang giảm giá hiện 210.000 ₫ + tên chương trình | | |
| TC-DEP-05 | Chặn trùng khi đang chạy | Tạo chương trình mới cho HH0003, thời gian trùng → bước 3 | Hộp đỏ "1 sản phẩm đang thuộc chương trình khác…", Lưu nháp / Lưu và áp dụng bị chặn | | |
| TC-DEP-06 | Không sửa / xoá / ngắt kết nối khi đang chạy | Danh sách: chương trình Đang chạy không có nút Sửa, Xoá. Kết nối KiotViet → Ngắt kết nối | Báo "Còn chương trình đang giữ giá giảm trên KiotViet…" | | |
| TC-DEP-07 | Đổi giờ kết thúc | Màn chi tiết → Đổi kết thúc: thêm 1 ngày → **Đổi giờ kết thúc** | Thông báo "Đã đổi thời điểm kết thúc…", dòng Kết thúc cập nhật | | |
| TC-DEP-08 | Giá bị sửa tay | Trên KiotViet sửa tay giá HH0003 thành 50.000 → trong tool **Dừng chương trình** → xác nhận | Trạng thái **Đã dừng**; 19 SP về giá gốc; HH0003 vẫn **50.000** (không bị ghi đè), dòng "Giá bị sửa trên KiotViet"; hộp vàng có 2 nút | | |
| TC-DEP-09 | Trả về giá gốc cho SP sửa tay | Bấm **Trả về giá gốc** | HH0003 trên KiotViet = 61.000; hộp vàng biến mất | | |
| TC-DEP-10 | Tự kết thúc khi tool đang mở | Tạo "Thử 6 phút": 15%, bắt đầu ngay, kết thúc = bây giờ + 6 phút → Lưu và áp dụng. Để tool mở, chờ qua giờ kết thúc ~1–2 phút | Trong ~1 phút sau giờ kết thúc: thông báo "Đã tự xử lý 1 chương trình…", trạng thái **Đã kết thúc**, giá trên KiotViet về giá gốc | | |
| TC-DEP-11 | Hẹn giờ bắt đầu | Tạo "Hẹn giờ": bắt đầu = bây giờ + 10 phút, kết thúc + 20 phút → Lưu và áp dụng | Thông báo "Đã lên lịch…"; trạng thái **Đã lên lịch**, giá chưa đổi; nút **Huỷ lịch** | | |
| TC-DEP-12 | Tới giờ bắt đầu | Để tool mở, chờ qua giờ bắt đầu | Trong ~1 phút: trạng thái **Đang chạy**, giá đổi; tới giờ kết thúc: **Đã kết thúc**, giá về gốc | | |
| TC-DEP-13 | Huỷ lịch | Tạo chương trình hẹn giờ khác → Áp dụng → **Huỷ lịch** | Trạng thái về **Nháp**, sửa / xoá được; giá không đổi | | |
| TC-DEP-14 | Huỷ giữa chừng + Thử lại | Áp dụng chương trình toàn bộ; khi thanh tiến độ đang chạy bấm **Huỷ** | Trạng thái **Lỗi áp giá**, một phần SP đã đổi giá. Bấm **Thử lại** → **Đang chạy**, mọi SP đúng giá giảm (không giảm 2 lần) | | Gian hàng 20 SP ghi rất nhanh, có thể không kịp bấm — ghi Bỏ qua |
| TC-DEP-15 | Tắt tool khi đang chạy rồi mở lại sau giờ kết thúc | Chương trình kết thúc sau 6 phút → đóng tool (macOS: dev không có Task Scheduler) → chờ qua giờ kết thúc → mở tool | Ngay khi mở: trạng thái "Quá hạn — chưa trả giá" rồi chuyển **Đã kết thúc**, giá về gốc | | |
| TC-DEP-16 | **Windows: tool đóng vẫn trả giá đúng giờ (A14)** | Trên Windows: áp dụng chương trình kết thúc sau 6 phút → **đóng tool** → mở Task Scheduler, thư mục `KiotVietTool` | Có tác vụ `Program-<id>-End`. Qua giờ kết thúc ≤ 2 phút: giá trên KiotViet về gốc; mở tool thấy **Đã kết thúc**; tác vụ đã bị xoá | | Chỉ Windows |
| TC-DEP-17 | **Windows: máy tắt lúc hết giờ (A14)** | Trên Windows: áp dụng chương trình kết thúc sau 6 phút → tắt máy → bật lại sau giờ kết thúc, đăng nhập Windows (không mở tool) | Vài phút sau đăng nhập: giá về gốc (tác vụ chạy bù) | | Chỉ Windows |
| TC-DEP-18 | Không tự khoá khi đang áp giá | (Cấu hình 0.3) Áp dụng chương trình rồi để yên | Không khoá trong lúc thanh tiến độ chạy | | |
| TC-DEP-19 | Sao lưu trước khi áp giá | Mở thư mục chứa `app.db` → `backups` | Mỗi lần áp giá có 1 file `app-<ngày giờ>-apply-<id>-….db` | | |
| TC-DEP-20 | **Kết thúc: mọi giá về gốc** | Dừng mọi chương trình còn chạy → mở KiotViet | Giá HH0001…HH0020 đúng bảng giá ban đầu (bảng tham chiếu mục 0 hoặc file Excel backup) | | |

## 9. Ghi nhận sau khi test

| Hạng mục | Nội dung |
|---|---|
| Người test / ngày | |
| Môi trường | macOS dev / Windows exe — phiên bản: |
| Tổng: Đạt / Lỗi / Bỏ qua | |
| Lỗi nghiêm trọng (chặn sử dụng) | |
| Góp ý giao diện / câu chữ | |
