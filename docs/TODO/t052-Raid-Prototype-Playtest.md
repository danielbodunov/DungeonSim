# t052 — Validate the complete vertical slice

## Tracking

- **ID:** t052
- **Preview alias:** PROTO-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t043](t043-Prototype-Dart-Wall.md), [t044](t044-Prototype-Rolling-Rock.md), [t051](t051-Prototype-Raid-Resources.md)
- **Branch:** `feature/t052-raid-prototype-playtest`

## Goal and Scope

Assemble an authored dungeon with Warrior, treasure/exit, spikes, darts, rolling rock and melee minion. Record findings rather than expanding scope.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Build → validate → publish → separate-profile raid → result/reward → improve works without developer intervention.
- [ ] Success, failure, abandon, retry, save/load and edited-working-copy scenarios pass.
- [ ] All three traps and minion reset cleanly; published versions remain unchanged.
- [ ] Playtest report covers movement feel, hazard readability, construction friction and fun; unfinished criteria remain explicit.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Run full loop and failure cases in Unity; record version, actual results, known issues and go/no-go decision.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Design/Core_Game_Direction.md](../Design/Core_Game_Direction.md)
- [Roadmap/Core-Gameplay-Loop.md](../Roadmap/Core-Gameplay-Loop.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
