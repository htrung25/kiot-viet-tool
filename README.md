# KiotVietTool

Ứng dụng desktop (Avalonia, .NET 10) cho khách dùng Windows, phát hành dưới dạng **một file `.exe` self-contained**: máy khách không cần cài .NET.
Khi phát triển, app chạy thẳng trên macOS.

## Yêu cầu môi trường

| Việc | Cần có |
|---|---|
| Dev: build / chạy / publish | .NET SDK 10.0.401+ (xem `global.json`) trên macOS, Linux hoặc Windows |
| Khách dùng | Windows 10/11 x64 |
| IDE (tuỳ chọn) | Rider hoặc VS Code + extension **Avalonia for VS Code** (có xem trước XAML) |

macOS cài SDK: `brew install --cask dotnet-sdk`


## Lệnh thường dùng

```bash
dotnet tool restore                 # cài dotnet-ef (local tool)
dotnet build                        # build toàn solution

# Chạy giao diện khi dev (macOS/Windows)
dotnet run --project src/Desktop
dotnet watch --project src/Desktop   # tự chạy lại khi sửa code

# Publish ra artifacts/publish/win-x64/KiotVietTool.exe + appsettings.json
dotnet publish src/Desktop -p:PublishProfile=win-x64
```

### EF Core migration

```bash
dotnet ef migrations add <TenMigration> \
  -p src/Infrastructure -s src/Infrastructure \
  -o Persistence/Migrations
```

App tự áp migration mỗi lần khởi động, không cần chạy `dotnet ef database update`.

## Khi dev: chạy trên macOS

- `dotnet run --project src/Desktop`: bản Debug có Avalonia DevTools, bấm **F12** để soi cây control và binding.
- Dữ liệu: `~/Library/Application Support/KiotVietTool/app.db`
- Log: `~/Library/Application Support/KiotVietTool/logs/`

## Kiểm tra bản phát hành trên Windows

1. Publish trên máy dev (lệnh ở trên), hoặc tải artifact `KiotVietTool-win-x64-*` từ tab **Actions** trên GitHub.
2. Copy **cả thư mục** `artifacts/publish/win-x64/` (gồm `KiotVietTool.exe` và `appsettings.json`) sang VM. Với Parallels hoặc UTM, dùng thư mục chia sẻ là tiện nhất.
3. Chạy `KiotVietTool.exe`. Nếu SmartScreen cảnh báo, chọn *More info → Run anyway* (vì file chưa được ký số).
4. Kiểm tra:
   - Màn **Đăng nhập** hiện ra. Đăng nhập `admin` / `admin` thì bị chuyển sang màn **Đổi mật khẩu** (bắt buộc, mật khẩu mới tối thiểu 8 ký tự).
   - Sau khi đổi mật khẩu, màn hình **Sản phẩm** hiện ra (chưa có dữ liệu cho tới khi có chức năng đồng bộ KiotViet).
   - Mở lại `.exe` lần hai: cửa sổ cũ được đưa lên trước, không mở cửa sổ mới.
   - Dữ liệu: `%LocalAppData%\KiotVietTool\app.db`
   - Log: `%LocalAppData%\KiotVietTool\logs\app-YYYYMMDD.log` (mỗi ngày một file, giữ 30 ngày)

## Đăng nhập

- Tool chỉ có **một tài khoản admin**. Lần chạy đầu tiên app tự tạo tài khoản `admin` / `admin` (lấy từ mục `Auth` trong `appsettings.json`), và bắt đổi mật khẩu ở lần đăng nhập đầu.
- Đổi mật khẩu sau này: nút **Đổi mật khẩu** trên thanh trên cùng.
- **Mã OTP qua Telegram (không bắt buộc, khuyên dùng):** *Hệ thống → Kết nối Telegram* → *Kết nối Telegram* để gắn bot của cửa hàng; từ đó mỗi lần đăng nhập, sau mật khẩu cần thêm mã 6 số bot gửi tới. Bật / tắt mã OTP hoặc ngắt kết nối ngay trên màn đó (tắt và ngắt kết nối phải nhập lại mật khẩu, sai 5 lần sẽ bị đăng xuất). Mất Telegram thì dùng một trong 10 mã dự phòng đã lưu khi kết nối.
- **Quên mật khẩu:** tắt app, xoá bảng tài khoản bằng `sqlite3 app.db "DELETE FROM UserAccounts;"` (hoặc công cụ SQLite bất kỳ). Lần chạy sau app sẽ tạo lại `admin` / `admin`. Dữ liệu khác không bị ảnh hưởng.

