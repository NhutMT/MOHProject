---
id: PH-03
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-02]
estimated_effort: 2-3 weeks (1 dev)
---

# Phase 3 — Entry-Point State Machine

Implement the 5 entry substatuses × 6 UW decisions matrix (30 flows) as a Strategy pattern that delegates to `IRemainingPlansEvaluator` from Phase 2. Also implement Enhancement 1 (auto-route to UW when Risk Category = BLANK).

## Goal
After Phase 3, the sequence "user Saves UW tab → substatus lands in `PendingManualUw` → UW makes a decision → correct next state" runs entirely in code without any UI. Testable via application-layer command handlers.

## Deliverables

- [ ] **PH-03-01** `IEntryPointHandler` interface with `Handle(Policy policy, UwDecision decision)`. Concrete impls per entry substatus (see [FR-AOR-050 to FR-AOR-054](../sections/section-1-addition-of-riders.md)).
- [ ] **PH-03-02** `CondAcceptLetterGenHandler` — 1.5.1. Matches source lines 543-554.
- [ ] **PH-03-03** `PendingUwCloaAssessmentHandler` — 1.5.2. Source lines 556-567.
- [ ] **PH-03-04** `PendingCashCollectionHandler` — 1.5.3. Source lines 569-580. **Special:** Substandard decision auto-clears AcceptCloa + RcmpOption.
- [ ] **PH-03-05** `PendingIpRequestFileHandler` — 1.5.4. Source lines 582-593. **Special:** APS → auto-remove IP record 045G from CPF tab; Decline/Postpone/NTU → auto-create IP record.
- [ ] **PH-03-06** `PendingIpResponseCpfRejectedHandler` — 1.5.5. Source lines 595-606. **Special:** APS on this substatus recalculates Linked Rider(s) only, not Base allocation.
- [ ] **PH-03-07** Entry-point handler for NTU-only substatuses `PendingPpRequestFile` / `PendingPpResponseCpfRejected` (source lines 242-243).
- [ ] **PH-03-08** `SaveUnderwritingTabCommand` handler. **Enhancement 1** (source lines 211-235): when Risk Category = BLANK, system auto-routes to UW (writes `Substatus = PendingManualUnderwriting` + audit entry `"Submit for underwriting review"`). Does NOT require a separate button click.
- [ ] **PH-03-09** `UwDecisionCommand` handler. Reads current substatus, dispatches to the matching `IEntryPointHandler`, applies transitions, calls `IRemainingPlansEvaluator`, persists changes.
- [ ] **PH-03-10** Resubmit-for-Manual-UW command (source lines 202-205). Allowed only from: `PendingUwAps`, `ConditionalAcceptanceLetterGenerated`, `PendingCashCollection`, `PendingIpRequestFile`. Rejects with a domain exception otherwise.
- [ ] **PH-03-11** NTU-per-rider UI backing command: `MarkRiderNtuCommand`, `MarkRiderDeclinedCommand`, `MarkRiderPostponedCommand`. Each triggers `EvaluateRemainingPlans` (this is where the UAT bug root cause is actually wired in).

## Test suite

- [ ] **PH-03-T01** Table-driven test per entry point: 6 UW decisions × N residency/shortfall permutations. Target: 30+ tests across the 5 entry handlers.
- [ ] **PH-03-T02** Table-driven test for Save-UW-tab auto-route: (RC=BLANK → auto-route) vs (RC set → no auto-route).
- [ ] **PH-03-T03** Resubmit-for-Manual-UW guard: 9 substatuses × allowed/rejected — 9 tests.
- [ ] **PH-03-T04** MarkRiderNtu integration test using an in-Domain fake `IPolicyRepository` — verify the evaluator is called and state is persisted.
- [ ] **PH-03-T05** Renewal exception test (source lines 451-457): Base = Extra Loading, add Cancer Guard → RCMP Flag retained after PENDING MANUAL UW entry.

## Definition of Done
- 30-cell entry-point × UW-decision matrix has 100% test coverage.
- No `switch` on `PolicySubstatus` outside the handler dispatch — everything downstream reads state from the entity.
- Audit trail entry written for every state transition (verify via test assertion).

## Out of scope
- Letter generation implementation (Phase 4).
- Reminder scheduling (Phase 5).
- Web UI (Phase 7).

## Risks
- **Missing edge cases in the 30-cell matrix.** Cross-check the section spec's per-entry tables. If BA/PO clarifies any cell later, add a source_lines annotation to the test.
- **Duplicated logic between entry handlers.** Extract shared helpers (`ApplyStandardDecisionResults`, `HandleAcceptCloaClear`) rather than inline copy-paste.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
