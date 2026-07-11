---
id: PH-08
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-06]
estimated_effort: 6-8 weeks (1 dev) — parallelizable
---

# Phase 8 — Other Sections (Medisave, CoP, CoC, FR flow, Midterm)

Sections 2–6 from the source doc plus §1.7 Midterm. These are independent workstreams that can proceed in parallel once Phase 6 is complete (or in parallel with Phase 7 UI).

## Goal
Each section fully implemented, tested, and integrated with the existing state machine. Midterm gets its own separate pipeline because it uses `ProductStatus` + `PremiumNotificationLetter`, not `Substatus` + `LOA/CLOA`.

## Sub-phases (parallelizable)

Each sub-phase is a standalone slice. Assign to different devs or take them sequentially.

### PH-08a — Section 2: Medisave Payer Switch Notification (~1 week)
- Trigger: New Business Reason = Renewal + Insured turns 25 in the renewal cycle. See [FR-MED-001](../sections/section-2-medisave-switch.md).
- Deliverable: `MedisaveSwitchNotificationChecker` service, letter type, letter body template, integration into Renewal issuance flow.
- Test: age boundary (24.99 / 25.00 / 25.01) + New Business Reason gating (NB/CoP/CoC/Midterm all skip).

### PH-08b — Section 3: Premium Summary & Collection Table UI polish (~1 week)
- Purely presentational — the underlying data is already computed by Phase 4. This sub-phase surfaces it in Razor.
- See [section-3-premium-tables.md](../sections/section-3-premium-tables.md).
- Deliverable: partial views, formatters, red-highlighting when shortfall > 0.

### PH-08c — Section 4: Change of Plan (Upgrade / Downgrade) (~2-3 weeks)
- Mapping matrix (from Excel per doc) as data-driven table in `MOHProject.Domain/Configuration/CopMappingMatrix.cs`.
- Upgrade vs downgrade flow branch. "Copy Exclusion" and "Copy Risk Loading" buttons per §4.3.
- See [section-4-change-of-plan.md](../sections/section-4-change-of-plan.md).
- Test: matrix coverage — every allowed transition in Excel has a test row.

### PH-08d — Section 5: Change of Citizenship (~2 weeks)
- Copy logic from previous policy per §5.4. Auto-route to UW per §5.3. CoC-specific CLOA rules on renewal per §5.7.
- See [section-5-change-of-citizenship.md](../sections/section-5-change-of-citizenship.md).
- Test: copy fidelity (source policy → new policy field-by-field), FR-specific NTU/Decline/Postpone flows.

### PH-08e — Section 6: FR Process Flow (~1 week)
- Consolidates FR-specific rules already scattered across §1 and §5. Mostly documentation + coverage check.
- See [section-6-fr-process-flow.md](../sections/section-6-fr-process-flow.md).
- Deliverable: cross-cutting audit — every FR × FR terminal-substatus path exercised by at least one test.

### PH-08f — Section 1.7: Midterm AOR (~3 weeks — separate bounded context)
- **New pipeline**: uses `ProductStatus`, `PremiumNotificationLetter`, no `Substatus`/LOA/CLOA. Should NOT reuse Phase 2 evaluator — different semantics.
- See [section-1-7-midterm.md](../sections/section-1-7-midterm.md).
- Deliverable: `MidtermAdditionOfRiderService`, own state machine, own letter types, own reminder rules.
- Reuses: `IPremiumCollectionRecalculator` (with a Midterm variant), `IAuditTrailWriter`, `ILetterGenerator` (extended with Midterm letter types).

## Cross-cutting deliverables

- [ ] **PH-08-01** Bumped `LetterType` enum with new values from each sub-phase.
- [ ] **PH-08-02** Updated Fluent API and a single migration `AddOtherSectionsSupport`.
- [ ] **PH-08-03** UI routes and navigation for each sub-phase's feature.

## Definition of Done
- Each sub-phase individually shippable (feature-flag if needed).
- No sub-phase leaks its logic into Section 1's Phase 6 code — respect the boundary.
- Regression pass on all 4 UAT bugs from earlier phases still green.

## Sequencing guidance
- Start with **PH-08a** (Medisave) — smallest, fastest to demo, standalone.
- Then **PH-08f** (Midterm) — largest and most isolated; benefits from long lead time.
- **PH-08c** (CoP) and **PH-08d** (CoC) after Phase 7 UI, since they need UI to be usable.
- **PH-08b** (Premium tables) and **PH-08e** (FR flow) are polish — do last.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