## Cấu hình

Sửa `appsettings.json` ngay cạnh file `.exe`, không cần build lại:

| Khoá | Ý nghĩa |
|---|---|
| `Auth:DefaultAdminUsername`, `Auth:DefaultAdminPassword` | Tài khoản admin tạo ở lần chạy đầu (chỉ khi chưa có tài khoản nào) |
| `Auth:IdleLockMinutes` | Số phút không thao tác thì tool tự khoá, phải nhập lại mật khẩu (mặc định 30, `0` = tắt) |
| `Auth:IdleWarningSeconds` | Hiện cảnh báo trước khi khoá bao nhiêu giây (mặc định 60) |
| `DiscountFeed:RequestTimeoutSeconds` | Thời gian chờ khi gửi danh sách giảm giá tới Worker. Địa chỉ Worker và mã **không** nằm ở đây: mỗi cửa hàng cấu hình trong tool (*Hệ thống → Máy thu ngân*), lưu mã hoá trong `app.db` |
| `Cloudflare:ApiBaseUrl`, `Cloudflare:CompatibilityDate` | Cloudflare API dùng khi tool tự cài Worker, và compatibility date của Worker |
| `Database:Path` | Đường dẫn file SQLite, có thể dùng biến môi trường `%LOCALAPPDATA%` |
| `Serilog:MinimumLevel:Default` | Mức log (`Debug`, `Information`, `Warning`…) |
| `Serilog:WriteTo:0:Args:path` | Nơi ghi log |

## Giảm giá tại quầy

Giá bán trên KiotViet không đổi. Khi thu ngân bấm **Thanh toán** (hoặc F9) trên KiotViet bản web, tiện ích Chrome cộng tiền giảm của các chương trình đang chạy vào ô **Giảm giá** của hoá đơn; mã QR và hoá đơn in theo số đã giảm.

```
Tool (máy chủ cửa hàng) ──PUT /v1/feed──▶ Worker `feed/` (Cloudflare KV) ◀──GET mỗi phút── tiện ích `extension/` (máy thu ngân)
```

Worker chỉ giữ danh sách "sản phẩm → mức giảm, từ … đến …"; không có Client Secret KiotViet. Tiện ích tự xét giờ (theo header `Date` của Worker), nên tool không cần bật vào giờ bắt đầu / kết thúc.

**Mỗi cửa hàng một Worker, trên tài khoản Cloudflare của chính cửa hàng.** Cài trong tool: *Hệ thống → Máy thu ngân*.

- **Tự cài lên Cloudflare (khuyên dùng):** tạo API Token tại dash.cloudflare.com → *My Profile → API Tokens → Create Token* theo mẫu **Edit Cloudflare Workers**, dán vào tool, bấm *Kiểm tra token*, chọn tài khoản, bấm *Cài lên Cloudflare*. Tool tạo KV namespace (tên = tên Worker), upload `feed/discount-feed.js` (nhúng sẵn trong exe), sinh mã ghi / mã đọc ngẫu nhiên làm secret và bật địa chỉ `https://<tên-worker>.<tên-tài-khoản>.workers.dev`. API Token chỉ dùng cho lần cài, không được lưu. Bấm lại cùng tên Worker (*Cập nhật Worker*) để đưa bản Worker mới lên mà giữ nguyên mã. Tài khoản Cloudflare mới cần mở *Workers & Pages* một lần để có tên miền workers.dev.
- **Đã có Worker:** tự deploy thư mục `feed/` rồi nhập địa chỉ và mã ghi (mã đọc tuỳ chọn) vào tool:

