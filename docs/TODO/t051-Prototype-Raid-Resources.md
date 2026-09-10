# t051 — Close the construction/reward loop

## Tracking

- **ID:** t051
- **Preview alias:** ECON-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t050](t050-Raid-Result-Recording.md)
- **Branch:** `feature/t051-prototype-raid-resources`

## Goal and Scope

Adapt build-cost authority to a simple configurable construction currency, success rewards and defender rewards for credited raider kills.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Eligible success rewards the raider; credited trap/minion kill rewards the builder once.
- [ ] Validation, debug and self-validation runs grant no rewards.
- [ ] Persisted receipts keyed by attempt ID prevent duplicate grants, including after reload.
- [ ] Starter resources permit a first dungeon; accepted purchases debit atomically and rejected placements do not.
- [ ] No rare rewards, upgrades, talismans, attachments or online anti-farming system.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test success and defense payouts, duplicate delivery/reload, insufficient funds and spend-to-build.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Architecture/Save_System.md](../Architecture/Save_System.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
