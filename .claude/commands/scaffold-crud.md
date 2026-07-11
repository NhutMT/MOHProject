---
description: Scaffold controller + Razor views (Index/Create/Edit/Details/Delete) for an entity
argument-hint: <EntityName>
---

Scaffold CRUD for an existing entity.

Entity: **$ARGUMENTS**

Prereqs:
1. `MOHProject.Web/Models/<EntityName>.cs` exists.
2. `AppDbContext` has `DbSet<<EntityName>>`.
3. The `Microsoft.VisualStudio.Web.CodeGeneration.Design` package is referenced. If missing, add it: `dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design --version 8.0.*`.
4. `dotnet-aspnet-codegenerator` global tool installed. If missing: `dotnet tool install --global dotnet-aspnet-codegenerator --version 8.0.*`.

Steps:
1. `cd MOHProject.Web`.
2. Run:
   ```
   dotnet aspnet-codegenerator controller \
     -name <EntityName>Controller \
     -m MOHProject.Web.Models.<EntityName> \
     -dc MOHProject.Web.Data.AppDbContext \
     --relativeFolderPath Controllers \
     --useDefaultLayout \
     --referenceScriptLibraries
   ```
3. Verify these files were created:
   - `Controllers/<EntityName>Controller.cs`
   - `Views/<EntityName>/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `Delete.cshtml`
4. `dotnet build`.
5. Add a link to the new Index in `Views/Shared/_Layout.cshtml` navbar only if the user asks. Otherwise report the URL (`/<EntityName>`) and stop.

Refuse to overwrite an existing controller — ask the user first.
