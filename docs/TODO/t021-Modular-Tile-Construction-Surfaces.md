# t021 — Modular Tile Construction Surfaces

## Tracking
- **ID:** t021
- **Status:** Awaiting Unity Validation
- **Milestone:** Strategic Construction
- **Depends on:** t019 findings

## Goal
Evolve dungeon tile prefabs enough to support physically believable external trap mechanisms and future construction variation without turning dungeon building into arbitrary voxel editing.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Design/World_Generation_and_Building.md`
- `docs/Reference/Prefab_Conventions.md`

Begin with these documents and their directly related code/assets. Broaden investigation only when required by a demonstrated dependency.

## Direction
Separate the conceptual gameplay/traversal volume from configurable construction surfaces such as floor, ceiling, cardinal walls, openings, and trap/service regions.

## Requirements
- Audit current tile prefab assumptions exposed by t019.
- Define the minimum modular prefab contract needed for replaceable/configurable wall/floor/ceiling surfaces and trap attachment regions.
- Preserve authored modularity: use controlled modules/sockets/surfaces rather than arbitrary geometry editing.
- Keep traversal/topology data authoritative and independent from purely visual module swaps where possible.
- Provide migration guidance for existing tile prefabs.

## Acceptance Criteria
- At least one representative tile can expose configurable floor/ceiling/wall construction surfaces required by external trap placement.
- Trap attachment does not require destructive ad-hoc modification of a monolithic tile mesh.
- Existing topology/traversal behavior remains correct.
- The prefab contract is documented for future tile authoring.

## Out of Scope
- Full art pass
- Voxel construction
- Procedural mesh editing
- Every possible wall/floor upgrade

## Git
Suggested branch: `feature/t021-modular-tile-surfaces`

## Implementation Status

- Added the optional `TileConstructionSurfaces` prefab-root contract with stable
  surface IDs, semantic floor/ceiling/cardinal-wall/service kinds, local anchors,
  trap masks, and controlled authored variants.
- Visual-only variants may be selected through the contract. Variants marked
  `RequiresTopologyResolution` are rejected by the direct swap API so tile
  profiles and edge intent remain authoritative.
- Migrated `Narrow_Straight_I` as the representative tile. It exposes all
  required construction anchors without altering its existing visual mesh,
  colliders, prop sockets, or baked topology profiles.
- Added editor tests for the representative contract and all four trap
  attachment surfaces.
- Documented prefab authoring and incremental migration. Existing monolithic
  visuals may remain during migration; only replaceable children need become
  controlled module roots.

## Validation Notes

- `Assembly-CSharp` compiled with 0 warnings and 0 errors.
- `Assembly-CSharp-Editor` compiled with 0 errors and one pre-existing unused
  `TileSocketBakerWindow.visualizeSamples` warning.
- Manual Unity validation remains pending.

## Manual Unity Validation

1. Open `Narrow_Straight_I` in Prefab Mode and confirm its root exposes unique
   Floor, Ceiling, North/South/East/West Wall, and Trap Service Region slots.
2. Confirm each slot anchor is a child of the prefab and the four applicable
   slots expose Floor/Ceiling/LeftWall/RightWall trap compatibility.
3. Place rotated `Narrow_Straight_I` tiles and confirm their existing openings,
   ladders, entrance socket, colliders, and NPC traversal are unchanged.
4. Run the editor tests, including `TileConstructionSurfacesTests`.
5. Author two harmless visual variants on a copied representative slot and
   confirm selection activates exactly the requested authored module.
6. Mark that copied slot `RequiresTopologyResolution` and confirm direct variant
   selection is rejected.
