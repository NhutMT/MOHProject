---
name: mvc-feature
description: Add a complete MVC feature to MOHProject — entity + DbContext + migration + controller + views + nav link. Trigger when the user says "add a X page", "add CRUD for Y", "new feature to manage Z", or asks for a Controller and Views for an entity that doesn't exist yet.
---

# Add a full MVC feature end-to-end

Use this when the user asks for a whole feature (entity + UI), not just a schema change.

## Order of operations

1. **Model** — create `Models/<Entity>.cs`
2. **DbContext** — register `DbSet<Entity>` in `Data/AppDbContext.cs`
3. **Migration** — `dotnet ef migrations add Add<Entity>Table` → `database update`
4. **Controller + Views** — scaffold with `dotnet aspnet-codegenerator`
5. **Nav link** — add to `Views/Shared/_Layout.cshtml` navbar
6. **Tests** — at minimum one `Unit/` test for entity validation + one `Integration/` test for roundtrip (see `write-test` skill)
7. **Verify** — build + `dotnet run` + hit the new page + `dotnet test`

Do not skip 3 before 4 — the scaffolder reads the DbContext model, not the DB, so it works without step 3 finished, but the app will 500 at runtime if you scaffold and try to browse without applying the migration.

## Step details

### Model
- Conventions from `CLAUDE.md`: `int Id`, `[Required] [StringLength(...)]`, UTC `CreatedAt`, `decimal` mapped to `decimal(18,2)`.
- If the entity is a child (belongs to another), add both the FK (`int ParentId`) and the navigation (`public Parent? Parent { get; set; }`).

### DbContext
- Add `public DbSet<Entity> Entities => Set<Entity>();` (pluralize sensibly; ask when unsure — e.g. `Category` → `Categories`).
- Add a Fluent API block for any `decimal` columns.

### Migration
Use the `ef-code-first` workflow — everything about naming, data-loss checks, and rollback applies here.

### Scaffold
Follow the `/scaffold-crud` command. Prereq packages:
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` (project ref)
- `dotnet-aspnet-codegenerator` (global tool)

Install missing prereqs before scaffolding; do not silently skip.

Scaffolded output is a starting point — review the controller for:
- Missing `[ValidateAntiForgeryToken]` on POST actions (the template should include it).
- Overly wide `[Bind]` list — trim if the entity has fields that should not be user-editable (`CreatedAt`, computed fields, FKs the user shouldn't set directly).
- Sync EF calls — swap `.ToList()` → `.ToListAsync()`, etc. Make actions `async Task<IActionResult>`.

### Nav link
Only edit `Views/Shared/_Layout.cshtml` if the entity is user-facing. Add one `<li class="nav-item">` in the existing `navbar-nav` block. Do not restructure the layout.

### Verify
- `dotnet build` — must be clean.
- `dotnet run`.
- Open the new route (`/<Entity>`), create one row, edit it, delete it. Report any 500 or client-side validation glitch.
- Do NOT claim the feature works if you did not exercise it in the browser (or with `curl` for JSON endpoints).

## What NOT to do

- Don't add a service layer / repository just because the feature is new. Start with EF in the controller; extract only when logic actually grows.
- Don't add view models unless the entity has fields that shouldn't be exposed. Bind the entity directly to keep the diff small.
- Don't add AutoMapper, MediatR, or FluentValidation as part of "just adding a page" — those are architectural decisions, ask first.
