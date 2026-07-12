---
id: FR-LTR-COMBO
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 1675-1859
last_updated: 2026-07-11
depends_on: [FR-AOR, DOMAIN-MODEL]
implements_in: [PH-04]
---

# Section 1.6 — Letter Issue for Combination Decision

Rules for which CLOA to generate when Base Plan and Rider Plan(s) have different Risk Categories. Three sub-sections based on Base Plan risk category.

## Purpose
Disambiguate which CLOA (Exclusion vs RCMP) to generate, and whether an Ack page is needed, when the composition is not uniform.

## Requirements

### FR-LTR-COMBO-001 — Base = Standard
- **Behavior:** Letter type is determined entirely by riders' aggregate composition. If any rider is HasRcmp → CLOA(RCMP). Else if any rider has Exclusion → CLOA(Exclusion). Else → LOA.
- **Source:** lines 1690-1717.

### FR-LTR-COMBO-002 — Base = Substandard (Risk Loading)
- **Behavior:** Base contributes Loading; combined with rider composition determines letter:
  - Any rider with Exclusion → CLOA(RCMP) (Base's Loading + any Rider's Exclusion = RCMP composition at the policy level).
  - All riders Standard → CLOA(RCMP) (Base itself is Loading, but without Exclusion the composition is HasRcmp only if a rider adds Exclusion — otherwise it's the Loading-only edge case; confirm with BA in [Q-CB-001](#open-questions)).
- **Source:** lines 1719-1761.

### FR-LTR-COMBO-003 — Base = Substandard (Exclusion)
- **Behavior:** Base contributes Exclusion; combined with rider composition:
  - Any rider with Loading → CLOA(RCMP).
  - All riders Standard or Exclusion → CLOA(Exclusion).
- **Source:** lines 1763-1805.

### FR-LTR-COMBO-010 — Universal rules
- **Ack page required** for CLOA when the customer has not yet accepted (AcceptCloa = Blank).
- **Ack page NOT required** for CLOA when AcceptCloa = Yes and a superseding CLOA is being issued (customer already agreed to prior terms; new letter is informational).
- **Source:** lines 1807-1839.

### FR-LTR-COMBO-020 — Decision flow (canonical)
```
Composition of (Base + all Active Riders):
  HasRcmp → CLOA (RCMP)
  ExclusionOnly → CLOA (Exclusion)
  AllStandard → LOA

Ack page:
  AcceptCloa == Blank → include Ack
  AcceptCloa == Yes  → omit Ack
```
- **Source:** lines 1841-1859.

## Acceptance criteria
- [ ] AC-1: `ILetterTypeEvaluator` returns the correct `(LetterType, hasAck)` for the 9-cell matrix (3 Base categories × 3 rider aggregate categories).
- [ ] AC-2: `hasAck` is decided independently of the base/rider categories — it's purely a function of `AcceptCloa`.

## Open questions
- **Q-CB-001:** Base = Loading + all riders Standard — is the letter LOA (base is the only exception, treated as Loading-only edge) or CLOA(RCMP)? Doc's table is ambiguous. BA to confirm.
- **Q-CB-002:** *Composition semantics mismatch between doc §1.5 and §1.6.* — `PlansCompositionEvaluator` (source lines 1904-1911, per-plan check) returns `ExclusionOnly` when Base=Loading + Rider=Exclusion. §1.6.2 (source lines 1719-1761) expects `CloaRcmp` for this exact combo because the *aggregate* of Base + Riders spans both Loading and Exclusion. Impl of `LetterTypeEvaluator` in PH-02 currently uses per-plan composition — will emit `CloaExclusion` where `CloaRcmp` is expected. **Blocker for PH-04 letter emission.** Two candidate resolutions: (a) `LetterTypeEvaluator` takes Base plan's `RiskCategory` as extra input and layers §1.6 rules on top of composition; (b) introduce a separate `AggregateComposition` distinct from the per-plan `RiskComposition`. Confirm intent with BA.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
