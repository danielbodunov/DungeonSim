# DEV008 — Single Authoritative Entrance Placement

## Tracking
- **ID:** DEV008
- **Status:** Planned
- **Milestone:** Developer Tooling / Building Rules
- **Depends on:** DEV007

## Goal
Enforce a simple 0-or-1 entrance invariant. Placing a new entrance should replace the existing entrance, while explicit manual removal remains available.

## Requirements
- Allow at most one active dungeon entrance.
- Validate the proposed replacement before destroying/removing the existing entrance where practical.
- On successful placement of a new entrance, remove the previous entrance and make the new entrance authoritative.
- Preserve the ability to manually remove the current entrance without immediately placing another.
- Apply the invariant consistently to normal placement, save/load, and test scenario capture/apply.
- NPC spawn/return systems must always resolve the current authoritative entrance.

## Acceptance Criteria
- Placing entrance B while entrance A exists leaves only B.
- An invalid attempted replacement leaves A intact.
- Manual removal can leave the dungeon with zero entrances.
- Save/load and scenario load never create duplicate active entrances.
- NPC spawn/return behavior follows the replacement entrance.

## Out of Scope
- Multiple entrances
- Entrance selection UI
- Entrance upgrade/progression mechanics

## Manual Validation
Place, replace, reject an invalid replacement, manually remove, save/reload, and scenario capture/load while verifying the 0-or-1 invariant.

## Git
Suggested branch: `tool/dev008-single-entrance`

Proceed according to `docs/AGENTS.md`.
