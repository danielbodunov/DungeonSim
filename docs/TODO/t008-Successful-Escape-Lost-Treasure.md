# t008 — Successful Escape & Lost Treasure

## Tracking

- **ID:** t008
- **Status:** Planned
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t007 — Adventurer Loot Drop & Dungeon Recovery
- **Blocks:** t010 — Expedition Outcomes

## Summary

Define the escape-side consequence of treasure ownership. When an adventurer successfully exits the dungeon while carrying dungeon treasure, that treasure is genuinely lost from the dungeon rather than automatically returned or converted into a reward for the player.

## Requirements

- Detect authoritative successful exit through the dungeon entrance.
- Finalize carried dungeon treasure as escaped/lost exactly once.
- Clear the exiting adventurer's visit-local custody appropriately.
- Expose the escaped value/items for debug and future reputation/story systems.
- Keep successful escape distinct from death, retreat, and other future outcomes.

## Acceptance Criteria

- An adventurer escaping with treasure removes that treasure from dungeon ownership permanently for the current save/state.
- The treasure does not respawn automatically at its original socket.
- Escape processing cannot duplicate or restore the treasure.
- Escaping with no treasure produces no phantom loss.
- No Aura reward is granted merely because treasure escaped.

## Constraints

- Do not implement reputation/notoriety yet.
- Do not implement soul harvesting here.
- Do not treat escape as generic player failure; record the outcome neutrally for later systems.
- Do not add a full item economy.

## Manual Test Scenario

Have an adventurer take known treasure and successfully return to the entrance. Verify the treasure remains absent from the dungeon, is no longer recoverable as dungeon property, and the escaped amount is reported exactly once.

## Git

Suggested branch: `feature/t008-successful-escape`

Proceed according to `docs/AGENTS.md`.
