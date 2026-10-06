# Task: Giảm rủi ro mô hình giảm giá tại quầy

> Trạng thái: **T1 đã làm (chưa commit, chờ review)** · T2–T6 chưa làm
> Nguồn: phản biện 1 trong buổi review dự án (2026-10-07)

## 1. Bối cảnh

Giảm giá tại quầy hiện dựa vào hai thứ KiotViet **không công bố và không cam kết giữ ổn định**:

1. **Code nội bộ của trang bán hàng (AngularJS).** [extension/pos.js](../../extension/pos.js) tìm controller có hàm `adjustDiscount` bằng cách đi ngược lên từ `button.form-discount`, đọc giỏ hàng từ `$rootScope.activeCart` (`ProductId`, `Quantity`, `Price`, `BasePrice`, `Discount`, `DiscountByPromotion*`), và nhận diện lúc thanh toán qua `payment-invoice-component`, chữ "THANH TOÁN" / "QR", phím F9.
2. **Server KiotViet không kiểm tra quyền giảm giá.** Hoá đơn có giảm giá vẫn được lưu từ tài khoản thu ngân đã tắt quyền giảm giá; quyền này chỉ được chặn ở giao diện.

### Rủi ro

| # | Rủi ro | Hậu quả | Khả năng phát hiện hiện nay |
|---|---|---|---|
| R1 | KiotViet đổi giao diện hoặc cấu trúc, extension không tìm thấy gì | Không giảm giá, khách bị thu đủ giá | Ô đỏ góc màn hình (thu ngân có thể không để ý) |
| R2 | KiotViet đổi **ý nghĩa** field (`Price`, `Quantity`…) mà tên vẫn giữ | **Giảm sai số tiền, có thể giảm thừa** | Không phát hiện được |
| R3 | KiotViet vá lỗ hổng phân quyền ở server | Hoá đơn bị từ chối hoặc bị gỡ giảm giá ngay tại quầy | Thu ngân gặp lỗi khi thanh toán |
| R4 | Thu ngân dùng DevTools tự giảm giá | Gian lận; chủ cửa hàng tưởng đã tắt quyền là an toàn | Không phát hiện được |
| R5 | Đã lệch sẵn: tool xem trước trên `BasePrice`, extension tính trên giá trong giỏ (`Price ?? BasePrice`); có thể cộng thêm vào giảm giá theo dòng | Số giảm thực tế khác số tool hiển thị; có thể giảm hai lần | Không phát hiện được |
| R6 | Khai thác lỗ hổng phân quyền và can thiệp ứng dụng web có thể trái điều khoản sử dụng của KiotViet | KiotViet chặn hoặc khoá tài khoản API; ảnh hưởng uy tín | — |

### Mục tiêu

- Mọi sai lệch về tiền (R1, R2, R4, R5) **được phát hiện trong vòng 1 ngày**, có danh sách hoá đơn cụ thể.
- Extension **không bao giờ giảm sai một cách âm thầm**: khi không chắc thì không giảm và báo đỏ.
- Chủ cửa hàng biết được tình trạng từng quầy mà không phụ thuộc vào việc thu ngân để ý.
- Rủi ro R3 và R6 được nói rõ với khách hàng, và có sẵn phương án dự phòng.

## 2. Danh sách việc

Thứ tự đề xuất: **T1 → T2 → T6 → T4 → T3 → T5**. T1 và T2 giảm rủi ro tiền nhiều nhất.

---

### T1. Đối soát hoá đơn qua KiotViet Public API — *ưu tiên cao*

**Mục đích:** phát hiện R1, R2, R4, R5 bằng dữ liệu thật trên KiotViet, độc lập với extension.

**Cách làm:**

1. **Lưu lịch sử feed.** Mỗi lần publish thành công, lưu một bản snapshot: bảng mới `DiscountFeedSnapshots` gồm `Revision`, `PublishedAtUtc`, `ProgramsJson`.
   Không tính lại từ `DiscountProgram` hiện tại, vì phạm vi chương trình (danh mục, sản phẩm, loại trừ) có thể đã đổi kể từ lúc bán. Snapshot là thứ extension *thực sự* đã nhận.
