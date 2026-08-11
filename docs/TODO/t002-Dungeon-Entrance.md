# t002 — Dungeon Entrance / Adventurer Spawn Contract

## Tracking

- **ID:** t002
- **Status:** Ready
- **Milestone:** Expedition Loop
- **Depends on:** t001 — NPC Traversal Memory
- **Blocks:** t003 — Point-of-Interest Foundation

## Type

Feature

## Summary

Establish one authoritative semantic dungeon entrance that defines where adventurers enter, spawn, return to, and exit the dungeon.

Use a specialized starter/entrance tile as the initial authored implementation, but do not make a specific tile type the permanent gameplay contract. The entrance should be represented by a dedicated component/socket or equivalent semantic marker hosted by a valid tile.

## Current Behavior

NPC traversal can remember an entrance/start cell and return through familiar connections, but the project does not yet have a clearly authored, reusable entrance/spawn contract suitable for the core expedition loop.

## Desired Behavior

A dungeon can expose an entrance through a semantic authored object. NPC systems can query that entrance to determine the logical entrance cell and appropriate spawn/entry/exit transform without special-casing one concrete tile prefab.

The first content implementation may be a specialized starter tile containing the entrance marker.

## Requirements

- Define a dedicated dungeon entrance/spawn contract using the smallest architecture appropriate to the existing project.
- Associate the entrance with its containing dungeon cell.
- Provide an authored NPC spawn/entry transform.
- Provide the information required for an NPC returning home to resolve at the same logical entrance.
- Support a specialized starter tile as the initial host without requiring all future entrances to use that tile type.
- Integrate with existing NPC start/return concepts rather than creating a parallel navigation system.
- Expose enough debug information to verify which entrance/cell is active if this is not already obvious through existing tooling.

## Acceptance Criteria

The ticket is complete when:

- A dungeon can contain an authoritative entrance marker/component/socket.
- An NPC visit can begin from the entrance's authored spawn/entry position.
- The NPC's entrance/start cell is derived from the entrance contract rather than a hard-coded tile identity.
- Familiar return behavior can target the same logical entrance.
- The initial specialized starter tile can host the entrance implementation.
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

## Manual Test Scenario

1. Generate or construct a dungeon containing the starter entrance tile.
2. Spawn/start an adventurer visit.
3. Verify the adventurer begins at the authored entrance location and records the correct entrance cell.
4. Allow the adventurer to traverse several cells.
5. Trigger normal return behavior.
6. Verify the adventurer follows familiar routing back to the same logical entrance and resolves the exit there.

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
