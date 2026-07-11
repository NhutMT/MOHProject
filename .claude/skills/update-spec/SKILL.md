---
name: update-spec
description: Merge a new or updated MOH requirement document into the existing spec files under docs/specs/ without duplicating FR IDs or breaking traceability. Trigger when the user says "requirement updated", "new version of the doc", "merge these changes into the specs", "spec doc changed", or provides a new HTML/PDF/markdown requirement doc to reconcile.
---

# Merging a new/updated requirement doc into `docs/specs/`

Specs use stable FR IDs (`FR-AOR-030`, `BUG-2610000310P`, `PH-02`). New requirement docs almost always overlap with existing IDs — the job is to diff **at the requirement level**, not to rewrite files.

## When this skill applies

- User provides a new version of `MOH_AdditionOfRiders_Analysis.html` (or the successor doc).
- User pastes a snippet from a requirement doc and asks to reconcile it.
- User says "the doc changed" / "new spec" / "update from BA".

## When NOT to use this skill

- Cosmetic edits to a spec (typo fix, formatting) — just edit.
- Adding an open question or a note — just edit the file.
- Bootstrapping specs from scratch — that's a fresh `docs/specs/` build, not a merge.

## Procedure

### 1. Identify the incoming doc
- Confirm file path (or ask the user to paste the source).
- Note its **version** and **date**. Every spec's frontmatter has `source: <filename> v<x>` — the incoming doc is either a version bump or a new file entirely.

### 2. Map the incoming content to existing specs
For each section/panel in the new doc:
- Find the matching spec file:
  - Section 1.x → [section-1-addition-of-riders.md](../../docs/specs/sections/section-1-addition-of-riders.md), [section-1-6-letter-combination.md](../../docs/specs/sections/section-1-6-letter-combination.md), [section-1-7-midterm.md](../../docs/specs/sections/section-1-7-midterm.md)
  - Section 2 → section-2-medisave-switch.md
  - Section 3 → section-3-premium-tables.md
  - Section 4 → section-4-change-of-plan.md
  - Section 5 → section-5-change-of-citizenship.md
  - Section 6 → section-6-fr-process-flow.md
  - Bugs / regression → bugs/uat-2026-06.md (or a new bugs file per UAT cycle)
- Find the matching FR ID inside the file. Grep by ID or by the requirement's short name.

### 3. Classify each incoming change
For each requirement in the new doc, decide:

- **UNCHANGED** — spec text and source lines still match. Just update `source_lines:` if line numbers shifted.
- **MODIFIED** — same requirement, different behavior. Edit the FR block in place. Bump the file's `version:`. Add a change-log row citing the FR ID.
- **NEW** — no matching FR. Add a new FR block with the next unused number for that prefix (e.g. if `FR-AOR-070` is the highest AOR FR, the next is `FR-AOR-071`). Add change-log row.
- **REMOVED** — the requirement was dropped. Do NOT delete the FR block. Change its `status:` to `deprecated`, add a **Reason** paragraph, and (if applicable) a **Superseded by:** reference. Add change-log row.

### 4. Bump metadata
For every file you touched:
- `version:` bump minor (e.g. `0.1` → `0.2`).
- `source:` update to the new doc filename + version.
- `source_lines:` update to the new line ranges.
- `last_updated:` set to today.

### 5. Update phase specs and CLAUDE.md
- If a change to an FR affects a phase's deliverables (e.g. new FR-AOR-071 requires implementation in Phase 3), update `docs/specs/phases/<n>-*.md`:
  - Add a deliverable checkbox.
  - Update its change log.
- If the change alters something CLAUDE.md advertises, edit CLAUDE.md too.

### 6. Update the traceability
- If UAT bugs are added, update `docs/specs/bugs/*.md` or create a new file per cycle (e.g. `uat-2026-09.md`).

### 7. Report
Emit a summary in chat:
```
## Merge summary — <incoming doc> vs docs/specs/

### Modified
- FR-AOR-030: <one-line what changed>
- FR-LTR-005: <...>

### Added
- FR-AOR-071 (new): <one-line summary>

### Deprecated
- FR-AOR-042: superseded by FR-AOR-071

### Files touched
- docs/specs/sections/section-1-addition-of-riders.md (0.1 → 0.2)
- docs/specs/phases/03-entry-point-state-machine.md (0.1 → 0.2)
```

## Hard rules

- **Never renumber an existing FR ID.** Even if the new doc reorders sections, the ID stays.
- **Never delete a deprecated FR block.** Keep it in the file with `status: deprecated`. History matters for auditors.
- **Never bulk-replace** whole sections. Diff at the FR level so a PR reviewer can trace each behavior change.
- **Never introduce a new prefix** without updating [docs/specs/README.md](../../docs/specs/README.md)'s ID scheme table.
- **Never merge without change-log entries.** Every touched file gets a row per meaningful change.
- **Never resolve an open question by inventing an answer** — mark it explicitly `resolved: <answer> (source: <who>, <date>)` if BA/PO answered, otherwise leave it open.

## Verification

After merging, run:
```bash
# All spec files still lint-clean (frontmatter parses)
grep -rL '^---$' docs/specs/*.md docs/specs/**/*.md
# Should be empty output.

# Every FR ID referenced from CLAUDE.md still exists
grep -oE 'FR-[A-Z]+-[0-9]+' CLAUDE.md docs/specs/**/*.md | sort -u | while read id; do
  grep -q "$id" docs/specs/**/*.md 2>/dev/null || echo "MISSING: $id"
done
```

Report the output. Non-empty means broken cross-references.

## Not your job

- Do not implement the requirement change in code as part of the merge. Merge specs first. Code changes go through the phase spec's deliverables.
- Do not decide "this new requirement is out of scope." Escalate to the user if the incoming doc adds a whole new area — you can propose scope changes but cannot approve them.
