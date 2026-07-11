---
id: FR-AOR
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 161-491, 493-534, 536-607, 609-908
last_updated: 2026-07-11
depends_on: [DOMAIN-MODEL, GLOSSARY]
owners: []
implements_in: [PH-02, PH-03, PH-04, PH-06]
---

# Section 1 — Addition of Rider(s) (§1.1–1.5)

The main workstream. Applies to NB, Renewal, Change of Plan, Change of Citizenship (Midterm is separate — see [section-1-7-midterm.md](section-1-7-midterm.md)).

## Purpose
Add one or more riders to an existing policy, run them through Underwriting, and correctly propagate downstream side-effects (substatus, RCMP fields, letters, reminders, premium collection).

## Scope
- **IN:** NB, Renewal, Change of Plan, Change of Citizenship AOR flows.
- **OUT:** Midterm AOR (`FR-MID-*`), Medisave switch (`FR-MED-*`), plan mapping / upgrade / downgrade (`FR-COP-*`).

## Requirements

### FR-AOR-001 — Auto-Route to Underwriter (Enhancement 1)
- **Trigger:** User clicks Save on Underwriting tab with Risk Category = BLANK.
- **Behavior:** System auto-sets substatus to `PendingManualUnderwriting`. No separate "Submit Manual Underwriting" click required. Audit trail records event `"Submit for underwriting review"` (source line 233 — replaces old text `"user manual submit for underwriting review"`).
- **Preconditions:** Policy has at least one rider added in this AOR session.
- **Postconditions:** `Policy.Substatus = PendingManualUnderwriting`. UW work queue has this policy.
- **Source:** lines 173-177, 211-235.
- **Test scenarios:** Save with RC=BLANK auto-routes / Save with RC set does not auto-route / audit trail text exact match.

### FR-AOR-002 — NTU / Termination Rider(s) UI section
- **Trigger:** User opens the NTU/Termination section on the Product tab.
- **Behavior:** Section renamed to "NTU/Termination Rider(s)" (was untitled). Shows a radio button + checkbox per rider that is currently ticked in the Product section. Riders not in the product section are hidden.
- **Preconditions:** —
- **Postconditions:** User can select a rider and invoke NTU / Decline / Postpone.
- **Source:** lines 178-180.

### FR-AOR-003 — Excess premium moves to Unallocated Cash
- **Trigger:** After NTU/Decline/Postpone, `Collected > ToCollect`.
- **Behavior:** `Shortfall = $0` (not negative). Excess amount added to `UnallocatedCash`. `RefundOfExcessPremium` letter generated.
- **Preconditions:** Rider has premium collected.
- **Postconditions:** Premium collection table shows Shortfall = 0, UnallocatedCash increased, one new letter row.
- **Source:** lines 182-186, 662-687.
- **Fixes:** the bug where Shortfall was displayed as negative.

### FR-AOR-030 — EvaluateRemainingPlans() orchestrator ⭐
- **Trigger:** Any of: mark rider NTU, mark rider Declined, mark rider Postponed, re-add previously NTU'd/declined/postponed rider, UW Complete decision.
- **Behavior:** Runs the 5-step pipeline (source lines 621-656):
  1. Recalculate Premium Summary + Premium Collection.
  2. Generate NTU/Decline/Postpone letter for affected rider(s).
  3. Scan remaining Active Plans → determine `RiskComposition`.
  4. Generate new LOA or CLOA reflecting remaining plans — **conditional** on substatus ∈ {`ConditionalAcceptanceLetterGenerated`, `PendingUwCloaAssessment`, `PendingCashCollection`}.
  5. Update Reminder / Final Reminder (new supersedes old).
- **Preconditions:** Policy has at least one plan whose status will change.
- **Postconditions:** Substatus, `UWState`, letters, reminders all consistent with remaining active plans.
- **Source:** lines 256-337, 611-909, 1862-1948.
- **Not calling this = the root cause of all 4 UAT bugs.** See [BUG-uat-2026-06](../bugs/uat-2026-06.md).

### FR-AOR-031 — RCMP field state per composition
- **Trigger:** `EvaluateRemainingPlans()` step 3 completes.
- **Behavior:** Set RCMP Flag, Accept CLOA, RCMP Option, Complete UW according to composition:

  | Composition | RCMP Flag | Accept CLOA | RCMP Option | Complete UW | New Letter |
  |---|---|---|---|---|---|
  | `AllStandard` | Untick + Greyed | Blank + Greyed | Blank + Greyed | AUTO SELECT | LOA |
  | `ExclusionOnly` | Untick + Greyed | Retain if Yes (enabled) | Blank + Greyed | AUTO SELECT | CLOA (Exclusion) |
  | `HasRcmp` | Remain Ticked | Retain (enabled) | Remain selected | AUTO SELECT | CLOA (RCMP) |

