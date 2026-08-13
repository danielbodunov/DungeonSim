# t011 — Sinister Dungeon Vertical Slice Validation

## Tracking

- **ID:** t011
- **Status:** Planned
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t002–t010
- **Blocks:** Meaningful Exploration / next roadmap batch

## Type

Integration / Gameplay Validation

## Summary

Validate the first complete expression of the game's core fantasy: the dungeon uses treasure as bait, an adventurer enters and explores using personal knowledge, treasure can be stolen, death can recover loot and harvest Aura, and escape can carry dungeon value away.

This ticket is primarily integration and gameplay-loop validation, not a container for broad new features.

## Validation Flows

### Death / Harvest Flow

1. Dungeon contains authored treasure bait.
2. Adventurer enters through the dungeon entrance.
3. Adventurer discovers treasure only after reaching it.
4. Adventurer investigates and takes the treasure.
5. Adventurer later dies in the dungeon.
6. Carried dungeon treasure becomes recoverable exactly once.
7. Death generates the configured Aura harvest exactly once.
8. Expedition completes as defeated/killed.

### Escape / Loss Flow

1. Adventurer enters and takes dungeon treasure.
2. Adventurer returns/exits successfully.
3. Carried treasure is lost from dungeon ownership.
4. No death harvest occurs.
5. Expedition completes with the appropriate non-death outcome.

### Regression Flow

- NPC personal traversal memory still governs familiar return routes.
- Unvisited treasure does not influence NPC decisions magically.
- Resolved/taken treasure does not trigger repeated investigation.
- Existing traps continue to function.
- Empty cells remain continuous transit.

## Gameplay Review

After correctness is established, assess:

- Does treasure feel like something the dungeon risks rather than a free reward?
- Is the difference between an adventurer stealing treasure and dying with it understandable?
- Does killing an adventurer feel materially valuable through both Aura and recoverable loot?
- Does an escape feel consequential without simply reading as a generic failure state?
- Can the player follow enough of the NPC's journey for a small story to emerge?
- Does dungeon layout appear capable of manipulating the adventurer's journey?

Record weaknesses as follow-up tickets rather than expanding this validation ticket indiscriminately.

## Constraints

- Do not implement reputation/notoriety yet.
- Do not implement advanced weighted exploration yet.
- Do not add combat, parties, equipment systems, or deep personality architecture.
- Do not implement tower-defense raids.
- Add only narrow integration fixes required to make t002–t010 function coherently.

## Git

Suggested branch: `feature/t011-sinister-dungeon-slice`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report.
