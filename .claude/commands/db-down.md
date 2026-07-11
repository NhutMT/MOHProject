---
description: Stop the local SQL Server container (keeps data)
---

Stop the `mohsql` container without removing it (volumes and data preserved).

1. Run `docker stop mohsql`.
2. If the container does not exist, say so — do not try to remove or recreate it.
3. Do NOT run `docker rm` unless the user explicitly asks to destroy the container.
