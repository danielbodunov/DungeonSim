# DungeonSim Ticket

## Title

[Short, descriptive title]

## Type

[Feature / Bug Fix / Refactor / Tooling / Investigation]

## Goal

[1-3 sentences describing the observable result this ticket should produce.]

## Current Behavior

[Briefly describe the current behavior, or `Not applicable - new feature.`]

## Desired Behavior

[Describe the expected behavior after implementation. Focus on outcomes unless a specific implementation is required.]

## Requirements

- [Required behavior]
- [Required behavior]
- [Required behavior]

## Acceptance Criteria

The ticket is complete when:

- [Concrete, verifiable result]
- [Concrete, verifiable result]
- Existing unrelated behavior remains unchanged.

## Relevant Systems / Files

Known starting points:

- `[System / file / asset]`
- `[System / file / asset]`

Relevant documentation, if known:

- `[docs/...md]`

Start with these locations. Inspect additional files only when needed to trace dependencies, confirm ownership, or satisfy the ticket.

## Constraints

[Ticket-specific restrictions beyond the root `AGENTS.md`, or `None beyond AGENTS.md.`]

Examples:

- Preserve the existing Tile Profile data format.
- Do not modify prefabs.
- Existing saves must continue to load.
- Keep the feature Editor-only.

## Out of Scope

- [Related behavior that must not change]
- [Cleanup/refactor not included]

If there are no special exclusions:

`Anything not required by this ticket's requirements or acceptance criteria.`

## Reproduction Steps

[Bug tickets only, when applicable.]

1. [Step]
2. [Step]
3. [Step]

**Actual:** [Current result]

**Expected:** [Expected result]

## Implementation Notes

[Optional concise context, known helpers, architectural preferences, edge cases, screenshots, or error excerpts. Avoid pasting large files/logs that Codex can inspect directly.]

## Manual Unity Validation

[Optional but recommended when Editor/runtime behavior must be verified.]

1. [Open scene/prefab/tool]
2. [Configure state]
3. [Perform action]
4. [Expected result]

## Git

Suggested branch:

`[feature/fix/refactor/tool/investigation]/[short-ticket-name]`

Do not merge into `master`.

---

Proceed according to the root `AGENTS.md`.

Use the smallest sufficient context: begin with the systems/files/docs listed above and expand investigation only when required.

Use `docs/Reference/Codex_Workflow.md` for detailed ticket-state, completion-report, validation, and pull-request procedure when applicable.
