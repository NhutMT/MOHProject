---
id: PH-05
version: 0.1
status: draft
last_updated: 2026-07-11
depends_on: [PH-04]
estimated_effort: 1-2 weeks (1 dev)
---

# Phase 5 — Reminders / Final Reminders

Automated LOA/CLOA reminder + final reminder scheduling with strict "new letter supersedes old" semantics.

## Goal
When a letter is issued, the system schedules reminder + final reminder jobs. When a superseding letter is issued (same policy, same context, later timestamp), all pending reminders from the old letter are cancelled and new ones are scheduled from the new letter's date.

## Deliverables

- [ ] **PH-05-01** Hangfire integration (SQL Server storage). Dashboard mounted at `/hangfire` behind an admin auth check (Windows auth or per-config; no anonymous).
- [ ] **PH-05-02** `IReminderScheduler` impl backed by Hangfire delayed jobs. Job payload: `{ policyId, correlationId, reminderType }` — job body reads the current DB state at fire time (do NOT snapshot state at schedule time).
- [ ] **PH-05-03** Reminder cancellation on supersession: `ILetterGenerator` (from Phase 4) calls `IReminderScheduler.CancelForAsync(oldCorrelationId)` inside the same transaction as marking `IsCurrent = false`.
- [ ] **PH-05-04** Reminder schedule rules (source lines 339-372):
  - New LOA → schedule LOA Reminder + LOA Final Reminder from LOA date if outstanding cash exists.
  - New CLOA → schedule CLOA Reminder + CLOA Final Reminder from CLOA date if shortfall or pending CLOA reply.
  - Substatus transitions to `PendingUwAps` → STOP all reminders.
  - RCMP NTU with remaining ExclusionOnly → STOP CLOA(RCMP) reminders, continue CLOA(Exclusion).
  - Exclusion NTU with remaining HasRcmp → STOP CLOA(Exclusion) reminders, continue CLOA(RCMP).
- [ ] **PH-05-05** Reminder cadence: TBD by BA — placeholder configurable via `appsettings.json` (`ReminderRules:LoaReminderOffsetDays`, `LoaFinalReminderOffsetDays`, etc.). See [Q-501](#open-questions).
- [ ] **PH-05-06** At-most-once semantics: `ReminderJob.Execute()` checks parent letter's `IsCurrent`; if false, exits without sending. Prevents race between scheduled fire and just-issued supersession.
- [ ] **PH-05-07** Audit trail: every reminder emission writes `ReminderSent` event; every cancellation writes `ReminderCancelled` event.

## Test suite

- [ ] **PH-05-T01** Supersession cancels pending reminders. Setup: schedule reminder for LOA_v1 → generate LOA_v2 → assert v1's Hangfire job status = Deleted, v2 scheduled.
- [ ] **PH-05-T02** Fire-time revalidation. Setup: schedule reminder → mark letter `IsCurrent = false` outside the scheduler (simulate race) → force job to run → assert no reminder row inserted.
- [ ] **PH-05-T03** Reminder rule table: substatus transitions × outstanding-cash flag = expected reminder set. 10-15 test rows.
- [ ] **PH-05-T04** Integration (Testcontainers + Hangfire in-memory storage) — end-to-end: issue letter, run scheduler, verify job fires, verify reminder letter row exists.

## Definition of Done
- Zero reminders sent from a superseded letter (verified by integration test).
- Hangfire dashboard accessible in Dev; disabled or auth-guarded in Prod.
- All reminder cadence values are configuration, not hardcoded.

## Out of scope
- Physical delivery of reminder letters (email, print) — persistence + audit only.
- Custom reminder rules per policy (e.g. per broker preference). Global config only.

## Open questions
- **Q-501:** BA to confirm reminder offsets: how many days from LOA date to LOA Reminder, and to Final Reminder. Doc lists rules but not exact days.
- **Q-502:** Do reminders roll over across weekends/holidays? Assume no unless BA specifies.
- **Q-503:** If a letter is issued at 23:59 SGT with reminder offset = 0 days, does the reminder fire same-day or next-day? Assume next-day (SGT midnight boundary).

## Change log
| Date       | Version | Change        | Author |
|------------|---------|---------------|--------|
| 2026-07-11 | 0.1     | Initial draft | Claude |
