# t049 — Raid as a separate local profile

## Tracking

- **ID:** t049
- **Preview alias:** RAID-02 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t048](t048-Immutable-Local-Publishing.md)
- **Branch:** `feature/t049-local-published-dungeon-raids`

## Goal and Scope

Add a minimal published-version launcher, separate builder/raider identity, attempt lifecycle, retry and results handoff.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Raid always loads a published version and cannot edit its source.
- [ ] Every attempt resets objectives, characters, minions and all hazards.
- [ ] Treasure plus live return is success; death and abandon are distinct terminal outcomes.
- [ ] Use local data only; no backend or elaborate browser is required.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Raid a published version as another profile; repeat success, death, abandon and restart.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Architecture/Save_System.md](../Architecture/Save_System.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
