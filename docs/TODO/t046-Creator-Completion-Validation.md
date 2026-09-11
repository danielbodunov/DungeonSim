# t046 — Require a successful creator run

## Tracking

- **ID:** t046
- **Preview alias:** VALID-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t035](Complete/t035-Dungeon-Lifecycle-and-Modes.md), [t040](t040-Treasure-and-Escape-Objective.md), [t042](t042-Prototype-Spike-Trap.md), [t045](t045-Prototype-Melee-Minion.md)
- **Branch:** `feature/t046-creator-completion-validation`

## Goal and Scope

Validate a frozen working revision with the same fixed Warrior and gameplay rules as ordinary raids.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Entrance → treasure → same exit alive produces proof bound to revision and gameplay content/rules version.
- [ ] Death or abandon creates no proof and grants no rewards.
- [ ] Build, debug, save-restore shortcuts and mid-run edits cannot produce valid proof.
- [ ] Any accepted authoring edit clears proof; status and rejection reasons are visible.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test valid completion, death, abandon, stale revision and privileged-action rejection.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
