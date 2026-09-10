# t045 — Add one defensive minion

## Tracking

- **ID:** t045
- **Preview alias:** MINION-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t036](t036-Player-Authored-Dungeon-Construction.md), [t038](t038-Shared-Raider-Abilities.md)
- **Branch:** `feature/t045-prototype-melee-minion`

## Goal and Scope

Place a minion with idle/short patrol, obstructed-line-of-sight detection, bounded pursuit, melee attack, damage/death and reset.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Warrior and minion can damage and kill each other through shared rules.
- [ ] Minion navigates supported prototype geometry and does not see through blocking walls.
- [ ] Unsupported jumps/climbs are rejected or pursuit stops safely; full NPC raid planning is not required.
- [ ] Placement edits invalidate proof; retry restores authored position, health and behavior.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test patrol, obstruction, pursuit limit, reciprocal combat and clean restart.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/NPC_Runtime.md](../Architecture/NPC_Runtime.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
