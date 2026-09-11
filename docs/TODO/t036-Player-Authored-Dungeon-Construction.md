# t036 — Adapt existing grid construction

## Tracking

- **ID:** t036
- **Preview alias:** BUILD-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t035](t035-Dungeon-Lifecycle-and-Modes.md)
- **Branch:** `feature/t036-player-authored-dungeon-construction`

## Goal and Scope

Reuse TileGridGenerator, TilePlacement, socket checks, service reservations and tile resolution. Player-authored topology is authoritative; WFC only resolves compatible presentation.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Place, remove and rotate supported pieces with clear invalid-placement feedback.
- [ ] Reject overlaps, incompatible sockets and reserved-space violations atomically.
- [ ] Only committed edits invalidate validation; rejected edits do not alter revision or resources.
- [ ] Authoring data can be captured without transient previews.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Build a small layout, reject conflicting placements, remove/rotate a tile and inspect revision changes.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Dungeon_Generation.md](../Architecture/Dungeon_Generation.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
