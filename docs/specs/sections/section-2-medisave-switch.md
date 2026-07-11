---
id: FR-MED
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 2027-2210
last_updated: 2026-07-11
depends_on: [DOMAIN-MODEL]
implements_in: [PH-08a]
---

# Section 2 — Notification for Switch of Medisave Payer

**🆕 NEW requirement** — chưa có trong hệ thống. Chỉ áp dụng Renewal.

## Purpose
Notify policyholder/insured that Medisave payer must switch (from parent's to insured's own account) when insured turns 25, per CPF/MOH Singapore rule.

## Requirements

### FR-MED-001 — Trigger conditions (ALL must hold)
| # | Condition | Value |
|---|---|---|
| 1 | New Business Reason | `PolicyType == Renewal` |
| 2 | Insured turns 25 in the Renewal cycle | `RenewalStartDate ≤ (DOB + 25 years) ≤ RenewalEndDate` |
| 3 | Additional condition per source lines 2060+ | (read source for exact 3rd condition) |

- **Source:** lines 2044-2100.

### FR-MED-002 — Conditions that suppress the letter
- Confirm from source lines 2100+ — usually: switch already completed / opt-out flag / non-Medisave payment method.

### FR-MED-010 — Delivery timing
- Letter is generated **together with the LOA/CLOA** at Renewal (not a standalone mail-out).
- **Source:** implied by "cùng với LOA/CLOA" note at line 2034.

### FR-MED-020 — Letter template
- New `LetterType.MedisavePayerSwitchNotification`.
- Body template supplied by BA (see [Q-MED-001](#open-questions)).

## Acceptance criteria
- [ ] AC-1: NB / CoP / CoC / Midterm never generate this letter.
- [ ] AC-2: Renewal with insured DOB exactly on cycle boundary triggers correctly (age boundary test: DOB+25y == RenewalStartDate → include).
- [ ] AC-3: Letter appears in the same batch as the Renewal LOA/CLOA (same `CorrelationId` prefix or same batch job).

## Open questions
- **Q-MED-001:** BA to provide letter template copy.
- **Q-MED-002:** How is "insured turns 25 in cycle" computed if cycle spans a leap-year boundary? Assume calendar year-comparison, not day-of-year.
- **Q-MED-003:** Third trigger condition — mình chưa đọc trọn phần này của source doc; xác nhận với BA hoặc đọc lại lines 2060-2210.

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
