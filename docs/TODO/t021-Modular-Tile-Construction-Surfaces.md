# t021 — Modular Tile Construction Surfaces

## Tracking
- **ID:** t021
- **Status:** Planned
- **Milestone:** Strategic Construction
- **Depends on:** t019 findings

## Goal
Evolve dungeon tile prefabs enough to support physically believable external trap mechanisms and future construction variation without turning dungeon building into arbitrary voxel editing.

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
