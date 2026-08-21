# t019 — External Trap Attachment Model

## Tracking
- **ID:** t019
- **Status:** Awaiting Unity Validation
- **Milestone:** Strategic Construction
- **Depends on:** t011; preferably t018

## Goal
Change traps from arbitrary occupants of traversable dungeon cells into mechanisms that require compatible construction/service space outside the playable corridor volume.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Design/World_Generation_and_Building.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Design Principle
A trap should feel physically installed. Spikes entering a corridor from a floor, wall, or ceiling should have a corresponding mechanism/attachment region outside that traversable space.

## Requirements
- Define generic trap attachment surfaces/regions for floor, wall, and ceiling orientations.
- A trap placement occupies/reserves external/service space rather than simply claiming the traversable cell as its mechanism location.
- Preserve a clear relationship between mechanism, affected corridor/tile, trigger area, and hazard direction.
- Placement validation must reject incompatible or unavailable attachment/service space.
- Do not make the system spike-trap-specific.
- Record implications for tile prefab construction/modularity discovered during implementation.

## Acceptance Criteria
- A spike-style trap can be authored/placed through a compatible floor/wall/ceiling attachment model.
- The mechanism exists outside the traversable dungeon volume while its hazard affects that volume.
- Placement fails when required service space is unavailable.
- Existing traversal remains based on the dungeon cell, not the external mechanism position.

## Out of Scope
- Full modular tile rebuild
- All future trap types
- Trap rotation UX polish (t020)

## Git
Suggested branch: `feature/t019-external-trap-attachments`

## Implementation Status

- Added generic Floor, Ceiling, LeftWall, and RightWall attachment surfaces.
- Trap prefabs declare allowed/preferred surfaces through
  `TrapAttachmentDefinition`; tile profiles can restrict compatible surfaces.
- A resolved placement reserves an adjacent unbuilt service cell for the
  mechanism while retaining the built corridor cell as the trigger/hazard target.
- Validation rejects out-of-bounds, built, fixed-ground, generated-prop, and
  already-reserved service cells. Building into a placed trap's service cell is
  also rejected.
- `CellTrap` exposes target cell, service cell, surface, and hazard direction.
  NPC traversal and entry triggering remain keyed to the target dungeon cell.
- Save format version 14 and DungeonTestScenario records persist the resolved
  attachment surface; older trap records resolve a compatible surface on load.
- The SpikeWall prefab supports every surface and prefers Floor, providing the
  initial spike-style validation content without adding t020 rotation UX.
- Ladder routes now retain intermediate ladder cells as crossing waypoints, so
  traps targeting a middle ladder cell receive the normal cell-entry event even
  when that cell is not a usable horizontal ladder exit.
- Documented the future need for sub-cell tile construction volumes/sockets;
  reserving a whole adjacent service cell is intentionally the t019 foundation.

## Validation Notes

- `Assembly-CSharp` compiled with 0 warnings and 0 errors.
- Manual Unity validation remains pending.

## Manual Unity Validation

1. Place SpikeWall on a built corridor cell with at least one adjacent unbuilt
   service cell. Confirm the mechanism appears in that external cell and points
   toward the selected corridor.
2. Build around a target until every allowed adjacent service cell is occupied,
   then confirm trap placement is rejected without mutation.
3. Place two traps whose only available attachment would reserve the same
   service cell and confirm the second placement is rejected.
4. Attempt to build into an existing trap's service cell and confirm construction
   is rejected.
5. Run an adventurer through the target corridor cell and confirm the trap
   triggers there rather than at the service cell.
   Repeat with a trap targeting a middle ladder cell that has no horizontal
   ladder exit and confirm it triggers while the adventurer climbs through.
6. Save/load and capture/reset a DungeonTestScenario; confirm the mechanism
   returns on the same surface and continues targeting the same corridor cell.
