# t009 — Soul / Aura Harvesting Foundation

## Tracking

- **ID:** t009
- **Status:** Planned
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t005
- **Blocks:** t010 — Expedition Outcomes

## Summary

Establish the first explicit connection between adventurer experiences in the dungeon and the sinister force's Aura/soul-energy progression resource.

Death should provide meaningful Aura, but the architecture should allow other qualifying dungeon experiences to generate energy later without requiring a rewrite.

## Requirements

- Define a narrow authoritative Aura-harvest event/service/path using the existing Aura economy where appropriate.
- Award a configurable prototype Aura amount for adventurer death inside the dungeon.
- Prevent duplicate harvest from repeated death callbacks.
- Preserve source/context information sufficient to add other harvest events later.
- Expose harvested Aura clearly enough for debugging.
- Keep loot recovery and Aura harvesting as separate consequences.

## Acceptance Criteria

- Adventurer death inside the dungeon produces the configured Aura harvest exactly once.
- The existing persistent Aura total changes through its established authoritative mutation path.
- Repeated death/cleanup events cannot duplicate the harvest.
- Loot recovered from the adventurer does not itself automatically become Aura.
- The implementation can later support non-death harvest sources without changing the core currency API.

## Constraints

- Do not implement a large fear/pain/emotion simulation.
- Do not add many harvest event types yet; death is the prototype source.
- Do not couple Aura amount to treasure value.
- Do not implement reputation/notoriety.

## Manual Test Scenario

Record the current Aura total, kill one adventurer inside the dungeon, and verify the configured amount is added exactly once. Trigger cleanup/repeated defeat paths and verify no duplicate award. Confirm recovered treasure remains separate from Aura.

## Future Direction

Later tickets may consider smaller Aura generation from meaningful expedition events such as exploration, fear, traps, injury, combat, or magic use. Those should be introduced only when they improve the management loop and can be balanced coherently.

## Git

Suggested branch: `feature/t009-soul-aura-harvesting`

Proceed according to `docs/AGENTS.md`.
