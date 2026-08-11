# t004 — Treasure Prop + Treasure Socket

## Tracking

- **ID:** t004
- **Status:** Planned
- **Milestone:** Expedition Loop
- **Depends on:** t003 — Point-of-Interest Foundation
- **Blocks:** t005 — NPC Treasure Discovery & Investigation

## Type

Feature

## Summary

Add the first concrete point-of-interest content: an authored treasure prop that can occupy an appropriate prop/socket location in a dungeon cell, expose itself as an unresolved POI, carry a simple reward value, and transition to a resolved state.

Treasure is content hosted by a cell, not a special `TreasureCell` topology type.

## Current Behavior

DungeonSim does not yet provide a concrete treasure target for adventurers to discover. The POI foundation from t003 is expected to provide the semantic investigation contract.

## Desired Behavior

A tile can author a treasure-capable socket/location. A treasure prop placed there participates in the POI system and has simple inspectable reward/state data suitable for the next NPC investigation ticket.

Specialized treasure-room tiles may exist as content later, but NPC/gameplay systems should recognize the treasure prop/POI rather than a hard-coded tile classification.

## Requirements

- Define or extend an authored socket/location suitable for treasure placement using existing project conventions where possible.
- Add a simple treasure prop/component implementing the t003 POI contract.
- Associate the treasure with its containing dungeon cell.
- Expose a configurable prototype reward value.
- Expose unresolved/resolved state.
- Provide an authored interaction/investigation position if different from the prop origin.
- Provide a simple visible resolved behavior appropriate for testing (for example open, empty, disabled, or hidden) without requiring final art.
- Ensure resolved treasure is no longer offered as an unresolved POI.

## Acceptance Criteria

- A tile/prefab can author a valid treasure location.
- A treasure prop can be placed/authored at that location.
- The containing cell exposes the treasure through the POI system.
- Treasure has an inspectable/configurable reward value.
- Treasure begins unresolved and can transition exactly once to resolved state.
- Resolved treasure is not returned as an unresolved investigation target.
- Treasure does not require a dedicated `TreasureCell` tile type.
- Existing tile generation/topology behavior remains unchanged unless a minimal socket integration is genuinely required.

## Relevant Systems / Files

Investigate:

- t003 POI implementation
- Existing prop/socket authoring
- Dungeon cell/profile ownership
- Prefab conventions

## Constraints

- No item rarity tables.
- No player inventory.
- No procedural loot generation.
- No NPC treasure-seeking behavior yet.
- No economy settlement yet.
- Avoid changing tile topology solely to support treasure.

## Implementation Notes

Preferred conceptual separation:

- **Cell/tile:** topology and content host.
- **Treasure socket:** authored valid placement/location.
- **Treasure prop:** gameplay content and POI.

This allows treasure to appear in a treasure room, corridor alcove, shrine, guarded room, or other future tile without changing NPC treasure semantics.

## Manual Test Scenario

1. Author a treasure socket/location on a test tile.
2. Place/configure a treasure prop with a known value.
3. Generate or load a dungeon containing that content.
4. Verify the cell exposes the treasure as an unresolved POI.
5. Resolve the treasure through its public interaction path.
6. Verify its visible test state changes and it is no longer offered as unresolved.
7. Verify unrelated tile traversal/topology remains unchanged.

## Out of Scope

- NPC discovery/investigation behavior
- Carried treasure
- Reward settlement
- Procedural treasure placement
- Treasure rarity/itemization
- Trapped/guarded treasure

## Git

Suggested branch:

`feature/t004-treasure-prop`

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
