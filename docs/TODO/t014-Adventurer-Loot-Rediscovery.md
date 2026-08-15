# t014 — Adventurer Loot Rediscovery

## Tracking
- **ID:** t014
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t013

## Goal
Allow later adventurers to discover and acquire physical loot left by previous failed expeditions, creating persistent consequences and emergent expedition chains.

## Requirements
- Physical loot drops participate in the existing POI/discovery model.
- Adventurers must physically discover a drop; no remote knowledge.
- Investigation/acquisition transfers the drop's contents into the new adventurer's authoritative carried-loot state.
- Claimed drops resolve exactly once and disappear/transition appropriately.
- Preserve original provenance where useful so later outcomes can distinguish dungeon bait from resources brought/dropped by adventurers.
- Do not make NPCs omniscient about old drops.

## Acceptance Criteria
- Adventurer B can discover loot dropped by Adventurer A.
- B can acquire it through normal investigation/POI flow.
- The original world drop cannot be claimed twice.
- If B later dies, the acquired contents can enter the normal death-drop lifecycle again.
- If B escapes, the acquired contents follow the normal escape/loss lifecycle.

## Out of Scope
- Sophisticated loot desirability scoring
- Contested pickup/combat
- Player recovery interaction (t015)

## Git
Suggested branch: `feature/t014-loot-rediscovery`
