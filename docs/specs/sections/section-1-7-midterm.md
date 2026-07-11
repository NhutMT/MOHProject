---
id: FR-MID
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 961-1493
last_updated: 2026-07-11
depends_on: [DOMAIN-MODEL]
implements_in: [PH-08f]
---

# Section 1.7 — Midterm AOR (Separate Pipeline)

**⚠ Not to be confused with §1.1-1.5.** Midterm AOR uses:
- `ProductStatus` (plan-level), NOT `PolicySubstatus`.
- `PremiumNotificationLetter`, NOT LOA / CLOA.
- Its own reminder rules and NTU letter templates.

Do NOT reuse `IRemainingPlansEvaluator` here — build a parallel pipeline.

## Purpose
Handle rider additions/changes during an active policy year (between NB and Renewal). Different business context = different rules.

## Requirements

### FR-MID-001 — Trigger and scope
- **Trigger:** User initiates AOR when policy is `Active` and not in NB/Renewal/CoP/CoC context.
- **Behavior:** Uses Midterm pipeline. `PolicySubstatus` is NOT touched.
- **Source:** lines 963-968.

### FR-MID-010 — Premium Notification Letter — Trigger rules
- Rules for when to generate the letter. See source lines 1032-1076.
- Letter is standalone (not accompanied by LOA/CLOA).

### FR-MID-011 — Premium Notification Reminder / Final Reminder
- Cadence and cancellation rules parallel to LOA reminders but for the Midterm letter.
- Source lines 1078-1121.

### FR-MID-020 — NTU / Postponed / Declined during Midterm
- Different letter templates than NB/RN AOR (see FR-MID-030).
- Source lines 1123-1158.

### FR-MID-021 — Letter rules for Declined / Postponed / NTU (Midterm)
- Letter emission tied to `ProductStatus` changes, not `Substatus`.
- Source lines 1160-1203.

### FR-MID-030 — Midterm NTU Letter templates
- Templates distinct from NB/RN NTU letters.
- Source lines 1244-1261.

### FR-MID-040 — Removal of Rider(s) via Dustbin
- User can drag a rider to the dustbin to reverse a status. Triggers `ProductStatus` reversal.
- Source lines 1205-1207.

### FR-MID-041 — Reversal of Product Status after Dustbin
- Rules for what state the plan returns to.
- Source lines 1208-1242.

### FR-MID-050 — Premium Allocation for Midterm AOR — 3 cases
- Three distinct allocation cases documented in source.
- Source lines 1263-1379.

### FR-MID-051 — Premium destination table (Document vs Client view)
- Reconciliation between what the document says vs what the client displays.
- Source lines 1381-1412.

### FR-MID-060 — Midterm vs NB/RN/CoP/CoC comparison
- Summary of differences (used to decide which pipeline to route through).
- Source lines 1414-1430.

## Acceptance criteria
- [ ] AC-1: Midterm pipeline never mutates `Policy.Substatus`.
- [ ] AC-2: Midterm never generates LOA / CLOA rows.
- [ ] AC-3: NB/RN/CoP/CoC pipeline never generates `PremiumNotificationLetter`.
- [ ] AC-4: Architecture test enforces the boundary: types in `MOHProject.Domain.Midterm.*` do not reference `PolicySubstatus`.

## Open questions
- **Q-MID-001:** Do Midterm reversals fire the same `RiderStatusChanged` event as NB flow, or a separate `MidtermRiderStatusChanged`? Recommendation: separate — cleaner boundary.
- **Q-MID-002:** Premium destination "Document vs Client" divergence (source lines 1381-1412) — which is authoritative for the code? Confirm before implementing.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
