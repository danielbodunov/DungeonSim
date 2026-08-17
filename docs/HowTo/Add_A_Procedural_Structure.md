# Add a Procedural Structure

## Goal

Add content generated from the resolved dungeon rather than placed as authoritative player state.

## Preferred path

1. Decide whether the structure is `Single` or `Chained` generation.
2. Author the required tile prop sockets and bake them into `TileSocketProfile` assets.
3. Create the structure prefabs/bundles.
4. Add a `PropDefinition` to `PropCatalog.asset` with a stable `structureId`.
5. Configure occupancy, socket rotation, lane variants, bundle IDs, and role fallbacks.
6. Only modify `PropGenerator` if the new structure requires generation semantics not representable by existing catalog data.

## Data model

`PropDefinition` currently supports:

- generation mode;
- spawn chance;
- cell occupancy;
- socket rotation/rotation offset;
- lane-specific variants;
- role-specific bundles;
- legacy prefab-per-role fallback.

Prefer expressing art variation in `PropCatalog` data instead of branching on prefab names in `PropGenerator`.

## Traversal warning

If the generated structure creates navigation — for example a ladder — it is not merely decorative. `NPCTraversal` consumes generated traversal structures when building the route graph.

For traversal structures, verify:

- both endpoints/standing locations;
- route graph rebuild after generation;
- behavior after any participating tile re-resolves;
- stability during an active adventurer run.

The design direction is moving strategically meaningful traversal toward explicit player placement, so check [`../Design/World_Generation_and_Building.md`](../Design/World_Generation_and_Building.md) before expanding automatic traversal generation.

## Persistence

Generated structures should normally be reconstructed from authoritative layout plus generation seed/configuration. `PropGenerator.ClearGeneratedProps()` exists specifically so generated state can be discarded before authoritative layout restoration.

Do not promote a generated object into save data unless it contains player-owned state that cannot be reproduced.

## Verification

1. Generate with deterministic seed/configuration.
2. Confirm correct socket role/lane/bundle selection.
3. Confirm occupied cells are reported correctly.
4. Make an unrelated local dungeon edit and verify unaffected structures remain logically valid.
5. Modify a participating host tile and verify the structure rebuilds/removes correctly.
6. If traversal-capable, run NPC pathing through it.
7. Save/load and confirm regeneration reproduces valid state.

## Read next

- [`../Architecture/Props_and_Traps.md`](../Architecture/Props_and_Traps.md)
- [`../Architecture/NPC_Runtime.md`](../Architecture/NPC_Runtime.md)
- [`../Architecture/Save_System.md`](../Architecture/Save_System.md)
