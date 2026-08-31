# t031 — Placeable Ladders

## Tracking
- **ID:** t031
- **Status:** Planned
- **Milestone:** Vertical Construction / Ladders
- **Depends on:** t029; t030 recommended as the elevated-surface validation target

## Goal
Add player-placeable ladders that create explicit traversable connections between valid lower and upper walkable surfaces, allowing NPC/adventurer navigation across constructed elevations.

## Requirements
- Add at least one representative placeable ladder definition/prefab through the shared construction pipeline.
- Represent the ladder as an explicit vertical traversal connection between two compatible walkable endpoints from t029.
- Validate that both lower and upper endpoints exist and are geometrically/semantically compatible before placement succeeds.
- Require a valid attachment/placement context; ladders must not float freely without the structure needed to support/connect them.
- Validate the ladder's occupied/service volume against conflicting obstacles and construction.
- Provide normal valid/invalid placement preview feedback.
- Add the navigation/pathfinding connection needed for an agent to choose a route through the ladder.
- Add functional traversal behavior that:
  - enters the ladder from a valid endpoint;
  - moves the agent between elevations;
  - exits onto the destination walkable surface;
  - works in both directions when the ladder definition permits bidirectional traversal.
- Keep traversal state explicit so ordinary planar locomotion does not attempt to drive the character while climbing.
- Persist placed ladders and their endpoint/link data through save/load and scenario capture/reset.
- Remove the navigation link cleanly when a ladder is removed or becomes invalid.

## Animation Boundary
Functional traversal is required, but polished final climbing animation is not.

If the final shared character animation library is unavailable, use the narrowest acceptable placeholder movement/animation needed to validate traversal without coupling this ticket to the full character-animation roadmap.

## Acceptance Criteria
- The player can place a ladder only when it resolves valid lower and upper traversal endpoints and passes occupancy/attachment rules.
- Invalid ladder placement does not mutate authoritative construction state.
- A placed ladder creates an explicit navigation connection between its endpoints.
- An NPC/adventurer can path to, enter, traverse, and exit the ladder to reach the opposite elevation.
- Bidirectional ladders can be traversed in both directions.
- Ordinary locomotion does not fight ladder traversal while the agent is in the climbing state.
- Removing the ladder removes its traversal link and leaves no stale navigation connection.
- Save/load restores the ladder, endpoints, and traversal availability.
- Scenario capture/reset reproduces ladder placement and traversal state.
- Existing planar navigation continues to work normally.

## Out of Scope
- Final/polished ladder-climbing animation set
- Procedural ladder mesh generation
- Rope ladders with physics simulation
- Stairs
- Elevators
- Jumping between elevations
- General free-climbing systems
- Automatic generation of ladders by dungeon generation

## Manual Validation
1. Place a platform/elevated surface and preview a ladder between valid endpoints.
2. Attempt invalid placements with missing upper/lower surfaces, blocked volume, and unsupported attachment contexts.
3. Command/observe an NPC route from the lower surface to the upper surface through the ladder.
4. Validate traversal in the reverse direction.
5. Remove the ladder and verify the route is no longer available.
6. Save/load and scenario-reset the arrangement and repeat traversal.

## Post-Implementation Report
Record:
- ladder definition/prefab
- endpoint and attachment rules
- occupied/service-volume rules
- navigation-link representation
- traversal-state implementation
- animation/placeholder behavior used
- placement/removal integration
- save/scenario representation
- limitations deferred to later traversal/animation work

## Git
Suggested implementation branch: `feature/t031-placeable-ladders`

Proceed according to `docs/AGENTS.md`.