2. **Tải hoá đơn.** Thêm `IKiotVietApiService.GetInvoicesPageAsync(from, to, currentItem)`, gọi GET `/invoices` lọc theo khoảng thời gian bán, phân trang 100. Hàm này đi qua giới hạn GET/giờ và cơ chế retry đã có.
   *Cần kiểm chứng tên tham số và field thật của API* (dự kiến: `fromPurchaseDate`, `toPurchaseDate`, `invoiceDetails[].productId/quantity/price/discount`, `discount`, `total`, `soldById`, `purchaseDate`).
3. **Tính số giảm kỳ vọng.** Với mỗi hoá đơn, chọn snapshot có hiệu lực tại `purchaseDate` (lưu ý: giờ KiotViet là UTC+7, xem `VietnamTime`), rồi tính số giảm theo **đúng thuật toán của `computeDiscount` trong [extension/discount.js](../../extension/discount.js)**.
   - Viết bản C# trong Domain/Application, ví dụ `CheckoutDiscountCalculator`.
   - Phải chạy cùng một bộ ví dụ cho cả hai phía JS và C# (parity check), vì hai bản lệch nhau sẽ cho ra cảnh báo sai.
   - Snapshot cần đủ dữ liệu để tái tạo extension. Extension dùng giờ của Worker (độ lệch trong khoảng ±1 phút), nên hoá đơn sát mốc bắt đầu/kết thúc chương trình xếp vào loại "sát mốc".
4. **Phân loại** từng hoá đơn:

   | Kết quả | Điều kiện |
   |---|---|
   | Khớp | \|thực tế − kỳ vọng\| ≤ 1 ₫ |
   | Thiếu giảm | kỳ vọng > 0, thực tế = 0 (R1, R3) |
   | Lệch | cả hai > 0 nhưng khác nhau (R2, R5) |
   | Giảm ngoài chương trình | kỳ vọng = 0, thực tế > 0 (giảm tay hợp lệ **hoặc** R4) |
   | Sát mốc | `purchaseDate` cách giờ bắt đầu/kết thúc chương trình dưới 2 phút |

5. **Chạy định kỳ:** mỗi 15 phút và khi bấm tay, đối soát các hoá đơn từ lần chạy trước trừ lùi 5 phút (giống cách đồng bộ sản phẩm). Kết quả lưu vào bảng `InvoiceReconciliations`.
6. **Giao diện:** màn mới *Đối soát giảm giá*.
   - Lọc theo ngày và theo kết quả, hiện tổng tiền chênh lệch theo ngày.
   - Bấm vào một hoá đơn để xem từng dòng: kỳ vọng, thực tế, chương trình áp dụng.
   - Có trạng thái trống và trạng thái không có kết quả.
7. **Cảnh báo:** banner trong tool khi có "Thiếu giảm" hoặc "Lệch". Gửi Telegram nếu đã kết nối (dùng chỗ để sẵn ở mục "Thông báo chương trình").

**Tiêu chí hoàn thành:**
- [ ] Hoá đơn được giảm đúng thì ra "Khớp"; tắt extension rồi bán thì ra "Thiếu giảm"; tự nhập giảm tay thì ra "Giảm ngoài chương trình".
- [ ] Parity test: tối thiểu 20 ca, kết quả của JS và C# giống nhau tuyệt đối.
- [ ] Đối soát một ngày khoảng 500 hoá đơn dùng ít hơn 10 lượt GET; không chặn UI thread.
- [ ] Tool tắt rồi bật lại vẫn đối soát bù được khoảng thời gian đã bỏ lỡ.

**Câu hỏi mở:**
- Gói KiotViet của khách có mở API `/invoices` không? Cần những scope gì?
- Có cần phân biệt chi nhánh (`branchId`) và người bán (`soldById`) không? Hữu ích cho R4, vì cho biết thu ngân nào giảm ngoài chương trình.

---

### T2. Extension "fail closed" và tự kiểm tra — *ưu tiên cao*

**Mục đích:** biến R2 (giảm sai âm thầm) thành R1 (không giảm, báo đỏ).

