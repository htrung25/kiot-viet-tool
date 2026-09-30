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
- **Quên mật khẩu:** tắt app, xoá bảng tài khoản bằng `sqlite3 app.db "DELETE FROM UserAccounts;"` (hoặc công cụ SQLite bất kỳ). Lần chạy sau app sẽ tạo lại `admin` / `admin`. Dữ liệu khác không bị ảnh hưởng.

## Cấu hình

Sửa `appsettings.json` ngay cạnh file `.exe`, không cần build lại:

| Khoá | Ý nghĩa |
|---|---|
| `Auth:DefaultAdminUsername`, `Auth:DefaultAdminPassword` | Tài khoản admin tạo ở lần chạy đầu (chỉ khi chưa có tài khoản nào) |
| `Database:Path` | Đường dẫn file SQLite, có thể dùng biến môi trường `%LOCALAPPDATA%` |
| `Serilog:MinimumLevel:Default` | Mức log (`Debug`, `Information`, `Warning`…) |
| `Serilog:WriteTo:0:Args:path` | Nơi ghi log |

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
    Views/  ViewModels/  Models/  Services/ (Navigation, Dialog, Notification, SingleInstance)
    Resources/                            (Colors, Icons, Styles: design token)
```

Chi tiết kiến trúc và quy tắc code: xem [CLAUDE.md](CLAUDE.md).
