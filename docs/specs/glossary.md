---
id: GLOSSARY
version: 0.1
status: draft
source: MOH_AdditionOfRiders_Analysis.html v0.1
last_updated: 2026-07-11
---

# Glossary — MOH SHIELD

Alphabetical. New terms added at the bottom of their letter section.

## A

- **Accept CLOA** — UW field with values `BLANK` / `Yes`. Signals whether the customer has agreed to conditional loading/exclusion. Combined with substatus to decide next state.
- **Active Plan** — a plan whose `ProductStatus` is not one of {Declined, Postponed, Not Taken Up, Terminated, Cancelled, Pending Termination, Pending Cash Refund, Draft}.
- **AOR** — Addition of Rider(s). The user story this project centers on.
- **APS** — Attending Physician's Statement. A UW decision that requests more medical evidence before deciding.
- **Ack Page** — Acknowledgement page attached to CLOA. Required when customer must sign.
- **AllStandard** — a value of `RiskComposition`. All active plans have Risk Category = Standard.
- **Audit Trail** — append-only log of every state transition and letter emission.

## B

- **Base Plan** — the primary insurance plan on a policy. Every policy has exactly one Base Plan.

## C

- **Cancer Guard Rider (CGR)** — a rider type. Referenced in Renewal exception for RCMP retention.
- **Choice Rider** — a rider type. Referenced in Renewal exception for RCMP retention.
- **CLOA** — Conditional Letter of Acceptance. Issued when at least one plan is Substandard (Exclusion or Loading). Two variants tracked separately: **CLOA (Exclusion)** and **CLOA (RCMP)**.
- **CoC** — Change of Citizenship. Section 5.
- **CoP** — Change of Plan. Section 4 (Upgrade / Downgrade with mapping matrix).
- **Complete UW** — a UW-tab checkbox. Auto-selected by the system in various flows (see FR-AOR-XXX).
- **CPF** — Central Provident Fund (Singapore). Payer of Medisave. External system.
- **CPF Rejected** — CPF returned a rejection for an IP file submission. Triggers substatus `PENDING IP RESPONSE FILE (CPF REJECTED)`.
- **Current UW completion date** — the date the current UW cycle finished. Used to exclude riders whose NTU/Decline/Postpone happened *before* the current cycle.

## D

- **Declined** — UW decision: plan is refused, no coverage. Generates Decline Letter.
- **Dustbin** — UI action in Midterm flow to remove a rider (section 1.7.4). Undoes prior status.

## E

- **EvaluateRemainingPlans()** — the missing orchestrator that is the root cause of all 4 UAT bugs. Recomputes composition, RCMP fields, substatus, and letters after any NTU/Decline/Postpone/Re-add. See `FR-AOR-030`.
- **ExclusionOnly** — a value of `RiskComposition`. At least one plan has an Exclusion; no plan has active Risk Loading.
- **Excess Premium** — `Collected − ToCollect > 0`. Moves to Unallocated Cash and triggers Refund of Excess Premium Letter.
- **Extra Loading** — synonym for Risk Loading on the base plan; special-cases RCMP retention on Renewal.

## F

- **Final Reminder** — the last reminder sent for an outstanding LOA/CLOA before enforcement action.
- **FR** — Foreign Resident. Not SG citizen and not PR. Residency affects the terminal substatus rule (FR×FR → Policy Incepted).

## G

- **Greyed** — UI state: field is visible but disabled (uneditable). Distinct from **Unticked** (visible + editable + not checked).

## H

- **HasRcmp** — a value of `RiskComposition`. At least one active plan has both Risk Loading *and* Exclusion. Highest priority in composition evaluation.

## I

- **IP** — Integrated Plan. Private portion of SHIELD paid via Medisave through CPF.
- **IP Request File** — the file the system generates to submit an IP application to CPF.
- **IP Response File** — the file CPF returns (accept or reject).
- **Insured** — the person covered by the policy. May differ from Policyholder and Payer.

## L

- **LOA** — Letter of Acceptance. Issued when all active plans are Standard. Non-conditional.
- **Loading** — see Risk Loading.

## M

- **Manual UW** — Underwriting decision made by a human underwriter. All non-STP flows land here first.
- **Medical Evidence Letter** — letter generated when UW decision = APS.
- **Medisave** — CPF sub-account used to pay SHIELD premiums.
- **Midterm** — an AOR performed during an active policy year (not at NB or Renewal). Uses **Product Status** + **Premium Notification Letter**, not Substatus + LOA/CLOA.

## N

- **NB** — New Business. First-time policy inception.
- **NTU** — Not Taken Up. Customer walked away before finalising. Generates NTU Letter (with or without refund).

