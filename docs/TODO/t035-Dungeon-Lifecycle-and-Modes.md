# t035 — Separate authoring, validation and raid lifecycle

## Tracking

- **ID:** t035
- **Preview alias:** GAME-02 (conversation label only; existing IDs are not reused)
- **Status:** Ready
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](t034-Raid-Building-Game-Direction.md)
- **Branch:** `feature/t035-dungeon-lifecycle-and-modes`

## Goal and Scope

Extend existing mode ownership; keep working revision, validation proof, published versions and attempt state separate rather than one mutually exclusive enum.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] A published version remains playable while its working copy is edited.
- [ ] Every accepted authoring edit increments the revision and clears its proof; prototype cosmetic edits also invalidate.
- [ ] Build/debug actions are unavailable during validation and raids; invalid transitions fail without partial mutation.
- [ ] Restart, abandon, success and death have explicit transitions.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test edit after publish, failed/abandoned validation, stale proof and denied build commands.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
