# KiotVietTool

Ứng dụng desktop Windows (WPF, .NET 10), phát hành dưới dạng **một file `.exe` self-contained**: máy khách không cần cài .NET.

## Yêu cầu môi trường

| Việc | Cần có |
|---|---|
| Build / test / publish | .NET SDK 10.0.401+ (xem `global.json`), chạy được trên macOS, Linux, Windows |
| Chạy ứng dụng | Windows 10/11 x64 (máy thật hoặc VM) |
| IDE (tuỳ chọn) | Rider, Visual Studio 2026, VS Code + C# Dev Kit |

macOS cài SDK: `brew install --cask dotnet-sdk`

> Project WPF (`net10.0-windows`) **build được** trên macOS nhờ `EnableWindowsTargeting`, nhưng **chỉ chạy được trên Windows**.
> Các project còn lại dùng `net10.0`, nên toàn bộ test chạy được trên macOS.

## Lệnh thường dùng

```bash
dotnet tool restore                 # cài dotnet-ef (local tool)
dotnet build                        # build toàn solution
dotnet test                         # chạy test (Microsoft.Testing.Platform)

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

## Chạy trên Windows VM

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
  KiotVietTool.Desktop         WPF: View, ViewModel, Navigation, Dialog, composition root
tests/
  KiotVietTool.Application.Tests     Unit test (NSubstitute)
  KiotVietTool.Infrastructure.Tests  Integration test trên SQLite in-memory + migration thật
```

Chi tiết kiến trúc và quy tắc code: xem [CLAUDE.md](CLAUDE.md).
