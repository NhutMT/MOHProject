---
name: ef-code-first
description: Full EF Core Code First workflow for MOHProject — modify entity/DbContext, generate migration, apply to SQL Server, verify. Trigger when the user asks to add/change a column, add a relationship, seed data, rename a property, or says "migration" / "update database" / "add field to X" in this project.
---

# EF Core Code First workflow

This skill drives the end-to-end Code First loop in `MOHProject.Web` so the DB schema always matches the model.

## Ground rules (always)

- Migrations are the source of truth. Never `EnsureCreated()`, never manual SQL for schema.
- One logical change = one migration. If the user asks for two unrelated changes, do two migrations.
- Never edit a migration that has already been applied to any environment (dev included, if it's been shared).
- Migration names are PascalCase and describe intent: `AddOrderDiscountColumn`, `RenameProductNameToTitle` — not `Update1`, `Fix`, `Migration`.

## Workflow

### 1. Understand the change
Ask (or infer from context):
- Entity name and property/relationship being changed.
- Whether existing rows exist that need a backfill or a default value.
- For a rename: is the old data preserved (rename column) or is it a new column?

### 2. Edit the model
- Edit `MOHProject.Web/Models/<Entity>.cs` and/or `MOHProject.Web/Data/AppDbContext.cs`.
- Follow the conventions in `CLAUDE.md`: `int Id`, `decimal(18,2)`, UTC `DateTime`, `[Required]/[StringLength]` for validation.
- For relationships:
  - **1-to-many**: add `int <Parent>Id` + `public <Parent>? <Parent> { get; set; }` on the child; add `public ICollection<<Child>> <Children> { get; set; } = new List<<Child>>();` on the parent.
  - Configure delete behavior in `OnModelCreating` if it's not `Cascade` by default.

### 3. Build first
Run `dotnet build` in `MOHProject.Web/`. If it fails, fix and re-build before touching migrations. A red build should never turn into a migration.

### 4. Generate the migration
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
cd MOHProject.Web
dotnet ef migrations add <PascalCaseName>
```

Open the generated `Migrations/<timestamp>_<Name>.cs` and read both `Up` and `Down`. Check for:
- **Data loss**: `DropColumn`, `DropTable`, `AlterColumn` narrowing a type/length. Flag to the user and ask before applying.
- **Renames**: EF often emits Drop + Add instead of Rename. Rewrite the migration to use `RenameColumn` / `RenameTable` when the intent is to preserve data.
- **Non-nullable additions to a table with rows**: add a `defaultValue:` in the migration or make the column nullable, then backfill in a follow-up migration.

### 5. Apply
```bash
dotnet ef database update
```

If it fails:
- **Connection error** → check `docker ps --filter name=mohsql`. Suggest `/db-up`.
- **Snapshot mismatch** → the `Migrations/AppDbContextModelSnapshot.cs` is out of sync. Do not hand-edit; regenerate by removing the last unapplied migration and re-adding.
- **Data conflict** (e.g. unique constraint on existing rows) → surface the SQL error and ask the user how to resolve, don't paper over it.

### 6. Verify
- Run `dotnet build` again.
- If a running app is affected, restart it — EF caches the model at startup.
- Confirm the change in the DB with a quick query if reasonable (e.g. `docker exec mohsql /opt/mssql-tools/bin/sqlcmd ... "SELECT TOP 1 * FROM <Table>"`), but don't do this by default.

## Rollback

If a just-applied migration is wrong:
```bash
dotnet ef database update <PreviousMigrationName>   # revert DB
dotnet ef migrations remove                          # delete the .cs file
```
Then edit and re-add. Never remove a migration whose changes are already in a shared/prod DB — write a new corrective migration instead.

## Seeding

For static reference data, use `modelBuilder.Entity<T>().HasData(...)` in `OnModelCreating`. Each `HasData` change requires a new migration. Do not seed dynamic/user data this way.
