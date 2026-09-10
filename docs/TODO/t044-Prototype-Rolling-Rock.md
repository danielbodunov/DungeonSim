# t044 — Prove controlled physics hazards

## Tracking

- **ID:** t044
- **Preview alias:** TRAP-04 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t041](t041-Raid-Trap-Framework.md)
- **Branch:** `feature/t044-prototype-rolling-rock`

## Goal and Scope

Implement one rolling-rock release/launch, collision damage, bounded lifetime, out-of-bounds cleanup and fresh-attempt reset.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Each run restores authored pose, velocity and fixed release parameters.
- [ ] Rock interacts with environment and valid targets without repeated unintended damage.
- [ ] Physics stepping and speed/collision limits produce reliable behavior across tested frame rates.
- [ ] Trigger, initial conditions, hits and cleanup are inspectable; do not promise bit-identical cross-platform physics.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test repeat launches, low/high frame rates, wedging, out-of-bounds recovery and restart mid-roll.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
