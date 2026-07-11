---
id: PH-07
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-06]
estimated_effort: 2-3 weeks (1 dev)
---

# Phase 7 — Web UI (MVC)

Build the ASP.NET Core MVC pages that surface the AOR flow to end users. Domain logic already complete from Phases 1–6 — this phase renders it.

## Goal
A working UI where a user can: open a policy, navigate the Underwriting tab, mark rider status (NTU/Decline/Postpone), Save, and see the substatus update + letters listed. Enough to demo the full flow end-to-end without external system integration (CPF is stubbed).

## Deliverables

- [ ] **PH-07-01** Policy dashboard (`GET /policies/{policyNumber}`) — read-only summary: substatus badge, insured/payer residency, active plans, letters, audit trail (last 10).
- [ ] **PH-07-02** Product tab (`GET /policies/{policyNumber}/product`) — list of plans with per-plan status; "Add Rider" button.
- [ ] **PH-07-03** Underwriting tab (`GET /policies/{policyNumber}/underwriting`) — form with RCMP Flag / Accept CLOA / RCMP Option / Complete UW fields. Disabled/greyed states matching `UWState` flags.
- [ ] **PH-07-04** **NTU / Termination Rider(s)** section (source lines 178-180 · Enhancement §Existing vs Enhanced): radio+checkbox UI. Only shows riders currently ticked in Product tab. Enables NTU/Decline/Postpone per rider.
- [ ] **PH-07-05** **Auto-route to UW** (source lines 211-235 · Enhancement 1): when user Saves UW tab with Risk Category = BLANK, the POST handler triggers `SaveUnderwritingTabCommand` (already implemented in Phase 3) which sets substatus to `PendingManualUw`. UI shows a confirmation toast: "Submit for underwriting review."
- [ ] **PH-07-06** Premium Summary + Premium Collection tables (§Section 3). Two tables side-by-side or stacked. Shortfall column highlights in red if > $0.
- [ ] **PH-07-07** Letters tab — list of all `Letter` rows for this policy. Filter: All / Current only. Preview link renders the Razor template.
- [ ] **PH-07-08** Audit trail viewer — table with actor, event, timestamp, expandable payload.
- [ ] **PH-07-09** ViewModels distinct from domain entities (`PolicyDashboardVm`, `UnderwritingTabVm`, etc.). No entity leakage to Razor.
- [ ] **PH-07-10** Anti-forgery tokens on every POST. Server-side validation via `ModelState` + FluentValidation (or DataAnnotations if simpler).
- [ ] **PH-07-11** Basic auth: whatever the deployment demands (Windows Auth for intranet, cookie auth for hosted). Not blocking for Phase 7 — placeholder allow-all in dev, hook into infrastructure choice later.

## Test suite

- [ ] **PH-07-T01** Controller tests with `WebApplicationFactory<Program>` for each POST endpoint. Assert 200 + redirect + updated DB row.
- [ ] **PH-07-T02** UI test (Playwright optional): full flow — open policy, add rider, mark NTU, Save UW → see substatus badge change.
- [ ] **PH-07-T03** Anti-forgery bypass test — POST without token returns 400.
- [ ] **PH-07-T04** ViewModel snapshot: Razor renders deterministic HTML for a given VM (helps detect accidental UI changes).

## Definition of Done
- End-to-end demo runnable locally with `/db-up` + `/run-web`.
- No domain entities exposed in Razor `@model` declarations.
- All happy-path tests green in CI.

## Out of scope
- Chart/graph visualisations.
- Bulk operations (multi-policy).
- Mobile responsive design (desktop-first; MOH ops team uses desktops).
- Custom auth provider integration — that's an infra decision.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
