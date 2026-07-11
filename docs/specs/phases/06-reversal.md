---
id: PH-06
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-03, PH-04, PH-05]
estimated_effort: 1 week (1 dev)
---

# Phase 6 — Reversal of Substatus

Wire every rider-lifecycle event to the `IRemainingPlansEvaluator` orchestrator so the substatus, RCMP fields, letters, and reminders re-evaluate consistently. This phase has almost no new code — it is the glue that guarantees the 4 UAT bugs stay fixed.

## Goal
Every action that changes an active-plan set must call `IRemainingPlansEvaluator` before committing. If any handler skips it, add a static analysis / test rule to catch it in CI.

## Deliverables

- [ ] **PH-06-01** Enumerate every trigger (source lines 1660-1673):
  - Mark rider NTU
  - Mark rider Declined
  - Mark rider Postponed
  - Re-add a rider that was previously NTU/Declined/Postponed
  - UW Complete decision
  - Reverse a Product-Status transition (§Midterm dustbin — Phase 8+)
- [ ] **PH-06-02** Domain event `RiderStatusChanged` raised by `Plan.MarkNtu()`, `Plan.MarkDeclined()`, `Plan.MarkPostponed()`, `Plan.MarkReAdded()`.
- [ ] **PH-06-03** `RiderStatusChangedHandler` subscribes to the event and calls `IRemainingPlansEvaluator` → persists result via `IPolicyRepository` → generates letters via Phase 4 → schedules reminders via Phase 5.
- [ ] **PH-06-04** "Two-direction chained reversal" support (source lines 1647-1658): a single evaluation may itself cause further transitions (e.g. NTU rider → composition becomes AllStandard → old CLOA superseded → CLOA reminders cancelled). Handler runs to fixpoint (max 3 iterations; error if not converged).
- [ ] **PH-06-05** Architecture test (via `NetArchTest` or custom Roslyn analyzer): every command handler that mutates plan status must transitively depend on `IRemainingPlansEvaluator`. Fails build if a new handler skips it.

## Test suite

- [ ] **PH-06-T01** Re-run all 4 UAT bug regressions from Phase 2 through the full Phase 6 pipeline (not just the evaluator in isolation) — assert not just substatus but letters, reminders, audit are all updated.
- [ ] **PH-06-T02** Two-direction chain: NTU rider triggers new letter, which itself supersedes an old letter with reminders in flight — assert the reminders end up cancelled and new ones scheduled.
- [ ] **PH-06-T03** Re-add rider after NTU: original letter regenerates, RCMP fields recompute — verify composition swings back to prior state.
- [ ] **PH-06-T04** Architecture test: introduce a broken handler that skips the evaluator → build fails.

## Definition of Done
- All 4 UAT bug integration tests green (bug-to-fix traceability complete).
- Architecture test present and enforcing the rule.
- No handler in `MOHProject.Application` mutates `Plan.Status` without going through the domain event → evaluator path.

## Out of scope
- Midterm reversal (Section 1.7.5). That path uses `ProductStatus` semantics and is handled in Phase 8+.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
