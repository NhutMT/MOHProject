---
id: PH-04
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-03]
estimated_effort: 2 weeks (1 dev)
---

# Phase 4 — Side Effects (Premium, Letters, Audit)

Everything that happens *after* an evaluation result is computed: recalculating premium, moving excess to Unallocated Cash, generating letter rows, appending audit entries.

## Goal
After Phase 4, running a `UwDecisionCommand` produces persisted `Letter` rows, updated `PremiumCollection`, and a complete `AuditTrail` — but no reminders yet (Phase 5). Letter *content* (the actual template) can be stubbed as "TBD by BA."

## Deliverables

- [ ] **PH-04-01** `IPremiumCollectionRecalculator` + impl. Rules per §Bước 1 (source lines 662-687):
  - Recompute `LinkedRidersToCollect` from active riders.
  - Keep `Collected` unchanged.
  - `Shortfall = max($0, ToCollect − Collected)` — never negative.
  - If `Collected > ToCollect`, move excess to `UnallocatedCash`.
- [ ] **PH-04-02** `ILetterGenerator` impl. Marks previously current letters of the same type as `IsCurrent = false` and inserts a new row. Correlation ID links siblings for reminder supersession.
- [ ] **PH-04-03** Letter body templates as Razor `.cshtml` files in `MOHProject.Infrastructure/Letters/Templates/` — placeholder content; the actual copy comes from BA per [Q-401](#open-questions). Rendering pipeline: RazorLight or `IRazorViewEngine` — implementer's choice.
- [ ] **PH-04-04** `RefundOfExcessPremiumLetterGenerator` — triggered by the recalculator when excess is created. New letter type introduced by this enhancement.
- [ ] **PH-04-05** `NtuLetterGenerator`, `DeclineLetterGenerator`, `PostponementLetterGenerator` — each supports the "with refund" / "without refund" variant based on whether excess was created in the same transaction.
- [ ] **PH-04-06** `MedicalEvidenceLetterGenerator` — triggered when UW decision = APS.
- [ ] **PH-04-07** Letter inclusion rule ([FR-LTR-005](../sections/section-1-addition-of-riders.md)): letters include only Active Plans; exclude Declined / Postponed / NTU / Terminated / Cancelled.
- [ ] **PH-04-08** Newly-added-rider filter: NTU/Decline/Postpone letters cover only riders `AddedAt >= currentUwCycle.StartedAt`. Riders NTU'd/Declined/Postponed *before* the current UW completion date are excluded (source lines 692-712).
- [ ] **PH-04-09** `IAuditTrailWriter` impl. Serializes payload with `System.Text.Json`; excludes PII from the payload projection (payer name, DOB → hashed or omitted; internal IDs OK).
- [ ] **PH-04-10** Transactional boundary: `IUnitOfWork` wrapping the DbContext. `UwDecisionCommand` handler executes recalc + letter + audit + substatus change in one transaction; if any step throws, all roll back.

## Test suite

- [ ] **PH-04-T01** Recalculator excess handling: 4 cases — no excess / small excess / large excess / zero premium after all riders NTU. Assert `UnallocatedCash` and letter emission side effect.
- [ ] **PH-04-T02** Letter supersession: generate LOA v1 → then LOA v2 → v1's `IsCurrent = false`, v2 = true, same `CorrelationId`.
- [ ] **PH-04-T03** Letter inclusion filter: policy with mixed statuses → letter body's `IncludedPlanIds` matches only Active.
- [ ] **PH-04-T04** Newly-added filter with 2 UW cycles: rider NTU'd in cycle 1 must not appear in cycle 2's NTU letter.
- [ ] **PH-04-T05** Transaction rollback: force `ILetterGenerator` to throw halfway through `UwDecisionCommand` → assert substatus, premium, and audit table are all unchanged.
- [ ] **PH-04-T06** (Integration, Testcontainers) — run the full command against real SQL Server for one representative flow and assert row counts.

## Definition of Done
- Every letter type in `LetterType` enum has a generator (even if body is TBD).
- `PremiumCollection.Shortfall` cannot serialize as negative (property-level guard test).
- Audit table has one entry per state-affecting side-effect; no orphan letters without audit entry.

## Out of scope
- Reminder scheduling (Phase 5).
- Physical letter delivery (email/print). Letters are persisted rows; delivery is a separate concern.

## Open questions
- **Q-401:** BA to provide final template copy for each letter type. Placeholders in Razor templates until then.
- **Q-402:** Should `UnallocatedCash` refund be automatic or require Finance approval? Doc implies Finance team "allocate/process refund" (source line 683) — model as manual action for now.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
