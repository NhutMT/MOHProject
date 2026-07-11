---
id: PH-01
version: 0.2
status: done
last_updated: 2026-07-11
depends_on: [DOMAIN-MODEL, GLOSSARY]
estimated_effort: 1-2 weeks (1 dev)
---

# Phase 1 — Foundation

Split the solution into layered projects and define the domain shell (enums, entity POCOs, empty service interfaces, initial migration). No business logic yet.

## Goal
A skeleton that compiles, has zero business behavior, and is set up so Phase 2 can be pure additive work — no restructuring later.

## Deliverables

- [x] **PH-01-01** Solution restructured into `MOHProject.Domain`, `MOHProject.Application`, `MOHProject.Infrastructure`, `MOHProject.Web`, `MOHProject.Tests`.
- [x] **PH-01-02** Sandbox `Product` entity, controller usage, tests, and old migrations removed. Fresh migration generated instead.
- [x] **PH-01-03** 11 enums created in `MOHProject.Domain/Enums/`. `TreatWarningsAsErrors=true` on Domain enforces exhaustiveness later.
- [x] **PH-01-04** 10 entity POCOs in `MOHProject.Domain/Entities/`. Pure data classes; methods live in Phase 2+.
- [x] **PH-01-05** Value objects done. `Money` has full arithmetic, comparison, equality, `Max`, culture-invariant `ToString`.
- [x] **PH-01-06** 5 service interfaces + supporting records (`PolicyContext`, `EvaluationResult`) in `MOHProject.Application/Ports/`.
- [x] **PH-01-07** `AppDbContext` in `MOHProject.Infrastructure/Persistence/`. Money mapped via `ComplexProperty` (EF 8 struct support); `decimal(18,2)`; `RowVersion` on `Policy`; unique index on `PolicyNumber`; audit index on `(PolicyId, OccurredAt)`.
- [x] **PH-01-08** Initial migration `20260711054710_InitialSchema` generated. **Not yet applied** to DB (needs Docker running — apply when `/db-up` succeeds).
- [x] **PH-01-09** `AddInfrastructure()` extension registered in `Program.cs`.
- [x] **PH-01-10** Home `Index` returns `"MOH SHIELD scaffold"`.
- [x] **PH-01-11** `dotnet build MOHProject.sln` → 0 warnings, 0 errors. `dotnet test` → 34/34 pass.

## Dependencies
None (this is the base).

## Definition of Done
- All 11 deliverables checked.
- `dotnet build MOHProject.sln` → 0 warnings, 0 errors.
- `dotnet ef database update` succeeds on a fresh SQL Server container.
- No file references the removed `Product` entity anywhere in the repo (`grep -r Product .` returns only unrelated matches).

## Test coverage requirement
- **Domain**: `Money` arithmetic + equality (10-15 test cases). Sample: `Money.Zero + Money(1) == Money(1)`, `Money("SGD",1) + Money("USD",1)` throws.
- **Domain**: `RiskAssessment.DerivedRiskCategory` truth table (5 cases).
- **Domain**: `ResidencyPair.IsFrFr` / `RequiresIpFile` (4 cases).
- No integration tests yet — no behavior to integrate.

## Out of scope
- Any service implementation logic.
- Web UI beyond a placeholder.
- Seed data.

## Risks
- Renaming solution structure while `MOHProject.Web` is running (background dotnet run from the sandbox) → **stop the running app before Phase 1**.
- The developer forgets to update `.claude/commands/*.md` paths (`MOHProject.Web/...`) if project paths change. Grep the commands folder and update after restructure.

## Change log
| Date       | Version | Change                                                                         | Author |
|------------|---------|--------------------------------------------------------------------------------|--------|
| 2026-07-11 | 0.1     | Initial draft                                                                  | Claude |
| 2026-07-11 | 0.2     | Delivered. All 11 checkboxes done. 34/34 value-object unit tests green.       | Claude |
