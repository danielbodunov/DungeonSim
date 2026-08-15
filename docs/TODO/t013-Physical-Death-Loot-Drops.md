# t013 — Physical Death Loot Drops

## Tracking
- **ID:** t013
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t012; existing t007 recovery records

## Goal
Represent recoverable adventurer death loot as a persistent world object at the death location instead of only an abstract recovery record.

## Requirements
- Materialize authoritative recoverable loot as a world-space loot pile/bag/drop at the recorded death cell/position.
- The world object references/summarizes the existing recoverable loot data rather than becoming a competing ownership ledger.
- Preserve item identity, value, origin, and source-cell information needed by later recovery/rediscovery systems.
- The drop remains present until an authoritative consumer claims/resolves it.
- Integrate with save/load if the drop can survive across saved game state.
- Make the object discoverable/interactable through a generic POI-compatible path suitable for t014/t015.

## Acceptance Criteria
- NPC death with carried loot creates one visible drop at the death location.
- Empty-handed death creates no phantom drop.
- Duplicate death processing creates no duplicate physical drop.
- Drop contents match the authoritative t007 recovery record.
- The drop can be queried by later NPC/player systems without special-casing the original dead NPC.

## Out of Scope
- Other adventurers taking the loot (t014)
- Player recovery UI (t015)
- Detailed item models for every contained resource

## Git
Suggested branch: `feature/t013-physical-death-loot`