- **Source:** lines 279-307.

### FR-AOR-032 — Substatus outcome per composition × residency × shortfall
- **Trigger:** `EvaluateRemainingPlans()` step 3 completes with composition set.
- **Behavior:**
  - **AllStandard, shortfall = 0**:
    - FR × FR → `PolicyIncepted`
    - any other combo → `PendingIpRequestFile`
  - **AllStandard, shortfall > 0** → `PendingCashCollection`
  - **ExclusionOnly / HasRcmp, AcceptCloa = Blank** → `ConditionalAcceptanceLetterGenerated`
  - **ExclusionOnly / HasRcmp, AcceptCloa = Yes, shortfall = 0, at least one SG/PR** → `PendingIpRequestFile`
  - **ExclusionOnly / HasRcmp, AcceptCloa = Yes, shortfall = 0, both FR** → `PolicyIncepted`
  - **ExclusionOnly / HasRcmp, AcceptCloa = Yes, shortfall > 0** → `PendingCashCollection`
- **Source:** lines 309-336, 1923-1948.

### FR-AOR-040 — Reminder rules (new supersedes old)
- **Trigger:** New LOA or CLOA is generated by step 4 of `EvaluateRemainingPlans()`.
- **Behavior:**
  - New LOA → stop reminders from old LOA/CLOA. Schedule LOA Reminder + Final Reminder from new LOA date (if outstanding cash).
  - New CLOA → stop reminders from old LOA/CLOA. Schedule CLOA Reminder + Final Reminder from new CLOA date (if shortfall or pending CLOA reply).
  - Substatus → `PendingUwAps`: stop all LOA/CLOA reminders. New letter will be generated after UW.
  - Substandard rider NTU'd → composition becomes Standard → STOP CLOA reminders, keep LOA reminder if shortfall.
  - RCMP rider NTU'd → composition becomes ExclusionOnly → STOP CLOA(RCMP), continue CLOA(Exclusion) if applicable.
  - Exclusion rider NTU'd → composition becomes HasRcmp → STOP CLOA(Exclusion), continue CLOA(RCMP) if applicable.
- **Source:** lines 339-372.

### FR-AOR-041 — Letters only include Active Plans
- **Trigger:** Any letter generation.
- **Behavior:** Letter body's plan list contains only plans with `ProductStatus == Active`. Excludes Declined, Postponed, NTU, Terminated, Cancelled.
- **Source:** lines 369-370.

### FR-AOR-042 — Resubmit for Manual UW
- **Trigger:** User clicks "Resubmit for Manual UW" button.
- **Behavior:** Allowed only from substatuses: `PendingUwAps`, `ConditionalAcceptanceLetterGenerated`, `PendingCashCollection`, `PendingIpRequestFile`. From other substatuses, the button is hidden or the command is rejected.
- **Source:** lines 202-205.

### FR-AOR-045 — Product statuses NOT copied to renewal
- **Trigger:** Renewal process copies plans from previous term.
- **Behavior:** These statuses are excluded from copy: `Draft`, `NotTakenUp`, `Postponed`, `Declined`, `Terminated`, `Cancelled`, `PendingTermination`, `PendingCashRefund`.
- **Source:** line 401.

### FR-AOR-050 — Entry 1.5.1: CONDITIONAL ACCEPTANCE LETTER GENERATED
- **Save UW → next:** `PendingManualUnderwriting`.
- **UW Standard →** `ConditionalAcceptanceLetterGenerated` + LOA/CLOA (per composition).
- **UW Substandard →** `ConditionalAcceptanceLetterGenerated` + CLOA(Exclusion or RCMP) + Ack page. If new rider = Sub → clear Accept CLOA.
- **UW Declined →** `ConditionalAcceptanceLetterGenerated` + Decline Letter + new LOA/CLOA. Recalc premium; check remaining.
- **UW Postponed →** same shape as Declined, with Postponement Letter.
- **UW NTU →** same shape, with NTU Letter. Auto-select Complete UW if RC=BLANK.
- **UW APS →** `PendingUwAps` + Medical Evidence Letter.
- **Source:** lines 543-554.

### FR-AOR-051 — Entry 1.5.2: PENDING UW CLOA ASSESSMENT
- Similar to FR-AOR-050 but Standard decision → new CLOA + Ack page (not LOA).
- **Source:** lines 556-567.