**Cách làm:**
1. **Kiểm tra cấu trúc trước khi ghi** (thêm hàm `validateCart` trong `discount.js`, vẫn là hàm thuần):
   - Có đúng một mảng dòng hàng. Mỗi dòng có `ProductId` là số nguyên dương, `Quantity` là số dương, `Price` là số ≥ 0.
   - Tổng `Price × Quantity` của các dòng khớp (sai số ≤ 1 ₫) với field tổng tiền hàng của giỏ (*cần xác định tên field*). Cách này bắt được trường hợp `Price` đổi ý nghĩa.
   - Bất kỳ điều kiện nào sai thì **không giảm**, báo đỏ: "Cấu trúc hoá đơn KiotViet đã thay đổi".
2. **Đọc lại sau khi ghi:** sau `adjustDiscount`, đọc lại `cart.Discount`. Nếu khác số vừa ghi thì đặt về 0, báo đỏ và ghi log.
3. **Kiểm tra `adjustDiscount`:** là hàm, số tham số như dự kiến. Ghi lại "dấu vân tay" của trang (độ dài `adjustDiscount.toString()` hoặc hash) để so giữa các lần; dấu khác đi thì báo vàng: "KiotViet vừa cập nhật, kiểm tra lại".
4. **Giảm vượt ngưỡng:** nếu số giảm > X% tổng hoá đơn (mặc định 50%, khớp ngưỡng `HighDiscountPercent` của tool), không tự áp dụng mà yêu cầu thu ngân xác nhận.

**Tiêu chí hoàn thành:**
- [ ] Test Node cho `validateCart` và các ca biên: thiếu field, đổi kiểu dữ liệu, tổng không khớp, nhiều mảng.
- [ ] Giả lập `Price` bị đổi nghĩa (ví dụ đã trừ giảm theo dòng) thì extension không giảm và hiện đỏ.

---

### T3. Heartbeat từ máy thu ngân — *ưu tiên trung bình*

**Mục đích:** chủ cửa hàng thấy tình trạng từng quầy trên tool.

**Cách làm:**
1. Extension sinh một `deviceId` (lưu trong `chrome.storage.local`) và một tên quầy do người dùng đặt ở trang tuỳ chọn.
2. Worker thêm `POST /v1/heartbeat` (chấp nhận mã đọc), giới hạn kích thước, ghi vào key `hb:<deviceId>`: phiên bản extension, revision feed đang dùng, lần áp dụng gần nhất (thành công / lỗi gì), thời điểm.
   **Ràng buộc KV free tier: 1.000 lượt ghi/ngày.** Nếu gửi mỗi phút thì một quầy đã cần 1.440 lượt/ngày. Phải gửi khi trạng thái thay đổi, cộng tối đa 1 lần mỗi 30 phút (khoảng 50 lượt/ngày/quầy). Có thể dùng Durable Object hoặc Analytics Engine nếu cần dày hơn.
3. Worker thêm `GET /v1/heartbeats` (chỉ mã ghi). Màn *Máy thu ngân* trong tool hiện danh sách quầy: xanh / đỏ / mất liên lạc quá 1 giờ.

**Tiêu chí hoàn thành:**
- [ ] Tắt Chrome ở một quầy: tool hiện "mất liên lạc" trong vòng 1 giờ.
- [ ] Ô đỏ ở quầy: tool hiện lỗi tương ứng trong vòng 1 phút.
- [ ] Một cửa hàng 5 quầy dùng < 500 lượt ghi KV/ngày.

---

### T4. Thống nhất số giảm giữa tool và quầy (R5) — *ưu tiên trung bình*

1. **Test thực tế** trên gian hàng KiotViet thử (xem T6), ghi lại `Price` / `BasePrice` / `Discount` của dòng hàng trong từng trường hợp:
   - Hàng thuộc bảng giá không phải bảng giá chung.
   - Thu ngân đã giảm giá theo dòng.
   - Hàng có khuyến mãi của KiotViet (`DiscountByPromotion`).
2. Dựa vào kết quả, chọn một trong hai:
   - Extension tính trên **giá trước giảm theo dòng** và bỏ qua dòng đã giảm tay; **hoặc**
   - Tool xem trước theo giá bảng giá khi chương trình trùng bảng giá (đã có cảnh báo `OverlappingPriceBooks`, cần hiện rõ số tiền).
