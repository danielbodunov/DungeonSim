# t039 — Implement one fixed controllable Warrior

## Tracking

- **ID:** t039
- **Preview alias:** CHAR-02 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t038](Complete/t038-Shared-Raider-Abilities.md)
- **Branch:** `feature/t039-prototype-warrior-controller`

## Goal and Scope

Provide running, grounded jumping, gravity, landing, a basic melee attack, health, damage reactions, death and fresh-attempt restart. Include usable input and follow camera.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] The fixed Warrior traverses a representative side-view dungeon without build-camera input conflicts.
- [ ] Attacks and incoming damage use shared abilities; death ends the attempt once.
- [ ] Restart restores health and objective state with no residual velocity or cooldown.
- [ ] Use fixed 3D visuals/placeholder rig; no character selection, equipment inventory or customization prerequisite.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Play movement, jumps, attack, death and repeated restart; test foreground/background collision boundaries.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Architecture/Character_Visuals.md](../Architecture/Character_Visuals.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
