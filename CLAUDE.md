# CLAUDE.md

Avalonia desktop app (.NET 10) shipped to customers as one self-contained Windows `KiotVietTool.exe`. Future goal: integrate the KiotViet public API.
Dev machine is macOS: the app runs there too (`dotnet run`), the release exe is tested on a Windows VM. UI text is Vietnamese.

## Commands

```bash
dotnet build                                                     # must stay at 0 warnings
dotnet run --project src/KiotVietTool.Desktop                    # launch the UI (Debug: F12 = DevTools)
dotnet publish src/KiotVietTool.Desktop -p:PublishProfile=win-x64   # → artifacts/publish/win-x64/
dotnet tool restore && dotnet ef migrations add <Name> \
  -p src/KiotVietTool.Infrastructure -s src/KiotVietTool.Infrastructure -o Persistence/Migrations
```

The repo has no test projects (removed deliberately by the owner; do not add them unless asked).
Verify with `dotnet build`, a throwaway script in your scratchpad (file-based `dotnet run x.cs` with
`#:project` + `#:property PublishAot=false`), and by launching the app and checking the log. Screen capture is not available;
ask the user to eyeball the UI. Windows-only code paths (P/Invoke to user32) are guarded by `OperatingSystem.IsWindows()`.

## Architecture (Clean Architecture)

Dependency rule: `Desktop → Infrastructure → Application → Domain`. Never reference the other way.

| Project | TFM | Contains | References |
|---|---|---|---|
| Domain | net10.0 | Entities with invariants; throw `DomainException` (Vietnamese, user-facing message) | nothing |
| Application | net10.0 | `Abstractions/` (repo interfaces), `Features/<Feature>/` (service + DTOs), `Common/Result` | Domain, DI.Abstractions |
| Infrastructure | net10.0 | `Persistence/` (AppDbContext, Configurations, Migrations, DatabaseOptions), `Repositories/` | Application, EF Core SQLite |
| Desktop | net10.0 | `Program.cs`, `App.axaml(.cs)` (composition root), Views, ViewModels, Services (Navigation, Dialog), Resources | Infrastructure, Avalonia |

Key decisions:
- **Repositories use `IDbContextFactory`**: each method is its own short-lived DbContext (no long-lived context in a desktop app). Entities returned are detached; `UpdateAsync` attaches via `Update()`.
- **Validation lives in the Domain entity**; Application services catch `DomainException` and return `Result.Failure`. Services never throw for expected business errors.
- **Time** via `TimeProvider` (registered by `AddApplication`).
- **Navigation is ViewModel-first**: `INavigationService.NavigateToAsync<TVm>(parameter)` sets `CurrentViewModel`; `MainWindow`'s `ContentControl` renders it through `Application.DataTemplates` in `App.axaml`. VMs override `ViewModelBase.OnNavigatedToAsync`.
- **Dialogs** only through async `IDialogService` (Avalonia has no MessageBox; `Views/MessageDialog` implements it).
- **Startup**: `Program.Main` does the single-instance check (named Mutex: `Local\` on Windows, `Global\` on Unix because Unix `Local\` is per process session; activation signal via named pipe) and defines `%LOCALAPPDATA%` on non-Windows. `App.OnFrameworkInitializationCompleted` registers global exception handlers (`Dispatcher.UIThread.UnhandledException`, `AppDomain`, `TaskScheduler`), builds the Generic Host (`UseContentRoot(AppContext.BaseDirectory)`, Serilog) and shows `MainWindow` from DI; on `Opened` it starts the host, runs `InitializeDatabaseAsync()` (migrate + seed default admin) and navigates to Login.
- **Auth** (single local account): `UserAccount` in SQLite, password hashed with PBKDF2-SHA256 (`Infrastructure/Auth/Pbkdf2PasswordHasher`, 600k iterations, format `pbkdf2-sha256$iter$salt$hash`). Default admin comes from `Auth:DefaultAdminUsername/Password` in appsettings and is seeded only when the table is empty, with `MustChangePassword = true`. `IUserSession` (singleton, Application) holds the signed-in user; only `IAuthService` changes it. `NavigationService` is the auth guard: not signed in → `LoginViewModel`, temporary password → `ChangePasswordViewModel`; view models reachable anonymously implement `IAllowAnonymous`.
- **Config**: `appsettings.json` is excluded from the single-file bundle and sits beside the exe. `%LOCALAPPDATA%` is expanded in `Database:Path` and in the Serilog file path; use `/` separators (works on both OSes). Serilog `"Using"` must list sink assemblies (single-file cannot scan).
- Central Package Management: versions only in `Directory.Packages.props`; `dotnet add package` writes there automatically.
- Build output goes to `artifacts/` (`ArtifactsPath`), not `bin/obj`.

## Adding a feature (follow the Customers slice)

1. `Domain/<Feature>/<Entity>.cs`: private setters, `static Create(...)`, `Update(...)`, validation → `DomainException`.
2. `Application/Abstractions/I<Entity>Repository.cs` + `Application/Features/<Feature>/` (`I<X>Service`, `<X>Service`, DTO records). Register in `Application/DependencyInjection.cs`.
3. `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`, `DbSet` in `AppDbContext`, repository in `Repositories/`, register in `Infrastructure/DependencyInjection.cs`, then add a migration.
4. Desktop: ViewModel (transient) + UserControl `.axaml` View with `x:DataType` (compiled bindings) + one `DataTemplate` line in `App.axaml` + DI registration in `App.CreateHost`.

## Code rules

- `sealed` by default, file-scoped namespaces, primary constructors where natural.
- async/await end to end with `CancellationToken`; never `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`.
- No business logic in code-behind or ViewModels; ViewModels only call Application services.
- MVVM via CommunityToolkit.Mvvm partial properties: `[ObservableProperty] public partial T X { get; set; }` and `[RelayCommand]` on `async Task XAsync(CancellationToken)`.
- No hardcoded config values; use `appsettings.json` + `IOptions<T>` (bind + validate in the layer's `DependencyInjection.cs`).
- Nullable warnings are errors (`Directory.Build.props`). Keep `dotnet build` at 0 warnings.
- Namespace gotcha: inside `KiotVietTool.*`, `Application` resolves to the `KiotVietTool.Application` namespace; write `Avalonia.Application` explicitly in Desktop.
- Avalonia: styles use selectors + classes (`Resources/Styles.axaml`); `DataGridTextColumn` needs its own `x:DataType`; no MouseBinding, so double-click is forwarded to a command in code-behind (view glue only).
- Keep `PublishTrimmed=false` (EF Core, DI and Serilog configuration rely on reflection).
