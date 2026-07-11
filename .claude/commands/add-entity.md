---
description: Add a new EF Core entity class and register it in AppDbContext
argument-hint: <EntityName> [property:Type ...]
---

Add a new Code First entity to the project.

Input: **$ARGUMENTS**
- First token = entity name (PascalCase, singular — e.g. `Order`, not `orders`).
- Remaining tokens (optional) = properties in the form `Name:Type` (e.g. `Total:decimal Status:string PlacedAt:DateTime`).

Steps:
1. Validate the entity name. Refuse if it already exists as a file under `MOHProject.Web/Models/`.
2. Create `MOHProject.Web/Models/<EntityName>.cs`:
   - Namespace `MOHProject.Web.Models`.
   - Always include `public int Id { get; set; }` first.
   - Then any properties given, applying conventions:
     - `string` → `[Required] [StringLength(200)] public string <Name> { get; set; } = string.Empty;` (or `string?` if the user said the property is optional).
     - `decimal` → keep as `decimal` and add Fluent API mapping (see step 3).
     - `DateTime` / `DateTime?` → as-is.
   - Add `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;` last.
3. In `MOHProject.Web/Data/AppDbContext.cs`:
   - Add `public DbSet<<EntityName>> <EntityName>s => Set<<EntityName>>();` (pluralize simply — `y` → `ies`; ask if unsure).
   - If the entity has any `decimal` property, add a Fluent API block in `OnModelCreating` mapping those to `decimal(18,2)`.
4. `dotnet build`. If it fails, report and stop.
5. Do NOT create the migration automatically — tell the user to run `/migrate Add<EntityName>Table` when they're ready.

Do not scaffold controller or views here — that's `/scaffold-crud`.
