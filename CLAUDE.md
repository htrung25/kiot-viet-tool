# CLAUDE.md

Avalonia desktop app (.NET 10) shipped to customers as one self-contained Windows `KiotVietTool.exe`. Future goal: integrate the KiotViet public API.
Dev machine is macOS: the app runs there too (`dotnet run`), the release exe is tested on a Windows VM. UI text is Vietnamese.
Business requirements live in `docs/SRS_v2.md` (time-bound % / VND discounts pushed to KiotViet price books). Read it before building features; its "Vấn đề mở" table lists decisions still pending.

## Commands

```bash
dotnet build                                                     # must stay at 0 warnings
dotnet run --project src/Desktop                    # launch the UI (Debug: F12 = DevTools)
dotnet publish src/Desktop -p:PublishProfile=win-x64   # → artifacts/publish/win-x64/
dotnet tool restore && dotnet ef migrations add <Name> \
  -p src/Infrastructure -s src/Infrastructure -o Persistence/Migrations
```

The repo has no test projects (removed deliberately by the owner; do not add them unless asked).
Verify with `dotnet build`, a throwaway script in your scratchpad (file-based `dotnet run x.cs` with
`#:project` + `#:property PublishAot=false`), and by launching the app and checking the log. Screen capture is not available,
so verify UI by rendering headlessly (see "Verify UI visually" below). Windows-only code paths (P/Invoke to user32) are guarded by `OperatingSystem.IsWindows()`.

## Architecture (Clean Architecture)

Dependency rule: `Desktop → Infrastructure → Application → Domain`. Never reference the other way.
Folders are `src/<Layer>/` (short names); project files, assembly names and namespaces keep the `KiotVietTool.<Layer>` prefix.

| Project | TFM | Contains | References |
|---|---|---|---|
| Domain | net10.0 | `Entities/` (entities with invariants), `Exceptions/` (`DomainException`: Vietnamese, user-facing message) | nothing |
| Application | net10.0 | `Interfaces/` (service + port interfaces: repositories, hasher, session), `Services/` (implementations), `DTOs/`, `Common/` (Result, Pagination) | Domain, DI.Abstractions, Logging.Abstractions |
| Infrastructure | net10.0 | `Persistence/` (AppDbContext, DatabaseInitializer, DesignTimeDbContextFactory, `Configurations/`, `Migrations/`), `Repositories/`, `Services/` (e.g. Pbkdf2PasswordHasher), `Options/` (DatabaseOptions, AuthOptions) | Application, EF Core SQLite |
| Desktop | net10.0 | `Program.cs`, `App.axaml(.cs)` (composition root), `Views/` (windows, pages, dialogs), `ViewModels/` (incl. `ViewModelBase`, `MainViewModel`), `Models/` (display models, filters, enums), `Services/` (Navigation, Dialog, Notification, SingleInstance), `Resources/` (Colors, Icons, Styles) | Infrastructure, Avalonia |

