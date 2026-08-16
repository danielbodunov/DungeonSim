# DEV008 — Single Authoritative Entrance Placement

## Tracking
- **ID:** DEV008
- **Status:** Complete
- **Milestone:** Developer Tooling / Building Rules
- **Depends on:** DEV007

## Goal
Enforce a single authoritative entrance invariant. Placing a new manual entrance should replace the existing entrance, while removing that manual entrance restores the non-removable gameplay default.

## Requirements
- Allow at most one active dungeon entrance.
- Validate the proposed replacement before destroying/removing the existing entrance where practical.
- On successful placement of a new entrance, remove the previous entrance and make the new entrance authoritative.
- Preserve the ability to manually remove a placed entrance and immediately fall back to the gameplay default.
- Do not allow the gameplay default entrance itself to be removed.
- Apply the invariant consistently to normal placement, save/load, and test scenario capture/apply.
- NPC spawn/return systems must always resolve the current authoritative entrance.

## Acceptance Criteria
- Placing entrance B while entrance A exists leaves only B.
- An invalid attempted replacement leaves A intact.
- Manual removal restores exactly one gameplay default entrance.
- A removal attempt against the gameplay default is rejected and leaves it intact.
- Save/load and scenario load never create duplicate active entrances.
- NPC spawn/return behavior follows the replacement entrance.

## Out of Scope
- Multiple entrances
- Entrance selection UI
- Entrance upgrade/progression mechanics

## Manual Validation
Place, replace, reject an invalid replacement, manually remove, save/reload, and scenario capture/load while verifying the single-authority invariant.

### Focused Unity Steps

1. Enter Play Mode with the normal/default entrance active. Place manual entrance A, then place valid entrance B on another compatible cell. Verify A is removed, B is the only enabled `DungeonEntrance`, and NPC spawn/return resolves B.
2. With B active, attempt placement on an unbuilt cell, a cell without an entrance socket, and a cell occupied by a trap or floor prop. Verify each attempt is rejected and B remains active and unchanged.
3. Enter entrance-removal mode and remove B. Verify exactly one gameplay default entrance immediately becomes authoritative and NPC spawn/return resolves it.
4. Attempt to remove the gameplay default entrance. Verify the removal is rejected and the default remains unchanged.
5. Save with the default entrance, place a manual entrance, then reload. Verify exactly one default entrance is restored. Repeat a save/load with a manual entrance and verify exactly that manual entrance is restored.
6. Capture default and manual scenarios. Mutate and load/reset each scenario, verifying the captured authority and single-entrance invariant are restored.
7. In the manual scenario, use a layout containing a tile-authored default marker. Verify the manual entrance is authoritative and the layout marker is disabled rather than creating a duplicate.

## Implementation Status

- `TileGridGenerator` now owns the entrance authority state for placed manual entrances, gameplay fallbacks, and layout-authored default markers.
- Entrance placement reuses production validation, resolves the target socket pose, and instantiates the candidate before retiring the current placed entrance. Invalid replacements therefore leave the existing authority intact.
- A successful manual placement suppresses all tile-authored entrance markers. Retired placed components are disabled immediately before deferred Unity destruction, preventing a same-frame duplicate active entrance.
- The removal workflow accepts only a placed manual entrance. Removing it re-enables a layout-authored marker or lets `NPCTraversal` recreate the normal fallback; removal attempts against either default form are rejected.
- Automatic fallback handling during a layout refresh preserves the default-entrance contract and never converts a refresh into player-requested removal.
- Save compatibility is unchanged: a manual record restores that manual entrance, while a null record restores the gameplay default.
- Scenario capture/apply allows a manual entrance to replace a layout-authored default while preserving DEV007's complete preflight validation boundary. No-entrance scenarios remain limited to empty layouts.

## Validation Notes

- Runtime assembly compiles with zero warnings and zero errors.
- Editor assembly compiles with zero errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` unused-field warning.
- Static call-site review covers normal placement/removal, save/load and rollback, scenario capture/preflight/apply, tile-layout refresh, and NPC default-entrance recreation.
- Manual Unity validation completed successfully on 2026-08-16.

## Known Limitations

- A default scenario containing multiple tile-authored entrance markers remains invalid during scenario preflight; DEV008 does not invent selection semantics for malformed multi-entrance layouts.
- Manual entrance compatibility remains topology-sensitive by design. DEV001 generalized floor props only; a tile still needs an authored `Entrance/Single` socket. In the current tile-profile data, only `Narrow_Straight_I_Rot0` exposes that socket.
- Save loading retains its existing broader mutation/rollback behavior. DEV008 guarantees entrance replacement itself validates before retiring the prior authority; it does not redesign save loading into a fully non-mutating preflight pipeline.

## Git
Suggested branch: `tool/dev008-single-entrance`

Active branch: `tool/dev008-single-entrance`

Proceed according to `docs/AGENTS.md`.