### FR-AOR-052 — Entry 1.5.3: PENDING CASH COLLECTION
- **Save UW → next:** `PendingManualUnderwriting`.
- **UW Standard →** stay in `PendingCashCollection`; letter = LOA (if CLOA=Blank) or CLOA-without-Ack (if CLOA=Yes). Retain AcceptCloa + RcmpOption.
- **UW Substandard →** move to `ConditionalAcceptanceLetterGenerated`. **AUTO CLEAR** AcceptCloa + RcmpOption. Letter = CLOA + Ack.
- **UW Declined / Postponed / NTU →** stay in `PendingCashCollection`; letter = respective letter + new LOA/CLOA. Retain AcceptCloa + RcmpOption.
- **UW APS →** `PendingUwAps` + Medical Evidence Letter.
- **Source:** lines 569-580.
- **Special:** the auto-clear rule is 1.5.3-specific.

### FR-AOR-053 — Entry 1.5.4: PENDING IP REQUEST FILE
- **Save UW → next:** `PendingManualUnderwriting`.
- **UW APS →** `PendingUwAps` + Medical Evidence + **auto-remove IP record 045G from CPF tab**.
- **UW Standard →** shortfall>0 → `PendingCashCollection`; shortfall=0 → `PendingIpRequestFile`. Retain both fields.
- **UW Substandard →** `ConditionalAcceptanceLetterGenerated`. **CLEAR** both fields.
- **UW Declined / Postponed / NTU →** stay in `PendingIpRequestFile`. Retain both. **Auto-create IP record in CPF tab.**
- **Source:** lines 582-593.

### FR-AOR-054 — Entry 1.5.5: PENDING IP RESPONSE FILE (CPF REJECTED)
- **Save UW → next:** `PendingManualUnderwriting`.
- **UW APS →** `PendingUwAps`. **Base Plan premium allocation NOT recalculated — only Linked Riders.**
- **UW Standard →** shortfall>0 → `PendingCashCollection` (outstanding = CPF-rejected + rider shortfall cumulative); shortfall=0 → `PendingIpRequestFile`.
- **UW Substandard →** `ConditionalAcceptanceLetterGenerated`. Clear AcceptCloa + RcmpOption.
- **UW Declined / Postponed / NTU →** stay in `PendingIpResponseFileCpfRejected` (no substatus change).
- **Source:** lines 595-606.

### FR-AOR-060 — Renewal exception: RCMP Flag retained for Base Extra Loading
- **Trigger:** During Renewal AOR, Base Plan has Risk Category = Extra Loading (Risk Loading).
- **Behavior:** When adding Cancer Guard Rider OR Choice Rider, RCMP Flag RETAINS `Yes` even at `PendingManualUnderwriting`. Not cleared. Applies to entries 1.5.1 through 1.5.5.
- **Reason:** Base Plan already has Risk Loading → composition is still HasRcmp; no re-underwriting of flag needed.
- **Source:** lines 451-457.

### FR-AOR-070 — Allowed substatuses for NTU / Declined / Postponed
Per source lines 240-252:

| Substatus | NTU | Declined / Postponed |
|---|---|---|
| PendingPpRequestFile | ✅ | — |
| PendingPpResponseFileCpfRejected | ✅ | — |
| PendingManualUnderwriting | ✅ | ✅ |
| PendingUwAps | ✅ | ✅ |
| ConditionalAcceptanceLetterGenerated | ✅ | — |
| PendingUwCloaAssessment | ✅ | — |
| PendingCashCollection | ✅ | — |
| PendingIpRequestFile | ✅ | — |
| PendingIpResponseFileCpfRejected | ✅ | — |

**Special:** NTU at `PendingManualUnderwriting` or `PendingUwAps` deletes the plan type from the Plan Type / Risk Category field on the UW tab, but retains it in the Product tab (source line 253).

## Acceptance criteria
- [ ] AC-1: `IRemainingPlansEvaluator` called on every rider-status event (verified by architecture test).
- [ ] AC-2: All 4 UAT bugs pass the regression suite ([BUG-uat-2026-06](../bugs/uat-2026-06.md)).
- [ ] AC-3: 30-cell entry × UW-decision matrix has ≥1 test per cell.
- [ ] AC-4: No letter generated for a non-Active plan (property test).
- [ ] AC-5: Shortfall never serializes as negative (property test on the `Money` type via `PremiumCollection`).
- [ ] AC-6: Reminder from a superseded letter never fires (integration test in Phase 5).

## Open questions
- **Q-101:** In FR-AOR-050 "clear Accept CLOA if new rider = Sub" — how does the system detect "new rider"? Assume `Plan.AddedAt >= currentUwCycle.StartedAt`. Confirm with BA.
- **Q-102:** For FR-AOR-053 APS branch — does the auto-remove IP record 045G happen synchronously with the UW decision, or is it a follow-up job? Assume synchronous within the same transaction.
- **Q-103:** For FR-AOR-054 Declined/Postponed/NTU staying in same substatus — does that mean the reminder cycle continues from the *original* letter, or a new letter is issued for the affected rider without changing the terminal-substatus letters? Confirm — impacts Phase 5.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
