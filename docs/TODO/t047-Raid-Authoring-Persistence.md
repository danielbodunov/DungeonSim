# t047 — Persist authored dungeons safely

## Tracking

- **ID:** t047
- **Preview alias:** SAVE-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t035](t035-Dungeon-Lifecycle-and-Modes.md), [t037](t037-Raid-Entrance-and-Treasure-Placement.md), [t041](t041-Raid-Trap-Framework.md), [t045](t045-Prototype-Melee-Minion.md)
- **Branch:** `feature/t047-raid-authoring-persistence`

## Goal and Scope

Extend GameSaveManager with authored layout/objectives/traps/minions, stable IDs, revision and compatible validation metadata. Do not overwrite initial conditions with runtime state.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Round-trip restores configuration and placement without rerolling content.
- [ ] Working data, published data and transient attempt state are distinct.
- [ ] Missing/incompatible definitions fail with actionable errors before partial restore.
- [ ] Legacy management saves have an explicit migration or rejection policy; old Dread/loot is not silently converted.
- [ ] Proof is invalidated if gameplay definitions/rules change, including changed footprint definitions.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Round-trip each content type; load missing definitions and legacy saves; retry after load.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Save_System.md](../Architecture/Save_System.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
