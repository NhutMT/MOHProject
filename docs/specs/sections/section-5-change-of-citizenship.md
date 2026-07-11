---
id: FR-COC
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 2830-3140
last_updated: 2026-07-11
depends_on: [FR-AOR, DOMAIN-MODEL]
implements_in: [PH-08d]
---

# Section 5 — Change of Citizenship (CoC)

Insured's residency changes → policy must transition. Uses AOR core pipeline plus CoC-specific copy logic and letter rules.

## Purpose
Handle policy transitions when insured moves between SG / PR / FR statuses. Reuses AOR core; adds copy logic from previous policy and CoC-specific CLOA rules on renewal.

## Requirements

### FR-COC-001 — Business context
- **Source:** lines 2834-2839.

### FR-COC-002 — CoC vs CoP — key differences
- Fundamental behavioral differences (different residency implications, different letter set).
- **Source:** lines 2841-2855.

### FR-COC-010 — Validations
- Pre-conditions before allowing CoC (dates, product eligibility, etc.).
- **Source:** lines 2857-2875.

### FR-COC-020 — Auto-Route to UW (CoC-specific)
- Extension of FR-AOR-001 for CoC context. May have additional trigger conditions.
- **Source:** lines 2877-2891.

### FR-COC-030 — Copy logic from previous policy
- Rules for which fields carry over from the old-residency policy to the new-residency one.
- **Source:** lines 2893-2932.

### FR-COC-040 — Letter generation (CoC)
- Letters specific to CoC: which combinations trigger which letters.
- **Source:** lines 2934-2951.

### FR-COC-050 — Process flow for Insured = FR — NTU / Decline / Postpone
- Substatuses that allow NTU / Decline / Postpone when insured has FR residency.
- **Source:** lines 2953-2985.

### FR-COC-060 — CLOA rule for Renewal of CoC Policy
- Special CLOA behavior on the first Renewal after a CoC.
- **Source:** lines 2987-3022.

### FR-COC-070 — Premium recalculation and Re-add
- How premium is recomputed after CoC; re-add rider path.
- **Source:** lines 3024-3030.

### FR-COC-080 — CoC-specific implementation notes
- **Source:** lines 3032-3140.

## Acceptance criteria
- [ ] AC-1: Every field enumerated in FR-COC-030 is either copied or explicitly excluded, no accidental leakage.
- [ ] AC-2: FR-only NTU/Decline/Postpone paths respect FR-COC-050 substatus list.
- [ ] AC-3: A CoC that adds riders still passes through `IRemainingPlansEvaluator` (integration test).

## Open questions
- **Q-COC-001:** If insured changes SG → FR mid-cycle, does the current cycle's premium prorate, or does the change take effect next cycle? Doc uses "Renewal of CoC Policy" phrasing suggesting next-cycle — confirm.
- **Q-COC-002:** For FR → SG (upgrade), does the customer need to re-underwrite? Assume yes for medical questions.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
