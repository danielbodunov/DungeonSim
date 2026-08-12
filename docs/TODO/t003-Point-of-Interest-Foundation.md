# t003 — Point-of-Interest Foundation

## Tracking

- **ID:** t003
- **Status:** Complete
- **Milestone:** Expedition Loop
- **Depends on:** t002 — Dungeon Entrance / Adventurer Spawn Contract
- **Blocks:** t004 — Treasure Prop + Treasure Socket

## Type

Feature / Architecture

## Summary

Create the minimum common representation for something in a dungeon cell that is worth an NPC investigating or interacting with. The immediate consumer is treasure; this ticket should not attempt to implement a complete encounter framework.

## Current Behavior

NPC traversal already supports an explicit investigation decision hook and the NPC behavior design identifies treasure, traps, doors, shrines, and similar content as possible meaningful investigation targets. There is not yet a small shared POI contract for cell content to expose that role consistently.

## Desired Behavior

A dungeon cell can report one or more available points of interest. NPC behavior can identify a POI as a meaningful target without needing treasure-specific logic in the traversal layer.

The POI representation should support only the information needed by the near-term expedition slice and remain extensible without speculative abstractions.

## Requirements

- Define a minimal POI/investigation-target contract appropriate to existing architecture.
- Associate a POI with its containing cell and world interaction position.
- Expose whether the target is currently available/resolved.
- Provide a stable way for NPC investigation logic to identify the target/type or behavior needed for decision making.
- Support an investigation duration/cost or equivalent hook if required by the existing investigation flow.
- Allow a cell/NPC query to find relevant POIs without scene-wide searches in normal behavior loops.
- Preserve separation between game-rule state and player-facing visual feedback.

## Acceptance Criteria

- A test POI can be authored in a dungeon cell.
- NPC/cell logic can discover that POI through the new contract.
- The POI exposes a usable interaction/investigation location.
- The POI can transition from available to resolved/unavailable without being destroyed if the content chooses to remain visible.
- The traversal layer does not need to know concrete treasure implementation details.
- Existing empty cells continue to behave as ordinary continuous-transit cells.
- No generalized combat, loot, party, or full encounter framework is introduced.

## Relevant Systems / Files

Investigate:

- NPC investigation decision hook
- Dungeon cell representation
- Existing prop/socket conventions
- Trap/action-resolution architecture where useful for consistency

## Constraints

- Implement the smallest useful abstraction.
- Do not introduce a large inheritance hierarchy for hypothetical POI types.
- Do not implement treasure reward logic yet.
- Do not make POI discovery grant global route knowledge.
- Avoid repeated scene-wide object searches.

## Manual Test Scenario

1. Place a simple test POI in a known dungeon cell.
2. Enter play mode and verify the containing cell can expose the POI.
3. Verify an NPC/investigation query can identify it as available.
4. Resolve/disable the POI.
5. Verify it is no longer returned as an unresolved investigation target.
6. Verify neighboring empty cells still produce no investigation stop by default.

## Out of Scope

- Treasure visuals/rewards
- Loot inventories
- Trap redesign
- Combat encounters
- Social encounters
- Personality-driven POI evaluation

## Implementation Status

- Added a composable `DungeonPointOfInterest` component with a stable type and
  optional identifier, interaction position, investigation duration, containing
  cell, and available/resolved state.
- Added a cell-indexed POI registry and query API to `TileGridGenerator`.
- Available POIs now activate the existing explicit NPC investigation step and
  provide its duration without introducing treasure-specific traversal logic.
- Availability changes remain independent from presentation; resolving a POI
  does not destroy or hide its object.
- Runtime source validation and the manual Unity scenario were completed
  successfully on 2026-08-11.
- The interaction position is exposed to investigation consumers. NPC approach
  movement toward that position is deferred to follow-up behavior work; the
  current traversal pauses at its cell-arrival position.

## Git

Suggested branch:

`feature/t003-poi-foundation`

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
