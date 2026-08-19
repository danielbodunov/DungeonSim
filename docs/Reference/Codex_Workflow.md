# Codex Ticket Workflow

This document contains detailed workflow guidance for implementation tickets. It is intentionally separate from the root `AGENTS.md` so routine tasks do not need to load this entire procedure.

Use this document when creating or executing a ticket, maintaining ticket status, preparing completion notes, or creating a pull request.

## Before Implementation

1. Confirm the current branch and working tree state.
2. Identify existing uncommitted changes and preserve unrelated work.
3. Create or switch to the appropriate dedicated ticket branch.
4. Read the ticket and any specifically referenced architecture/design documentation.
5. Locate the relevant systems and trace enough of the execution path to identify the current owner of the behavior.
6. Inspect call sites, related tests/debug tools, and nearby reusable functionality only as needed.
7. Establish the smallest likely file-change boundary before editing.

Avoid broad repository exploration when the ticket already identifies the relevant subsystem.

## Branch Naming

Use lowercase kebab-case with an appropriate prefix:

- `feature/`
- `fix/`
- `refactor/`
- `tool/`
- `investigation/`

Examples:

- `feature/manual-tile-placement`
- `fix-invalid-tile-connections`
- `refactor-generation-state-machine`
- `tool/dungeon-debug-visualizer`

Use an existing suitable ticket branch rather than creating a duplicate.

## Implementation

- Implement only what is required by the ticket and acceptance criteria.
- Extend an existing appropriate owner rather than creating parallel behavior.
- Make neighboring changes only when they are required for correctness.
- Avoid unrelated refactors, renames, cleanup, formatting, scene/prefab edits, and speculative abstractions.
- If a broader architectural change appears necessary, identify why it is required before expanding the implementation boundary.
- Preserve public APIs, serialized data, prefab/scene references, and deterministic generation behavior where practical.
- Keep temporary debug code temporary.

## Ticket Documentation Maintenance

When work is driven by a file in `docs/TODO/`, keep that ticket synchronized with the actual implementation state.

Update status when the work meaningfully changes state, for example:

- `Ready` → `In Progress`
- `In Progress` → `Awaiting Unity Validation`
- `Awaiting Unity Validation` → `Complete`
- use `Blocked` when work cannot proceed

Record material implementation notes, limitations, manual validation requirements, and concrete follow-up issues when useful.

Do not:

- change ticket scope after implementation begins unless explicitly requested;
- silently rewrite acceptance criteria to match the implementation;
- mark Unity validation complete unless Unity was actually tested;
- renumber ticket IDs;
- change roadmap priorities;
- modify unrelated ticket files;
- create speculative cleanup tickets merely because refactoring is possible.

Update `docs/TODO/README.md` only when required to keep the queue/status placement accurate.

## Validation

Run the narrowest relevant validation first. Broaden validation only when the change affects shared systems or the narrow validation is insufficient.

Relevant validation may include:

- compilation;
- focused automated tests;
- static code inspection;
- call-site inspection;
- null/reference analysis;
- serialization review;
- final diff review.

Never claim a validation step was performed when it was not.

Before completion, inspect the final diff and verify:

- every changed file belongs to the ticket;
- no unrelated formatting/refactoring was introduced;
- temporary debugging is removed or intentionally retained;
- generated files were not added;
- serialized changes are intentional;
- API changes are necessary;
- acceptance criteria are satisfied to the extent actually validated.

## Completion Report

Keep the completion report concise. Include detail where it materially helps review rather than mechanically expanding every section.

### Ticket

Ticket title.

### Branch

Current ticket branch.

### Status

`Complete`, `Partially Complete`, or `Blocked`.

### Summary

Briefly describe what changed and the resulting behavior.

### Files Changed

List changed files with a short reason for each.

### Key Implementation Details

Summarize important architecture, data-flow, algorithm, compatibility, generation, or Unity-specific decisions.

### Existing Systems Reused

List important reused/extended systems, or `None`.

### New Systems / APIs

List meaningful new classes, components, interfaces, public methods, serialized fields, ScriptableObjects, editor tools, or data structures, or `None`.

### Unity / Serialized Asset Changes

List scene, prefab, ScriptableObject, serialized field, `.meta`, or GUID changes, or `None`.

### Validation Performed

State exactly what was actually validated.

### Manual Unity Validation

Provide concrete remaining Editor test steps and expected results, or `None`.

### Known Limitations

List concrete remaining limitations, or `None`.

### Unrelated Issues Discovered

List concise out-of-scope issues suitable for future tickets, or `None`.

### Final Scope Check

Confirm that changed files are ticket-relevant, unrelated refactoring was avoided, and temporary changes were removed or intentionally retained.

## Pull Request

When implementation and available validation are complete and the task calls for publication:

1. Commit only ticket-relevant changes.
2. Push the ticket branch.
3. Create a pull request targeting `master`.
4. Do not merge the pull request unless explicitly instructed.
5. Base the PR title on the ticket title.
6. Use a condensed completion report for the PR description.
7. Include:
   - summary;
   - key implementation details;
   - files/systems affected;
   - validation performed;
   - manual Unity validation still required;
   - known limitations;
   - concrete unrelated issues discovered, if any.
8. Review the final diff before creating the PR.

## Context-Efficiency Guidance

- Start from files and documents explicitly named in the ticket.
- Do not read all architecture, reference, design, or TODO documents by default.
- Prefer targeted symbol/file searches to repository-wide reading.
- Do not paste or restate large source files into working notes when they can be referenced directly.
- Keep status updates and final reports concise unless additional detail is needed for review.
- For long-running Codex sessions, use context compaction between major phases when appropriate.
