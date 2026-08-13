# t005 — NPC Treasure Discovery & Investigation

## Tracking

- **ID:** t005
- **Status:** Complete
- **Milestone:** Expedition Loop
- **Depends on:** t004 — Treasure Prop + Treasure Socket
- **Blocks:** t006 — NPC Carried Treasure / Visit Reward

## Type

Feature

## Summary

Connect the existing treasure POI to NPC behavior so an adventurer that physically reaches a cell containing available treasure recognizes it as a meaningful investigation target, pauses through the existing investigation flow, and resolves the treasure exactly once when investigation completes.

This ticket is about discovery and investigation. It does not yet award or carry the treasure value.

## Current Behavior

Treasure props can be authored in cells, expose themselves through `DungeonPointOfInterest`, carry a prototype reward value, and transition to resolved state. NPC traversal can query POIs and has an investigation pause mechanism, but treasure resolution is not yet owned by the completed NPC investigation flow.

## Desired Behavior

When an NPC reaches a cell containing an available treasure POI, the NPC should recognize that treasure using only information available from the reached cell, perform the existing investigation pause, and resolve the associated `TreasureProp` after the investigation successfully completes.

Once resolved, the same treasure should not trigger another investigation on later traversal.

## Requirements

- Reuse the existing POI discovery and NPC investigation flow rather than adding treasure-specific scene searches.
- Only discover treasure after the NPC reaches/enters the containing cell; do not grant knowledge of remote treasure.
- Associate the investigation target with the concrete `TreasureProp`/POI being investigated.
- Use the POI's configured investigation duration.
- Resolve treasure only when the investigation completes successfully.
- Ensure `TreasureProp.TryResolve()` occurs at most once for a given treasure instance.
- After resolution, later visits through the cell should not pause for that resolved treasure.
- Preserve the existing behavior of cells without available POIs: traversal remains continuous.
- Keep the implementation compatible with future POI types without making `NPCTraversal` a treasure-only system.

## Acceptance Criteria

The ticket is complete when:

- An NPC entering a cell with available treasure initiates the existing investigation behavior.
- The NPC pauses for the treasure POI's configured investigation duration.
- The correct treasure instance resolves when investigation completes.
- The treasure's visible resolved state is applied through the existing t004 behavior.
- The treasure is no longer returned as an available POI after resolution.
- Traversing the same cell again does not trigger a second treasure investigation.
- An empty cell or cell containing only resolved POIs does not create an investigation pause.
- Treasure in an unvisited/remote cell does not influence NPC behavior before discovery.
- Existing trap and traversal behavior remains unchanged.

## Relevant Systems / Files

Investigate before implementation. Likely relevant areas include:

- `NPCTraversal`
- `DungeonPointOfInterest`
- `TreasureProp`
- `TileGridGenerator` POI queries
- Existing investigation decision/event flow

These are starting points, not a prescribed file-change list.

## Constraints

- Do not add carried treasure or persistent reward settlement; that belongs to t006.
- Do not add treasure-seeking pathfinding or known-treasure prioritization; that belongs to later exploration tickets.
- Do not add global POI knowledge.
- Do not redesign the POI foundation unless a narrowly scoped correction is required for this ticket.
- Do not introduce a generalized encounter state machine unless the current architecture demonstrably requires one.

## Implementation Notes

Prefer an investigation context/target reference that lets the existing investigation lifecycle know what is being resolved. The traversal layer may coordinate the interaction, but treasure-specific state should remain on `TreasureProp`.

If investigation can be interrupted/cancelled in the current architecture, treasure must remain unresolved until successful completion.

## Manual Test Scenario

1. Generate/load a dungeon containing an authored treasure chest reachable from the entrance.
2. Start an adventurer visit.
3. Verify the NPC traverses normally until physically entering the treasure's cell.
4. Verify the NPC pauses for the configured treasure investigation duration.
5. Verify the treasure resolves exactly once at completion and displays its resolved visual.
6. Allow or force the NPC to traverse that cell again.
7. Verify the resolved treasure does not cause another pause.
8. Verify an unvisited treasure elsewhere in the dungeon does not alter the NPC's route or behavior.
9. Verify ordinary cells remain continuous-transit cells.

## Out of Scope

- Adding reward value to the NPC
- Persistent economy settlement
- Treasure attraction/path priority
- Multiple-POI prioritization policy
- Treasure rarity or inventory
- Final treasure animation/art

## Implementation Status

- Added a generic POI investigation-completion contract so traversal retains a
  concrete target without depending on treasure-specific types.
- NPCs select the available POI only after physically entering its cell, use
  that target's configured investigation duration, and complete it only after
  the stamina-backed wait finishes successfully.
- `TreasureProp` implements the POI completion contract and retains ownership of
  resolve-once state and its existing resolved visual.
- Interrupted or stamina-exhausted investigations leave treasure available;
  resolved treasure is excluded by the existing available-POI query and cannot
  trigger a second investigation.
- Runtime compilation, focused source validation, and the manual Unity scenario
  were completed successfully on 2026-08-12.

## Git

Suggested branch:

`feature/t005-npc-treasure-investigation`

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
