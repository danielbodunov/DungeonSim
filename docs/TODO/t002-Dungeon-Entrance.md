# t002 — Dungeon Entrance / Adventurer Spawn Contract

## Tracking

- **ID:** t002
- **Status:** Complete
- **Milestone:** Expedition Loop
- **Depends on:** t001 — NPC Traversal Memory
- **Blocks:** t003 — Point-of-Interest Foundation

## Type

Feature

## Summary

Establish one authoritative semantic dungeon entrance that defines where adventurers enter, spawn, return to, and exit the dungeon.

Use a placeable entrance object hosted by a compatible tile socket. `Narrow_Straight_I` provides the first validation socket, but neither NPC behavior nor the entrance contract should depend on that concrete tile type.

## Current Behavior

NPC traversal can remember an entrance/start cell and return through familiar connections, but the project does not yet have a clearly authored, reusable entrance/spawn contract suitable for the core expedition loop.

## Desired Behavior

A dungeon can expose an entrance through a semantic, player-placed object. NPC systems can query that entrance to determine the logical entrance cell and appropriate spawn/entry/exit transform without special-casing one concrete tile prefab.

The entrance can be placed from the build palette on a compatible entrance socket. The first validation socket is authored on the walkable orientation of `Narrow_Straight_I` because `Starter_X` is not a walkable expedition threshold.

## Requirements

- Define a dedicated dungeon entrance/spawn contract using the smallest architecture appropriate to the existing project.
- Associate the entrance with its containing dungeon cell.
- Provide an authored NPC spawn/entry transform.
- Provide the information required for an NPC returning home to resolve at the same logical entrance.
- Support a valid walkable tile as the initial host without requiring all future entrances to use that tile type.
- Allow the entrance object to be placed and removed through the building interface.
- Preserve the placed entrance in dungeon saves.
- Integrate with existing NPC start/return concepts rather than creating a parallel navigation system.
- Expose enough debug information to verify which entrance/cell is active if this is not already obvious through existing tooling.

## Acceptance Criteria

The ticket is complete when:

- A dungeon can contain an authoritative entrance marker/component/socket.
- An NPC visit can begin from the entrance's authored spawn/entry position.
- The NPC's entrance/start cell is derived from the entrance contract rather than a hard-coded tile identity.
- Familiar return behavior can target the same logical entrance.
- The initial `Narrow_Straight_I` validation tile can host the entrance implementation.
- The build palette can place one entrance on a compatible socket and reject incompatible cells or a second entrance.
- The placed entrance can be removed and survives a save/load round trip.
- Future tile prefabs could host an entrance without changing NPC traversal architecture.
- Existing unrelated generation and NPC traversal behavior remains unchanged.

## Relevant Systems / Files

Investigate before implementation. Likely relevant areas include:

- NPC traversal/start-cell handling
- Dungeon cell/profile ownership
- Existing prop/socket/portal conventions
- Runtime dungeon generation/build placement
- Debug visualization

These are starting points, not a prescribed implementation boundary.

## Constraints

- Do not create a second navigation graph for entrances.
- Do not couple NPC behavior permanently to one starter tile prefab.
- Do not build treasure/POI behavior in this ticket.
- Do not redesign general tile sockets unless the existing architecture genuinely requires a small extension.
- Preserve existing serialization and prefab references where practical.

## Implementation Notes

Preferred conceptual separation:

- **Tile:** geometry/topology/content host.
- **Entrance:** gameplay function identifying how an expedition enters/exits.

A specialized entrance tile is useful authored content, but the semantic entrance component/socket should be the system contract.

### Implementation Status

- Added a reusable `DungeonEntrance` component whose transform defines the authored entry/exit pose.
- Added runtime resolution from a placed tile instance to its containing grid cell.
- Updated NPC spawning and familiar return behavior to use the resolved entrance while preserving the previous spawn selection as a compatibility fallback for content without a marker.
- Added an entrance gizmo to the existing NPC traversal debug view.
- Added an `Entrance/Single` socket to the walkable `Narrow_Straight_I` orientation.
- Added a placeable entrance prefab, build-palette definition, removal tool, and save data.
- Source validation and the manual Unity scenario below were completed successfully on 2026-08-11.

## Manual Test Scenario

1. Generate or construct a dungeon containing a horizontal `Narrow_Straight_I` tile.
2. Select **Dungeon Entrance** from the build palette and place it on that tile.
3. Verify an incompatible cell and a second entrance placement are rejected.
4. Spawn/start an adventurer visit.
5. Verify the adventurer begins at the authored entrance location and records the correct entrance cell.
6. Allow the adventurer to traverse several cells, then trigger normal return behavior.
7. Verify the adventurer follows familiar routing back to the same logical entrance and resolves the exit there.
8. Save and load the dungeon, then verify the entrance is restored on the same socket.
9. Use **Remove Entrance** and verify the entrance is cleared.

## Out of Scope

- Treasure
- General POI/investigation framework
- Multiple simultaneous dungeon entrances
- Entrance selection UI
- Portal/teleport entrances
- Visual polish beyond what is necessary to author/test the entrance

## Git

Suggested branch:

`feature/t002-dungeon-entrance`

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
