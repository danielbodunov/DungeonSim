# t041 — Adapt traps for real-time raiding

## Tracking

- **ID:** t041
- **Preview alias:** TRAP-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t036](t036-Player-Authored-Dungeon-Construction.md), [t038](Complete/t038-Shared-Raider-Abilities.md)
- **Branch:** `feature/t041-raid-trap-framework`

## Goal and Scope

Reuse existing mechanism/service/hazard footprints and placement checks. Define trigger, targeting, damage, reset/cooldown, initial state and always-active phase contract.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Placement and removal participate in revision invalidation.
- [ ] Shared damage events report trap identity for attribution.
- [ ] Restart restores trigger, cooldown and hazard state without stale projectiles.
- [ ] Existing structural trap attachment surfaces remain supported; no purchasable modifier attachments or upgrade sockets are added.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Place/remove traps, trigger with eligible/ineligible targets and restart repeatedly.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
