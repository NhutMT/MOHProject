---
description: Run the ASP.NET Core web app
---

Run the MOHProject.Web app in Development mode.

1. Check the SQL Server container is up (`docker ps --filter name=mohsql --format '{{.Names}}'`). If not, tell the user to run `/db-up` first.
2. `cd MOHProject.Web`.
3. `dotnet run` — leave it running in the foreground.
4. Report the URL that the app printed (typically `https://localhost:5001`).

If any pending migrations are not applied, warn the user and suggest `dotnet ef database update` — do not apply them automatically.
