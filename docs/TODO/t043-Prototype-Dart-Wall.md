# t043 — Implement the directional dart wall

## Tracking

- **ID:** t043
- **Preview alias:** TRAP-03 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t041](t041-Raid-Trap-Framework.md)
- **Branch:** `feature/t043-prototype-dart-wall`

## Goal and Scope

Use authored trigger direction, fixed launch parameters, collision damage and bounded projectile lifetime.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Orientation controls the actual projectile/hazard direction.
- [ ] Debug view exposes trigger and nominal projectile path.
- [ ] Darts hit eligible targets and environment without unbounded accumulation.
- [ ] Restart removes active darts and restores launcher state.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test both directions, wall interception, fast projectile collisions, expiry and retry.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