Key decisions:
- **Repositories use `IDbContextFactory`**: each method is its own short-lived DbContext (no long-lived context in a desktop app). Entities returned are detached; `UpdateAsync` attaches via `Update()`.
- **Validation lives in the Domain entity**; Application services catch `DomainException` and return `Result.Failure`. Services never throw for expected business errors.
- **Time** via `TimeProvider` (registered by `AddApplication`).
- **Navigation is ViewModel-first**: `INavigationService.NavigateToAsync<TVm>(parameter)` sets `CurrentViewModel`; `MainWindow`'s `ContentControl` renders it through `Application.DataTemplates` in `App.axaml`. VMs override `ViewModelBase.OnNavigatedToAsync`. Screens never navigate to an unrelated screen by type just to "go home": use `NavigateHomeAsync()`; which screen is Login / ChangePassword / Home is declared once as `NavigationRoutes` in `App.CreateHost`.
- **Dialogs** only through async `IDialogService` (Avalonia has no MessageBox; `Views/MessageDialog` implements it). Confirm with a question title, the consequence as message and a verb on the button (`ConfirmAsync("Dừng chương trình?", "...", "Dừng chương trình", destructive: true)`), never a bare "OK".
- **Success feedback** is a toast via `INotificationService.ShowSuccess` (non-blocking); errors that need attention use a dialog or an inline `Border.alert.error`.
- **Startup**: `Program.Main` does the single-instance check (named Mutex: `Local\` on Windows, `Global\` on Unix because Unix `Local\` is per process session; activation signal via named pipe) and defines `%LOCALAPPDATA%` on non-Windows. `App.OnFrameworkInitializationCompleted` registers global exception handlers (`Dispatcher.UIThread.UnhandledException`, `AppDomain`, `TaskScheduler`), builds the Generic Host (`UseContentRoot(AppContext.BaseDirectory)`, Serilog) and shows `MainWindow` from DI; on `Opened` it starts the host, runs `InitializeDatabaseAsync()` (→ `Persistence/DatabaseInitializer`: migrate + seed default admin) and navigates to Login.
- **Auth** (single local account): `UserAccount` in SQLite, password hashed with PBKDF2-SHA256 (`Infrastructure/Services/Pbkdf2PasswordHasher`, 600k iterations, format `v1.<iterations>.<salt>.<hash>` Base64; legacy `pbkdf2-sha256$...` still verifies). `Verify` trusts nothing from the stored string: known version, exact 16-byte salt, exact 32-byte hash (output length is a constant), iterations 100k–10M; anything else is rejected and logged as a warning (never log the hash). `NeedsRehash` is true for legacy/older version/fewer iterations; `AuthService.SignInAsync` then re-hashes the password best-effort via `UserAccount.UpgradePasswordHash` (keeps `MustChangePassword`; a failed save never blocks sign-in). To strengthen hashing later: raise `Iterations`, or add a `v2` with its own sizes and keep parsing `v1`. Default admin comes from `Auth:DefaultAdminUsername/Password` in appsettings and is seeded only when the table is empty, with `MustChangePassword = true`. `IUserSession` (singleton, Application) holds the signed-in user; only `IAuthService` changes it. `NavigationService` is the auth guard: not signed in → `LoginViewModel`, temporary password → `ChangePasswordViewModel`; view models reachable anonymously implement `IAllowAnonymous`.
- **Config**: `appsettings.json` is excluded from the single-file bundle and sits beside the exe. `%LOCALAPPDATA%` is expanded in `Database:Path` and in the Serilog file path; use `/` separators (works on both OSes). Serilog `"Using"` must list sink assemblies (single-file cannot scan).
- Central Package Management: versions only in `Directory.Packages.props`; `dotnet add package` writes there automatically.
- Build output goes to `artifacts/` (`ArtifactsPath`), not `bin/obj`.

## Adding a feature (backend: follow Auth — `UserAccount` → `AuthService` → `UserAccountRepository`; UI: follow the product screen)

1. `Domain/Entities/<Entity>.cs`: private setters, `static Create(...)`, `Update(...)`, validation → `DomainException`.
2. Application: `Interfaces/I<Entity>Repository.cs`, `Interfaces/I<X>Service.cs`, `Services/<X>Service.cs`, one file per DTO in `DTOs/`. Register in `Application/DependencyInjection.cs`.
3. `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`, `DbSet` in `AppDbContext`, repository in `Infrastructure/Repositories/`, register in `Infrastructure/DependencyInjection.cs`, then add a migration.
4. Desktop: `Views/<X>View.axaml(.cs)` + `ViewModels/<X>ViewModel.cs` (transient, `x:DataType` compiled bindings) + display models in `Models/` + one `DataTemplate` line in `App.axaml` (`vm:` → `views:`) + DI registration in `App.CreateHost`.

## Code rules

- **Folder layout rules (layer-based)**: inside each project, files are grouped by kind (`Entities/`, `Interfaces/`, `Services/`, `DTOs/`, `Repositories/`, `Options/`, `Views/`, `ViewModels/`, `Models/`...), not by feature. Namespace = folder path. One main type per file, file name = type name. `Common/` only for cross-cutting building blocks (Result, Pagination).
- `sealed` by default, file-scoped namespaces, primary constructors where natural.
- async/await end to end with `CancellationToken`; never `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`.
- No business logic in code-behind or ViewModels; ViewModels only call Application services.
- MVVM via CommunityToolkit.Mvvm partial properties: `[ObservableProperty] public partial T X { get; set; }` and `[RelayCommand]` on `async Task XAsync(CancellationToken)`.
- No hardcoded config values; use `appsettings.json` + `IOptions<T>` (bind + validate in the layer's `DependencyInjection.cs`).
- Nullable warnings are errors (`Directory.Build.props`). Keep `dotnet build` at 0 warnings; unused usings (IDE0005) and broken doc `cref`s are reported, fix with `dotnet format style KiotVietTool.slnx --diagnostics IDE0005 --exclude src/Infrastructure/Persistence/Migrations`.
- Namespace gotcha: inside `KiotVietTool.*`, `Application` resolves to the `KiotVietTool.Application` namespace; write `Avalonia.Application` explicitly in Desktop.
- **UI design system**: colors only from `Resources/Colors.axaml` tokens (`{StaticResource Text.Muted}`, `Primary`, `Danger`...), never hex in views; icons are `PathIcon` with `Icon.*` geometries from `Resources/Icons.axaml` (Material Design Icons). Style classes in `Resources/Styles.axaml`: buttons `accent` (primary, Fluent), `secondary`, `danger`, `ghost`, `icon`; text `h1`, `h2`, `subtitle`, `caption`, `label`; layout `Border.card`, `Border.divider`, `Border.alert.error|warning`, `StackPanel.field` (label above input), `Grid.page` (page margins). Pages inside the shell = header (h1 + subtitle + primary action on the right) then a card; lists need an empty state and a no-results state. `ShowChrome` in `MainViewModel` hides the sidebar for full-screen flows (login, forced password change).
- **Verify UI visually**: render screens headlessly (Avalonia.Headless + Skia, `CaptureRenderedFrame()`) from a throwaway scratchpad script and inspect the PNGs at 1200×760 and the 960×600 minimum; don't ship layout changes unseen.
- Avalonia: styles use selectors + classes (`Resources/Styles.axaml`); a selector list must target one control type (no `TextBlock, PathIcon` sharing a setter); `DataGridTextColumn` needs its own `x:DataType`; no MouseBinding, so double-click is forwarded to a command in code-behind (view glue only).
- Keep `PublishTrimmed=false` (EF Core, DI and Serilog configuration rely on reflection).
