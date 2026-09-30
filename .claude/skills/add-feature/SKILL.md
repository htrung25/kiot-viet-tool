---
name: add-feature
description: Use when adding a new feature, entity, screen or service to KiotVietTool — lists the files to create in each Clean Architecture layer and where to register them.
---

# Adding a feature

Backend: follow Auth (`UserAccount` → `AuthService` → `UserAccountRepository`). UI: follow the product screen.

1. `Domain/Entities/<Entity>.cs`: private setters, `static Create(...)`, `Update(...)`, validation → `DomainException`.
2. Application: `Interfaces/I<Entity>Repository.cs`, `Interfaces/I<X>Service.cs`, `Services/<X>Service.cs`, one file per DTO in `DTOs/`. Register in `Application/DependencyInjection.cs`.
3. `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`, `DbSet` in `AppDbContext`, repository in `Infrastructure/Repositories/`, register in `Infrastructure/DependencyInjection.cs`, then add a migration.
4. Desktop: `Views/<X>View.axaml(.cs)` + `ViewModels/<X>ViewModel.cs` (transient, `x:DataType` compiled bindings) + display models in `Models/` + one `DataTemplate` line in `App.axaml` (`vm:` → `views:`) + DI registration in `App.CreateHost`.
