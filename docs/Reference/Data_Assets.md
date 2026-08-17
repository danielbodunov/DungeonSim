# Data Asset Reference

## `ObjectsDatabaseSO`

Purpose: build-palette definitions for player-placeable content.

Each `ObjectData` currently stores:

- display/name data;
- stable integer ID;
- logical size;
- prefab;
- `ObjectPlacementType` (`DungeonTile`, `Trap`, `Entrance`, `FloorProp`).

Use this when adding content that belongs to the existing player placement taxonomy.

## `TileSocketProfile`

Purpose: one rotated tile candidate used by tile resolution.

Stores:

- source prefab;
- bake resolution;
- rotation;
- base tile name;
- `TileCategory`;
- cardinal edge hashes;
- cardinal compatibility match lists;
- baked prop sockets.

Profiles are authored compatibility data, not just metadata for the mesh.

## `TileAdjacencyDatabase`

Purpose: central baked/registered tile compatibility data consumed by the grid solver.

Treat changes to its schema or matching semantics as architecture-level changes because dungeon resolution depends on them.

## `PropCatalog`

Purpose: procedural prop/structure definitions.

A `PropDefinition` currently includes:

- stable `structureId`;
- `Single` or `Chained` generation mode;
- spawn chance;
- whether the structure occupies a cell;
- socket rotation and rotation offset;
- lane variants;
- role/bundle definitions;
- legacy role-prefab fallback.

Use catalog data for art/configuration variation before adding code branches to `PropGenerator`.

## Save data

`DungeonSaveData` contains the serializable representation coordinated by `GameSaveManager`. When adding persistent state, prefer stable logical IDs and cells/socket identities over world-space references or instantiated object references.

## Important project assets

Current notable locations include:

- `Assets/TileAdjacencyDatabase.asset`
- `Assets/Resources/PropCatalog.asset`
- `Assets/Resources/TileProfiles/`
- `Assets/Resources/Dungeon/`
- `Assets/Resources/Traps/`
- `Assets/Resources/Traversal/`
- `Assets/Resources/Props/`

See [`Prefab_Conventions.md`](Prefab_Conventions.md) for asset-organization guidance.
