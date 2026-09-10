# t042 — Implement a predictable spike hazard

## Tracking

- **ID:** t042
- **Preview alias:** TRAP-02 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t041](t041-Raid-Trap-Framework.md)
- **Branch:** `feature/t042-prototype-spike-trap`

## Goal and Scope

Adapt existing spike content where possible; make telegraph, active damage window and reset timing explicit.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Supported-surface placement and hazard volume agree.
- [ ] Visual activation and damage timing agree; hit frequency is bounded.
- [ ] Targeting/friendly-fire policy is configured and tested for Warrior and minion.
- [ ] Same authored start restores consistently on retry.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test safe edge, direct contact, repeated exposure, cooldown and reset.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
