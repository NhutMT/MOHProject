---
id: FR-FR
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 3140-3424
last_updated: 2026-07-11
depends_on: [FR-AOR, FR-COC, DOMAIN-MODEL]
implements_in: [PH-08e]
---

# Section 6 — FR (Foreign Resident) Process Flow

Cross-cutting section that consolidates FR-specific rules already scattered across §1 (residency-based substatus decisions) and §5 (CoC copy logic).

## Purpose
Serve as a single reference for FR handling. Ensures no FR-specific rule is missed and provides a single audit target: "does every FR × FR path in the code go through the expected substatus?"

## Requirements

### FR-FR-001 — Terminal substatus for FR × FR
- When both insured and payer are FR:
  - AllStandard, shortfall = 0 → `PolicyIncepted` (not `PendingIpRequestFile`).
  - AllStandard, shortfall > 0 → `PendingCashCollection` (unchanged).
  - Non-Standard follows the same rules as SG/PR paths except the terminal substatus.
- **Source:** cross-references FR-AOR-032.

### FR-FR-002 — No IP file for FR × FR
- No IP Request File is generated when both parties are FR. Path bypasses `PendingIpRequestFile`.
- **Source:** implied by FR-FR-001.

### FR-FR-010 — Mixed residency (SG/PR × FR or FR × SG/PR)
- At least one SG/PR → IP file path applies.
- Terminal substatus = `PendingIpRequestFile`.
- **Source:** cross-references FR-AOR-032.

### FR-FR-020 — FR renewal without CoC
- FR insured renewing without changing citizenship: standard FR path.
- Reminder rules unchanged.

### FR-FR-030 — FR-only NTU / Decline / Postpone substatus list
- Reuses FR-COC-050.

## Acceptance criteria
- [ ] AC-1: Every FR × FR terminal path is covered by ≥1 test.
- [ ] AC-2: No IP Request File letter is generated on any FR × FR path (property test — search generated letters, assert no `IpRequestFile` type).
- [ ] AC-3: The FR flow reuses the same core evaluator — no separate FR-only branch that could drift.

## Open questions
- **Q-FR-001:** For a policy with mixed residency where one party changes to FR mid-cycle, does the terminal-substatus rule re-evaluate immediately (via FR-AOR-030) or only at next Renewal? Assume immediate.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
