# t040 — Implement the playable raid objective

## Tracking

- **ID:** t040
- **Preview alias:** RAID-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t037](t037-Raid-Entrance-and-Treasure-Placement.md), [t039](t039-Prototype-Warrior-Controller.md)
- **Branch:** `feature/t040-treasure-and-escape-objective`

## Goal and Scope

Use SeekingTreasure, CarryingTreasure, Escaped and Dead attempt states; include clear objective feedback and a treasure-carried event.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Spawn at the authoritative entrance.
- [ ] Returning without treasure does not succeed; returning alive with treasure succeeds exactly once.
- [ ] Death fails; abandon is a distinct non-success outcome.
- [ ] Treasure is freshly available on restart; no treasure-triggered trap content is implemented.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test exit before pickup, pickup and escape, death carrying treasure and fresh restart.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