## P

- **Payer** — the entity that pays the premium. May be Policyholder, Insured, or a third party. Residency of payer affects terminal substatus rule.
- **PENDING CASH COLLECTION** — a `PolicySubstatus`. There is a positive shortfall; awaiting cash.
- **PENDING IP REQUEST FILE** — a `PolicySubstatus`. All Standard, no shortfall, at least one of insured/payer is SG/PR — waiting for CPF submission.
- **PENDING IP RESPONSE FILE (CPF REJECTED)** — a `PolicySubstatus`. CPF rejected the IP submission.
- **PENDING MANUAL UNDERWRITING** — a `PolicySubstatus`. Awaiting UW human decision. Every AOR transits here after Save on the UW tab.
- **PENDING PP REQUEST/RESPONSE FILE** — NTU-only entry substatuses (PP = Payment Processing).
- **PENDING UW APS** — awaiting attending-physician evidence.
- **PENDING UW CLOA ASSESSMENT** — awaiting UW assessment of a CLOA reply from customer.
- **Policyholder** — the entity that owns the policy. Legally responsible.
- **POLICY INCEPTED** — terminal substatus. Both insured and payer are FR.
- **Postponed** — UW decision: refuse for now, may reconsider later. Generates Postponement Letter.
- **Premier Rider** — a rider type. Referenced in multiple UAT bugs.
- **Premium Collection** — tables of To Collect / Collected / Shortfall, per Base + Linked Rider(s).
- **Premium Notification Letter** — the Midterm equivalent of LOA. Used for Midterm AOR only.
- **Product Status** — the plan-level status (Active / NTU / Declined / Postponed / etc). Distinct from `PolicySubstatus` which is policy-level. Midterm operates on Product Status; NB/RN/CoP/CoC operate on Substatus.
- **PR** — Permanent Resident (Singapore). Treated like SG for terminal-substatus rules.

## R

- **RCMP** — Risk-Coded Medical Plan. Substandard plan with both Risk Loading and Exclusion. Also the name of a UW field (RCMP Flag / RCMP Option).
- **RCMP Flag** — UW field. Ticked when at least one plan is RCMP composition. Values: `Ticked+Enabled` / `Unticked+Enabled` / `Untick+Greyed`.
- **RCMP Option** — UW selection (Option 1 / Option 2) representing which loading offer the customer accepted.
- **Re-add** — user removes a rider (NTU/Decline/Postpone) then adds it again in the same session.
- **Reminder** — automated follow-up letter (LOA Reminder / CLOA Reminder), scheduled from the letter's issue date.
- **Refund of Excess Premium Letter** — new letter type introduced by this enhancement. Triggered when excess premium moves to Unallocated Cash.
- **Renewal (RN)** — a policy year rollover. Some rules differ from NB (e.g. RCMP retention when Base = Extra Loading).
- **Residency** — SG / PR / FR. Determines terminal-substatus rule.
- **Reversal of Substatus** — the process of re-evaluating substatus after any rider event. Section 1.5 in source doc.
- **Risk Category** — plan-level classification: `Standard` / `Substandard (Exclusion)` / `Substandard (Loading)` / `Substandard (Both = RCMP)` / `Declined` / `Postponed`.
- **Risk Loading** — extra premium loading applied to a Substandard plan.
- **Rider Plan** — an optional add-on plan attached to a Base Plan. A policy can have 0..N rider plans.

## S

- **SG** — Singapore Citizen. Treated identically to PR for terminal-substatus rules.
- **SHIELD** — the MOH health-insurance product family this system serves.
- **Shortfall** — `ToCollect − Collected`. Must never be negative; excess must move to Unallocated Cash instead.
- **Standard** — Risk Category: no loading, no exclusion. Passes UW cleanly.
- **STP** — Straight-Through Processing (not used in AOR — all paths land in PENDING MANUAL UW).
- **Substandard** — Risk Category: has loading, exclusion, or both.
- **Substatus** — see `PolicySubstatus`. Policy-level state used by NB/RN/CoP/CoC.

## T

- **Terminal substatus** — the end state for a happy-path flow: `PENDING IP REQUEST FILE` (if any SG/PR) or `POLICY INCEPTED` (if both FR).
- **To Collect** — the amount owed to the insurer for the current cycle.

## U

- **Unallocated Cash** — a holding bucket for excess premium pending refund/reallocation.
- **Underwriter (UW)** — human role responsible for deciding APS / Standard / Substandard / Declined / Postponed / NTU.
- **UW State** — the collection of UW-tab fields: Risk Category, RCMP Flag, Accept CLOA, RCMP Option, Complete UW.
