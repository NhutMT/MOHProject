---
id: FR-COP
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 2527-2828
last_updated: 2026-07-11
depends_on: [FR-AOR, DOMAIN-MODEL]
implements_in: [PH-08c]
---

# Section 4 — Change of Plan (CoP)

Upgrade or downgrade an existing plan. Uses a mapping matrix from Excel to determine which transitions are allowed.

## Purpose
Support user-initiated plan changes on an active policy, with correct handling of premium recalc, exclusion/loading copy, and letter generation.

## Requirements

### FR-COP-001 — Plan hierarchy & mapping matrix
- Matrix (from Excel) governs which plan-to-plan transitions are allowed and whether it's an Upgrade or Downgrade.
- Persist matrix in `MOHProject.Domain/Configuration/CopMappingMatrix.cs` as static/read-only data (or seed table if it must be editable by ops).
- **Source:** lines 2531-2553.

### FR-COP-010 — Change type classification
- Every transition classified as `Upgrade` / `Downgrade` / `LateralSwap` / `Disallowed`.
- **Source:** lines 2555-2614.

### FR-COP-020 — Validations (Midterm CoP)
- Pre-condition checks before allowing CoP submission (age, policy year, product family, etc.).
- **Source:** lines 2616-2631.

### FR-COP-030 — "Copy Exclusion" and "Copy Risk Loading" buttons
- New UI buttons on CoP flow to carry forward existing Exclusion or Risk Loading from previous plan.
- Backing command: `CopyExclusionCommand`, `CopyRiskLoadingCommand`.
- **Source:** lines 2633-2651.

### FR-COP-040 — Upgrade flow
- Complete state machine for upgrade path.
- **Source:** lines 2653-2712.

### FR-COP-050 — Downgrade flow
- State machine for downgrade path.
- **Source:** lines 2714-2737.

### FR-COP-060 — Common rules (Upgrade + Downgrade)
- Rules shared by both directions: audit, letter emission, premium recalc.
- **Source:** lines 2739-2773.

### FR-COP-070 — Mapping matrix summary
- Human-readable overview of the Excel matrix.
- **Source:** lines 2775-2792.

### FR-COP-080 — Implementation cautions
- Doc's warnings on edge cases.
- **Source:** lines 2794-2828.

## Acceptance criteria
- [ ] AC-1: Every cell in the Excel matrix maps to exactly one classification.
- [ ] AC-2: Copy Exclusion / Copy Risk Loading buttons carry the correct data into the new plan.
- [ ] AC-3: Upgrade/Downgrade generates the correct letter set per FR-COP-060.
- [ ] AC-4: CoP that adds a rider still flows through FR-AOR-030 (`EvaluateRemainingPlans`) — this section extends AOR, not replaces it.

## Open questions
- **Q-COP-001:** Where does the source Excel live? Need to import into the repo (checked in as CSV/JSON) so the matrix is version-controlled with the code.
- **Q-COP-002:** When downgrading, does the customer get an automatic refund of the premium difference? Assume yes (via Phase 4 Unallocated Cash + Refund Letter).

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
