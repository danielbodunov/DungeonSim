# t015 — Player Recovery Phase

## Tracking
- **ID:** t015
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t013

## Goal
Give the player an explicit between-expedition cleanup/recovery interaction for physical resources left in the dungeon rather than automatically converting all aftermath into ledger values.

## Requirements
- During an appropriate non-expedition/build phase, allow the player to identify recoverable physical loot remaining in the dungeon.
- Player can inspect/select a drop and deliberately recover its contents.
- Recovery transfers contents through an authoritative economy/inventory boundary and resolves the world drop exactly once.
- Make the action and recovered value legible in the world/UI.
- Keep the first pass lightweight; recovery should not require a full character-controlled cleanup simulation.
- Define behavior for unrecovered drops when the next expedition begins.

## Acceptance Criteria
- A death drop can remain after an expedition and be selected by the player.
- Recovering it credits the appropriate physical resources/treasure and removes/resolves the drop.
- A resolved drop cannot be recovered twice.
- Unrecovered-drop behavior across phase transitions is deterministic and documented.

## Out of Scope
- Full inventory management UI
- Worker/minion cleanup jobs
- Complex salvage recipes

## Git
Suggested branch: `feature/t015-player-recovery`