```bash
cd feed
npx wrangler kv namespace create FEED          # chép id vào feed/wrangler.jsonc
npx wrangler secret put WRITE_TOKEN            # chuỗi ngẫu nhiên dài, ví dụ: openssl rand -hex 32
npx wrangler secret put READ_TOKEN             # chuỗi ngẫu nhiên khác
npx wrangler deploy
```

Chạy thử trên máy: `npx wrangler dev` (đặt hai mã trong `feed/.dev.vars`); tool chấp nhận `http://localhost` làm địa chỉ Worker.

**Chống ghi đè giữa các máy:** mỗi bản cài tool có một mã định danh riêng và nhớ *revision* cuối cùng nó đã ghi. Mỗi lần gửi, tool kèm `If-Match: "<revision>"`; nếu Worker đã bị máy khác (hoặc bản sao lưu cũ của tool) cập nhật, Worker trả `412` và tool **ngừng gửi**, báo *Xung đột* ở màn Máy thu ngân. Người dùng chọn *Ghi đè bằng máy này* nếu đây là máy quản lý chính. Khi không có gì thay đổi, tool vẫn kiểm tra revision trên Worker mỗi phút để phát hiện sớm máy khác ghi đè.

**Cài tiện ích trên máy thu ngân:** Chrome → `chrome://extensions` → bật *Developer mode* → *Load unpacked* → chọn thư mục `extension/`. Bấm biểu tượng tiện ích, nhập địa chỉ Worker và **mã đọc** (xem ở *Hệ thống → Máy thu ngân*; không dùng mã ghi), bấm *Lưu*. Trên trang bán hàng, góc trái dưới hiện ô xanh "KiotViet Tool: N chương trình đang giảm giá"; ô đỏ nghĩa là tiện ích đang **không giảm giá** (chưa cấu hình, mất kết nối quá 30 phút, KiotViet đổi giao diện, hoặc cấu trúc hoá đơn không còn như tiện ích kiểm tra; khi không chắc, tiện ích không giảm thay vì giảm sai); ô vàng nghĩa là KiotViet vừa cập nhật trang bán hàng trong 24 giờ qua, nên kiểm tra vài hoá đơn đầu. Mức giảm trên 50% tiền hàng cần thu ngân bấm OK xác nhận.

Tiện ích dùng hàm nội bộ `adjustDiscount` của trang bán hàng KiotViet (AngularJS). KiotViet cập nhật giao diện có thể làm tiện ích ngừng chạy; khi đó ô góc trái chuyển đỏ.

## CI

`.github/workflows/build.yml` chạy trên `windows-latest` theo thứ tự restore → build → publish → upload artifact, mỗi khi push lên `develop`, mở PR hoặc chạy tay.

## Cấu trúc

Trong mỗi project, file được chia theo **loại** (layer-based), không theo feature.

```
src/
  Domain/          (KiotVietTool.Domain)          Entity + quy tắc nghiệp vụ, không phụ thuộc gì
    Entities/  Exceptions/
  Application/     (KiotVietTool.Application)     Use case
    Interfaces/  Services/  DTOs/  Common/ (Result, Pagination)
  Infrastructure/  (KiotVietTool.Infrastructure)  EF Core + SQLite
    Persistence/ (DbContext, DatabaseInitializer, Configurations, Migrations)
    Repositories/  Services/ (hash mật khẩu)  Options/
  Desktop/         (KiotVietTool.Desktop)         Avalonia
    Program.cs  App.axaml(.cs)            (composition root)
    Views/  ViewModels/  Models/  Services/ (Navigation, Dialog, Notification, SingleInstanceService)
    Resources/                            (Colors, Icons, Styles: design token)
```

Chi tiết kiến trúc và quy tắc code: xem [CLAUDE.md](CLAUDE.md).
