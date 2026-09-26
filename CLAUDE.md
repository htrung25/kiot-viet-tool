# CLAUDE.md

WPF desktop app (.NET 10) shipped as one self-contained `KiotVietTool.exe`. Future goal: integrate the KiotViet public API.
Dev machine is macOS (build/test/publish only); the app runs on a Windows VM. UI text is Vietnamese.

## Commands

```bash
dotnet build                                                     # must stay at 0 warnings
dotnet test                                                      # MTP runner (see global.json), runs on macOS
dotnet publish src/KiotVietTool.Desktop -p:PublishProfile=win-x64   # → artifacts/publish/win-x64/
dotnet tool restore && dotnet ef migrations add <Name> \
  -p src/KiotVietTool.Infrastructure -s src/KiotVietTool.Infrastructure -o Persistence/Migrations
```

You cannot launch the WPF app on macOS. Verify by build + tests; ask the user to test UI on Windows.

## Architecture (Clean Architecture)

Dependency rule: `Desktop → Infrastructure → Application → Domain`. Never reference the other way.

| Project | TFM | Contains | References |
|---|---|---|---|
| Domain | net10.0 | Entities with invariants; throw `DomainException` (Vietnamese, user-facing message) | nothing |
| Application | net10.0 | `Abstractions/` (repo interfaces), `Features/<Feature>/` (service + DTOs), `Common/Result` | Domain, DI.Abstractions |
| Infrastructure | net10.0 | `Persistence/` (AppDbContext, Configurations, Migrations, DatabaseOptions), `Repositories/` | Application, EF Core SQLite |
| Desktop | net10.0-windows | `App.xaml.cs` (composition root), Views, ViewModels, Services (Navigation, Dialog), Resources | Infrastructure |

Key decisions:
- **Repositories use `IDbContextFactory`**: each method is its own short-lived DbContext (no long-lived context in a desktop app). Entities returned are detached; `UpdateAsync` attaches via `Update()`.
- **Validation lives in the Domain entity**; Application services catch `DomainException` and return `Result.Failure`. Services never throw for expected business errors.
- **Time** via `TimeProvider` (mock with NSubstitute in tests).
- **Navigation is ViewModel-first**: `INavigationService.NavigateToAsync<TVm>(parameter)` sets `CurrentViewModel`; `MainWindow`'s `ContentControl` renders it through `Resources/DataTemplates.xaml`. VMs override `ViewModelBase.OnNavigatedToAsync`.
- **Dialogs** only through `IDialogService` (never `MessageBox` in a ViewModel).
- **Startup** (`App.OnStartup`): single-instance check (named Mutex + EventWaitHandle) → global exception handlers → Generic Host (`UseContentRoot(AppContext.BaseDirectory)`, Serilog) → `MigrateDatabaseAsync()` → show `MainWindow` from DI → navigate to the customer list.
- **Config**: `appsettings.json` is excluded from the single-file bundle and sits beside the exe. `%LOCALAPPDATA%` is expanded in `Database:Path` and in the Serilog file path. Serilog `"Using"` must list sink assemblies (single-file cannot scan).
- Central Package Management: versions only in `Directory.Packages.props`; `dotnet add package` writes there automatically.
- Build output goes to `artifacts/` (`ArtifactsPath`), not `bin/obj`.

## Adding a feature (follow the Customers slice)

1. `Domain/<Feature>/<Entity>.cs`: private setters, `static Create(...)`, `Update(...)`, validation → `DomainException`.
2. `Application/Abstractions/I<Entity>Repository.cs` + `Application/Features/<Feature>/` (`I<X>Service`, `<X>Service`, DTO records). Register in `Application/DependencyInjection.cs`.
3. `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`, `DbSet` in `AppDbContext`, repository in `Repositories/`, register in `Infrastructure/DependencyInjection.cs`, then add a migration.
4. Desktop: ViewModel (transient) + UserControl View + one `DataTemplate` line + DI registration in `App.CreateHost`.
5. Tests: service tests with NSubstitute in `Application.Tests`; repository tests on `SqliteTestDatabase` in `Infrastructure.Tests`.

## Code rules

- `sealed` by default, file-scoped namespaces, primary constructors where natural.
- async/await end to end with `CancellationToken`; never `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`.
- No business logic in code-behind or ViewModels; ViewModels only call Application services.
- MVVM via CommunityToolkit.Mvvm partial properties: `[ObservableProperty] public partial T X { get; set; }` and `[RelayCommand]` on `async Task XAsync(CancellationToken)`.
- No hardcoded config values; use `appsettings.json` + `IOptions<T>` (bind + validate in the layer's `DependencyInjection.cs`).
- Nullable warnings are errors (`Directory.Build.props`). Keep `dotnet build` at 0 warnings.
- Tests: xUnit v3 + AwesomeAssertions (not FluentAssertions, license) + NSubstitute; pass `TestContext.Current.CancellationToken`.
- Namespace gotcha: inside `KiotVietTool.*`, `Application` resolves to the `KiotVietTool.Application` namespace; write `System.Windows.Application` explicitly in Desktop.
- WPF does not support trimming; keep `PublishTrimmed=false`.
