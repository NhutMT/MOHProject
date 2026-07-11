---
description: Create a new EF Core migration and apply it to the local database
argument-hint: <MigrationName>
---

Create and apply a new EF Core migration.

Migration name: **$ARGUMENTS**

Steps:
1. Validate the name is PascalCase and describes intent (e.g. `AddProductStockColumn`). Refuse names like `Update1`, `Fix`, `Migration`.
2. Ensure `dotnet-ef` is available: `export PATH="$PATH:$HOME/.dotnet/tools"`.
3. `cd MOHProject.Web`.
4. `dotnet build` first — if build fails, stop and report the error; do NOT create a migration on a broken build.
5. `dotnet ef migrations add $ARGUMENTS`.
6. Show the generated `Up`/`Down` methods from the new migration file and ask the user to confirm before applying, if the migration drops columns or tables. Otherwise proceed.
7. `dotnet ef database update`.
8. Report success with the migration filename.

If the SQL Server container is not running, tell the user to run `/db-up` first — do not silently start it.
