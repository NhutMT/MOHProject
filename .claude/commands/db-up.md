---
description: Start local SQL Server container (mohsql on port 1433)
---

Start the SQL Server Docker container used by this project.

1. Detect chip architecture (`uname -m`):
   - `arm64` → use `mcr.microsoft.com/azure-sql-edge:latest`
   - `x86_64` → use `mcr.microsoft.com/mssql/server:2022-latest`
2. Check if a container named `mohsql` already exists:
   - Running → tell the user, do nothing.
   - Stopped → `docker start mohsql`.
   - Missing → `docker run` it detached with:
     - `-e "ACCEPT_EULA=Y"`
     - `-e "MSSQL_SA_PASSWORD=Your_password123"` (or the password already in `appsettings.json`)
     - `-p 1433:1433`
     - `--name mohsql`
3. Wait a couple of seconds, then confirm with `docker ps --filter name=mohsql`.
4. Report the container status back in one line.
