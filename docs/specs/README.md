# MOH SHIELD — Specifications

Specs for the MOH SHIELD Addition-of-Rider(s) enhancement and adjacent features (Medisave switch, Change of Plan, Change of Citizenship, FR flow).

Source of truth for what to build. Each spec has a stable ID so future requirement updates can be merged without duplicating.

## Structure

```
docs/specs/
├── README.md              ← you are here
├── glossary.md            ← domain terms (LOA, CLOA, RCMP, NTU, ...)
├── domain-model.md        ← enums, entities, value objects — SoT for code shapes
├── phases/                ← implementation plan, one file per phase
│   ├── 01-foundation.md
│   ├── 02-core-orchestrator.md
│   ├── 03-entry-point-state-machine.md
│   ├── 04-side-effects.md
│   ├── 05-reminders.md
│   ├── 06-reversal.md
│   ├── 07-web-ui.md
│   └── 08-other-sections.md
├── sections/              ← functional requirements, mapped to source doc
│   ├── section-1-addition-of-riders.md   (covers 1.1–1.5)
│   ├── section-1-6-letter-combination.md
│   ├── section-1-7-midterm.md
│   ├── section-2-medisave-switch.md
│   ├── section-3-premium-tables.md
│   ├── section-4-change-of-plan.md
│   ├── section-5-change-of-citizenship.md
│   └── section-6-fr-process-flow.md
└── bugs/
    └── uat-2026-06.md     ← 4 UAT regression scenarios
```

## Source document

- **File:** `MOH_AdditionOfRiders_Analysis.html`
- **Version referenced:** v0.1 (24/06/2026)
- **Origin ticket:** ITJR25120093 (03/12/2025)
- **Note:** MOH rolled the requirement back on 20/05/2026; this analysis is the re-approved version.

Every spec cites source line ranges via `source_lines:` frontmatter — when the source doc is updated, use these ranges to locate the drift.

## Stable ID scheme

IDs never change once assigned. If a requirement is removed, mark it `status: deprecated` and keep the ID.

| Prefix | Meaning | Example |
|---|---|---|
| `FR-AOR-###` | Addition of Rider(s) functional requirement | `FR-AOR-001` |
| `FR-LTR-###` | Letter generation rule | `FR-LTR-014` |
| `FR-RCMP-###` | RCMP flag / field logic | `FR-RCMP-003` |
| `FR-PREM-###` | Premium collection / allocation | `FR-PREM-007` |
| `FR-REM-###` | Reminder / final reminder rule | `FR-REM-002` |
| `FR-MID-###` | Midterm-specific | `FR-MID-001` |
| `FR-MED-###` | Medisave payer switch | `FR-MED-001` |
| `FR-COP-###` | Change of Plan | `FR-COP-004` |
| `FR-COC-###` | Change of Citizenship | `FR-COC-002` |
| `FR-FR-###`  | FR (Foreign Resident) flow | `FR-FR-001` |
| `PH-##` | Phase | `PH-02` |
| `BUG-####` | UAT / staging bug regression | `BUG-2610000310P` |
| `Q-###` | Open question waiting on BA/PO | `Q-001` |

Number ranges are per-prefix and monotonically increasing. **Do not reuse a retired number.**

## Spec file template

```markdown
---
id: FR-XXX-###
version: 0.1
status: draft            # draft | approved | in-progress | done | deprecated
source: MOH_AdditionOfRiders_Analysis.html v0.1
source_lines: 256-336    # line range(s) in source doc
last_updated: 2026-07-11
depends_on: [FR-XXX-###, PH-##]
owners: []               # BA/dev/PO owner names
---

# <Title>

## Purpose
1–2 sentences: why this requirement exists.

## Scope
- **IN:** what is covered
- **OUT:** what is not covered (route to another spec if relevant)

## Requirements
### FR-XXX-001 — <Short name>
- **Trigger:** …
- **Behavior:** …
- **Preconditions:** …
- **Postconditions:** …
- **Source:** lines A–B

(one section per requirement)

## Acceptance criteria
- [ ] AC-1: measurable outcome
- [ ] AC-2: …

## Test scenarios
- **TS-01:** …
- **TS-02:** …

## Open questions
- **Q-001:** …

## Change log
| Date       | Version | Change                     | Author |
|------------|---------|----------------------------|--------|
| 2026-07-11 | 0.1     | Initial draft              | Claude |
```

## How to merge a new/updated requirement doc

When you receive a new version of the source HTML (or a partial update):

1. **Locate the affected spec IDs.** Every FR has a stable ID. Grep the source line reference (`source_lines:`) or the section title against the new doc to find what shifted.
2. **Diff at the requirement level, not the file level.** For each affected `FR-XXX-###`:
   - Edit the requirement block in place.
   - Bump the file's `version:` in frontmatter.
   - Add a **change log** row: `date | version | "FR-XXX-042 changed: <what>" | author`.
3. **Never delete an ID.** If the new doc removes a requirement, keep the ID and set `status: deprecated` with a **Reason** and **Superseded by:** if applicable.
4. **New requirement → new ID.** Append at the next unused number for that prefix. Do not renumber existing IDs to make room.
5. **Cross-check phases.** If the changed FR affects a phase (`docs/specs/phases/*.md`), update that phase's deliverables checklist and change log too.
6. **Traceability.** Update `source:` and `source_lines:` in frontmatter to point at the new version of the doc.
7. **Diff review.** The whole point of stable IDs + frontmatter is that a PR diff of the specs is human-readable — no reshuffled sections, no renumbering.

Use the `update-spec` skill (`.claude/skills/update-spec/SKILL.md`) to drive this process consistently.

## Status lifecycle

```
draft → approved → in-progress → done
                 ↘
                   deprecated (any point)
```

- **draft:** written but not yet reviewed by BA/PO
- **approved:** reviewed and locked; dev may start
- **in-progress:** work has started (link to PR / branch)
- **done:** merged + tests green + demo'd
- **deprecated:** superseded or removed; body retained for history

## Quick index

- Reading order for a new dev: [glossary](glossary.md) → [domain-model](domain-model.md) → [phase 01](phases/01-foundation.md) → [phase 02](phases/02-core-orchestrator.md).
- Reading order for a BA/PO: [glossary](glossary.md) → any `sections/*.md` of interest.
- Reading order for a bug triager: [bugs/uat-2026-06](bugs/uat-2026-06.md) → [section-1-addition-of-riders](sections/section-1-addition-of-riders.md).
