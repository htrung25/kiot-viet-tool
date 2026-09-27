# KiotVietTool

Ứng dụng desktop (Avalonia, .NET 10) cho khách dùng Windows, phát hành dưới dạng **một file `.exe` self-contained**: máy khách không cần cài .NET.
Khi phát triển, app chạy thẳng trên macOS.

## Yêu cầu môi trường

| Việc | Cần có |
|---|---|
| Dev: build / test / chạy / publish | .NET SDK 10.0.401+ (xem `global.json`) trên macOS, Linux hoặc Windows |
| Khách dùng | Windows 10/11 x64 |
| IDE (tuỳ chọn) | Rider hoặc VS Code + extension **Avalonia for VS Code** (có xem trước XAML) |

macOS cài SDK: `brew install --cask dotnet-sdk`


## Lệnh thường dùng

```bash
dotnet tool restore                 # cài dotnet-ef (local tool)
dotnet build                        # build toàn solution
dotnet test                         # chạy test (Microsoft.Testing.Platform)

# Chạy giao diện khi dev (macOS/Windows)
dotnet run --project src/KiotVietTool.Desktop
dotnet watch --project src/KiotVietTool.Desktop   # tự chạy lại khi sửa code

# Publish ra artifacts/publish/win-x64/KiotVietTool.exe + appsettings.json
dotnet publish src/KiotVietTool.Desktop -p:PublishProfile=win-x64
```

### EF Core migration

```bash
dotnet ef migrations add <TenMigration> \
  -p src/KiotVietTool.Infrastructure -s src/KiotVietTool.Infrastructure \
  -o Persistence/Migrations
```

App tự áp migration mỗi lần khởi động, không cần chạy `dotnet ef database update`.

## Khi dev: chạy trên macOS

- `dotnet run --project src/KiotVietTool.Desktop`: bản Debug có Avalonia DevTools, bấm **F12** để soi cây control và binding.
- Dữ liệu: `~/Library/Application Support/KiotVietTool/app.db`
- Log: `~/Library/Application Support/KiotVietTool/logs/`

## Kiểm tra bản phát hành trên Windows

1. Publish trên máy dev (lệnh ở trên), hoặc tải artifact `KiotVietTool-win-x64-*` từ tab **Actions** trên GitHub.
2. Copy **cả thư mục** `artifacts/publish/win-x64/` (gồm `KiotVietTool.exe` và `appsettings.json`) sang VM. Với Parallels hoặc UTM, dùng thư mục chia sẻ là tiện nhất.
3. Chạy `KiotVietTool.exe`. Nếu SmartScreen cảnh báo, chọn *More info → Run anyway* (vì file chưa được ký số).
4. Kiểm tra:
   - Màn hình **Khách hàng** hiện ra, thử Thêm, Sửa (double-click một dòng), Xoá (nút hoặc phím Delete) và Tìm.
   - Mở lại `.exe` lần hai: cửa sổ cũ được đưa lên trước, không mở cửa sổ mới.
   - Dữ liệu: `%LocalAppData%\KiotVietTool\app.db`
   - Log: `%LocalAppData%\KiotVietTool\logs\app-YYYYMMDD.log` (mỗi ngày một file, giữ 30 ngày)

## Cấu hình

Sửa `appsettings.json` ngay cạnh file `.exe`, không cần build lại:

| Khoá | Ý nghĩa |
|---|---|
| `Database:Path` | Đường dẫn file SQLite, có thể dùng biến môi trường `%LOCALAPPDATA%` |
| `Serilog:MinimumLevel:Default` | Mức log (`Debug`, `Information`, `Warning`…) |
| `Serilog:WriteTo:0:Args:path` | Nơi ghi log |

## CI

`.github/workflows/build.yml` chạy trên `windows-latest` theo thứ tự restore → build → test → publish → upload artifact, mỗi khi push lên `main`, mở PR hoặc chạy tay.

## Cấu trúc

```
src/
  KiotVietTool.Domain          Entity, quy tắc nghiệp vụ (không phụ thuộc gì)
  KiotVietTool.Application     Use case (Features/<Feature>), interface, DTO
  KiotVietTool.Infrastructure  EF Core + SQLite, repository, migration
  KiotVietTool.Desktop         Avalonia: View, ViewModel, Navigation, Dialog, composition root
tests/
  KiotVietTool.Application.Tests     Unit test (NSubstitute)
  KiotVietTool.Infrastructure.Tests  Integration test trên SQLite in-memory + migration thật
```

Chi tiết kiến trúc và quy tắc code: xem [CLAUDE.md](CLAUDE.md).
