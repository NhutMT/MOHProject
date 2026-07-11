---
id: FR-PREM
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 2210-2525
last_updated: 2026-07-11
depends_on: [DOMAIN-MODEL]
implements_in: [PH-04, PH-07, PH-08b]
---

# Section 3 — Premium Summary & Premium Collection Table

Rules for displaying and reconciling the two premium tables (Summary + Collection), including handling of NTU / Decline / Postponed with refund and excess-premium scenarios.

## Purpose
Ensure the UI shows the correct amounts for To Collect / Collected / Shortfall / Unallocated Cash, and that the Refund flow works end-to-end.

## Requirements

### FR-PREM-001 — Substatuses allowing early receipt (NTU with refund)
- Enumerated in source lines 2369-2423.
- List of substatuses where NTU can happen even before full cycle completion.

### FR-PREM-010 — Decline / Postponed with refund
- Rules for when Decline/Postponed leads to premium refund vs no-refund letter variants.
- **Source:** lines 2425-2463.

### FR-PREM-020 — Excess premium (Switch from Key Rider to Choice Rider)
- Specific scenario: user swaps one rider for a cheaper one → excess must flow to Unallocated Cash and refund letter.
- **Source:** lines 2465-2480.
- Reuses FR-AOR-003 mechanics.

### FR-PREM-030 — Letter templates catalog (Section 3)
- All letter templates referenced in Section 3.
- **Source:** lines 2482-2492.

### FR-PREM-040 — Implementation notes
- Doc's implementation hints — cross-check with Phase 4 side-effects design.
- **Source:** lines 2494-2525.

## Acceptance criteria
- [ ] AC-1: Excess premium always visible in Unallocated Cash column when Collected > ToCollect.
- [ ] AC-2: Refund letter generated on the same transaction as the excess move.
- [ ] AC-3: Switch scenario (Key → Choice) reproduces the doc's example numbers exactly.

## Open questions
- **Q-PREM-001:** UI: are Summary and Collection two separate tables side-by-side, or two tabs? Doc doesn't specify layout.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
