---
description: Run tests (unit-only by default; pass "all" or "integration" to include integration tests)
argument-hint: [unit|integration|all]
---

Run the test suite.

Argument: **$ARGUMENTS** (default = `unit` if empty)

Behavior:
- `unit` (default) → `dotnet test MOHProject.Tests/MOHProject.Tests.csproj --filter "FullyQualifiedName~Unit"`
- `integration` → `dotnet test MOHProject.Tests/MOHProject.Tests.csproj --filter "FullyQualifiedName~Integration"`
- `all` → `dotnet test MOHProject.Tests/MOHProject.Tests.csproj`

Preconditions:
- For `integration` or `all`: verify Docker is available (`docker info` exits 0). If not, stop and tell the user. Do NOT try to start Docker.
- Testcontainers manages its own SQL Server — do NOT require `/db-up` and do NOT reuse the `mohsql` container. The two are independent.

After running:
- Print the pass/fail summary in one line.
- On failure, print only the failed test names + first assertion failure. Do not dump the full log.
- Do not auto-fix failing tests — surface them to the user first.
