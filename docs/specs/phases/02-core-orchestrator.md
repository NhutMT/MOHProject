---
id: PH-02
version: 0.2
status: done
last_updated: 2026-07-11
depends_on: [PH-01, DOMAIN-MODEL]
estimated_effort: 2 weeks (1 dev)
---

# Phase 2 — Core Orchestrator (MVP)

Implement `EvaluateRemainingPlans()` and its 4 sub-evaluators. This is the missing service that fixes all 4 UAT bugs (see [BUG-uat-2026-06](../bugs/uat-2026-06.md)) — it is the highest-value slice and should be delivered before any UI work.

## Goal
A pure domain service that, given a `Policy` and a `PolicyContext`, returns:
- `RiskComposition` of the remaining active plans
- Updated `UWState` (RCMP flag / AcceptCloa / RcmpOption / CompleteUw)
- Next `PolicySubstatus`
- `LetterType?` to generate + whether Ack page is needed

The service has no I/O, no EF, no async — it is a pure function of its inputs. All 8 minimum unit tests from the source doc pass, plus 4 UAT bug regressions.

## Deliverables

- [ ] **PH-02-01** `IPlansCompositionEvaluator` + impl. Truth table exactly matches source lines 279-307 and 1901-1921. See [FR-AOR-030](../sections/section-1-addition-of-riders.md#fr-aor-030).
- [ ] **PH-02-02** `IUwFieldStatesEvaluator` + impl. Rules for RCMP Flag / AcceptCloa / RcmpOption / CompleteUw per composition, per §Enhancement 3 table. Handles the Renewal + Extra Loading + CGR/Choice retention exception (source lines 451-457).
- [ ] **PH-02-03** `INextSubstatusEvaluator` + impl. Truth table: composition × residency × shortfall × AcceptCloa → `PolicySubstatus`. Matches sub-evaluator 4 code (source lines 1923-1948).
- [ ] **PH-02-04** `ILetterTypeEvaluator` + impl. Returns `(LetterType?, bool hasAck)`. Handles Standard → LOA, ExclusionOnly → CLOA(Exclusion), HasRcmp → CLOA(RCMP), and the substatus-gated skip (source line 271: only generate at `CondAccept`, `PendingUwCloaAssessment`, `PendingCashCollection`).
- [ ] **PH-02-05** `IRemainingPlansEvaluator` orchestrator composing the 4 sub-evaluators. Returns a single immutable `EvaluationResult`.
- [ ] **PH-02-06** DI registration in `MOHProject.Infrastructure/DependencyInjection.cs` (extension method) — all evaluators as `AddSingleton`.
- [ ] **PH-02-07** Diagnostic logging via `ILogger<T>` at Debug level: input hash + output enum values. Never log PII.
- [ ] **PH-02-08** Test project structure: `MOHProject.Tests/Domain/RemainingPlansEvaluator/` with one folder per sub-evaluator + one `Regression/UatBugs/` folder.

## Test suite (bar to ship)

Every test lives in `Unit/` — no DB, no Docker.

### 8 baseline unit tests (from source lines 1950-1963)

| # | Scenario | Expected |
|---|---|---|
| 1 | Enter PENDING MANUAL UW normal | RCMP Flag CLEARED, AcceptCloa RETAINED |
| 2 | Renewal + Extra Loading + add Cancer Guard → PENDING MANUAL UW | RCMP Flag RETAINED, AcceptCloa RETAINED |
| 3 | UW Decision = Substandard, entry = Pending Cash Collection | AcceptCloa AUTO CLEAR, substatus = COND. ACCEPT |
| 4 | UW Decision = Declined, entry = Pending Cash Collection | AcceptCloa RETAINED, substatus = PENDING CASH |
| 5 | NTU → remaining AllStandard, shortfall=0, SG/PR × SG/PR | RCMP untick+grey, AcceptCloa blank+grey, substatus = PENDING IP REQUEST |
| 6 | NTU → remaining AllStandard, shortfall=0, FR × FR | Same fields, substatus = POLICY INCEPTED |
| 7 | NTU → remaining ExclusionOnly, AcceptCloa=Yes, shortfall=0 | RCMP untick+grey, AcceptCloa Yes (enabled), substatus = PENDING IP REQUEST |
| 8 | NTU → remaining HasRcmp, AcceptCloa=Blank | RCMP ticked+enabled, AcceptCloa Blank, substatus = COND. ACCEPT |

### 4 UAT bug regression tests

One per policy in [BUG-uat-2026-06](../bugs/uat-2026-06.md). Each test:
1. Constructs the exact `Policy` state described in the "Setup" row.
2. Invokes `EvaluateRemainingPlans` with the "Action" event.
3. Asserts the "Expected" outcome — substatus + RCMP fields + LetterType + CompleteUw.

Naming: `UatBug_2610000310P_NtuExclusionRider_SubstatusResetsToPendingCash` etc.

### Composition edge cases (add to `PlansCompositionEvaluator/`)
- No active plans → composition undefined; document as invariant violation (throw).
- Only Base + no riders → composition equals Base's category.
- Priority ordering: RCMP + ExclusionOnly + Standard mix → returns `HasRcmp`.

## Definition of Done
- All 8 baseline tests green.
- All 4 UAT bug regression tests green.
- `IRemainingPlansEvaluator` marked `[Pure]`-equivalent (no fields mutated on inputs; every call with same input returns equal output).
- Domain project has 0 references to `Microsoft.EntityFrameworkCore.*`.
- Code review: no `if`-chains deeper than 3 levels; each sub-evaluator method fits on one screen.

## Out of scope
- Actually persisting the `EvaluationResult` back to the DB (that is Phase 3+).
- Actually generating letters (Phase 4).
- Scheduling reminders (Phase 5).

## Risks
- **Overfitting to the 8 baseline tests.** Add exploratory property-based tests or table-driven tests covering the full matrix (composition × residency × shortfall × AcceptCloa = ~24 cells) to catch cases the 8 don't hit.
- **Silent enum drift.** Every enum value referenced in the truth tables should have an explicit branch or a `switch` expression with exhaustiveness — compiler warnings enabled (`TreatWarningsAsErrors` on the Domain project).

## Change log
| Date       | Version | Change                                                                              | Author |
|------------|---------|-------------------------------------------------------------------------------------|--------|
| 2026-07-11 | 0.1     | Initial draft                                                                       | Claude |
| 2026-07-11 | 0.2     | Delivered. 4 sub-evaluators + orchestrator + DI + logging. 104/104 tests green.    | Claude |