3. Ghi quy tắc đã chọn vào CLAUDE.md, rồi cập nhật cả `discount.js` lẫn `DiscountProgram.Quote` (và bản C# trong T1).

**Tiêu chí hoàn thành:** với mọi trường hợp ở bước 1, số giảm trên hoá đơn khớp số tool xem trước (hoặc tool hiện rõ vì sao khác).

---

### T5. Truyền thông và phương án dự phòng (R3, R6) — *ưu tiên trung bình, không phải code*

1. **Tài liệu cho khách (tiếng Việt, ngắn):**
   - Tool giảm giá bằng cách nào.
   - "Tắt quyền giảm giá" trên KiotViet **không ngăn được** thu ngân tự giảm. Khuyến nghị xem màn *Đối soát* (T1) hằng ngày.
   - KiotViet cập nhật có thể làm tool ngừng giảm giá; khi đó ô góc màn hình sẽ đỏ.
2. **Kiểm tra điều khoản sử dụng** KiotViet và Public API. Cân nhắc liên hệ KiotViet hỏi cách chính thức để áp dụng giảm giá theo thời gian; nếu có thì bỏ được R3 và R6.
3. **Phương án dự phòng khi R3 xảy ra:**
   - Chế độ cũ "ghi giá lên KiotViet" còn trong lịch sử git (`089d228 feat(promo): apply and restore discounted prices on KiotViet`). Đánh giá xem có nên giữ thành một chế độ thay thế được bật bằng cấu hình không. Nhược điểm của chế độ này: phải sửa giá thật, và phải bật tool đúng giờ để trả giá cũ.
   - Hoặc hướng dẫn chủ cửa hàng tạm bật lại quyền giảm giá cho tài khoản thu ngân.
4. **Kill switch:** thêm cờ `disabled` trong feed (tool có nút "Tạm dừng giảm giá ở mọi quầy"). Khi có sự cố, chủ cửa hàng tắt toàn bộ trong vòng 1–2 phút mà không cần dừng từng chương trình.

**Tiêu chí hoàn thành:** tài liệu được duyệt; quyết định về chế độ dự phòng được ghi lại; kill switch chạy được trong 1–2 phút.

---

### T6. Bộ kiểm thử thủ công trên KiotViet thật — *làm trước T4, lặp lại mỗi khi KiotViet cập nhật*

Viết thành checklist `docs/tasks/checkout-manual-test.md`, chạy trên một gian hàng thử:

- [ ] Thanh toán bằng nút Thanh toán, phím F9, QR, từng phương thức thanh toán.
- [ ] Hàng nhiều đơn vị tính; combo; hàng dịch vụ; hàng ngừng kinh doanh.
- [ ] Bảng giá riêng, giảm theo dòng, khuyến mãi KiotViet (liên quan T4).
- [ ] Thu ngân nhập giảm tay trước, rồi bấm Thanh toán: extension không ghi đè.
- [ ] Thêm hoặc bớt hàng sau khi đã giảm: giảm giá được đặt lại.
- [ ] Trả hàng một phần của hoá đơn đã giảm: tiền hoàn có đúng không?
- [ ] Hoá đơn điện tử: chiết khấu thể hiện ra sao?
- [ ] Báo cáo doanh thu theo hàng hoá của KiotViet sau khi giảm cấp hoá đơn.
- [ ] Tài khoản thu ngân đã tắt quyền giảm giá: hoá đơn có lưu được không (theo dõi R3).
- [ ] Mất mạng ở quầy quá 30 phút: không giảm, ô đỏ.

## 3. Ngoài phạm vi

- Thay thế hoàn toàn cơ chế extension (cần KiotViet hỗ trợ chính thức, xem T5.2).
- Bảo mật cục bộ của tool (phản biện 2): là task riêng.

## 4. Ghi chú cho người review

- **Ưu tiên:** T1 + T2 có đủ cho bản phát hành kế tiếp không, hay cần cả T3?
- **T1.4:** ngưỡng "khớp" 1 ₫ và vùng "sát mốc" 2 phút có hợp lý không?
- **T2.4:** ngưỡng giảm 50% thì yêu cầu thu ngân xác nhận, hay chặn hẳn?
- **T5.3:** có giữ lại chế độ ghi giá lên KiotViet làm dự phòng không?
