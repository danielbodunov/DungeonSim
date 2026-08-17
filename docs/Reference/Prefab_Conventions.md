# Prefab and Asset Conventions

This page records conventions implied by the current project structure and architecture. Keep it updated as the asset pipeline becomes more formalized.

## Dungeon tiles

Location: `Assets/Resources/Dungeon/`

Dungeon tile prefabs are visual/resolution candidates. Their gameplay connectivity is represented through baked `TileSocketProfile` data rather than inferred only from mesh appearance.

Recommended conventions:

- keep a consistent cell scale and origin convention;
- use names that identify family/function rather than rotation-specific implementation details where the profile system already represents rotation;
- author openings/walls so socket baking reflects the intended logical passage;
- keep decorative wall/back-wall variants separable from topology where possible;
- avoid embedding a one-off gameplay mechanic directly into a tile prefab when it can be a placed prop/trap instead.

## Tile profiles

Location: `Assets/Resources/TileProfiles/`

Profiles may be rotation-specific and should be generated/maintained through the tile socket baking workflow rather than manually edited without a clear reason.

A profile should correctly identify:

- source prefab;
- tile category;
- rotation;
- all four edge hashes/matches;
- baked prop sockets.

## Traps

Location: `Assets/Resources/Traps/`

A normal one-cell trap prefab should:

- contain one `CellTrap` subclass;
- treat the grid cell as its logical occupancy;
- keep trap-local animation/effect components inside the prefab;
- expose deterministic orientation through placement rather than depending on arbitrary scene rotation.

The current design favors traps that reserve whole cells. Mechanism geometry can live in the trap cell and face a neighboring traversed/target cell.

## Traversal

Location: `Assets/Resources/Traversal/`

Traversal prefabs such as ladder pieces are gameplay-significant. Socket role/lane/orientation must remain compatible with route generation; do not treat them as interchangeable decoration.

## Props

Location: `Assets/Resources/Props/`

Use `FloorProp` for ordinary player-placed cell content. Use `PropCatalog` + `PropGenerator` for reconstructed procedural content. If a prop changes topology or connects traversal, it should use a stronger authored socket/structure model.

## Decorative back walls

Back-wall meshes should generally remain visual/decorative and replaceable without changing the logical dungeon connection model. If a wall variant truly opens/closes traversal, that state belongs in tile/profile topology, not only in decoration.

## Multi-cell content

Do not represent one logical multi-cell mechanism as unrelated independent prefabs merely to fit the current one-cell APIs. Multi-cell crushers, large encounter structures, and similar content should eventually have explicit footprint/ownership data so placement, save/load, replacement, and removal remain atomic.

## FBX organization

Prefab modularity matters more than requiring every resolved tile to originate from its own FBX. Assets may share source FBXs when that improves authoring, as long as Unity prefab boundaries, pivots, naming, and profile baking remain deterministic. Prefer source organization that minimizes duplicated geometry/materials without coupling unrelated gameplay pieces.

## Related docs

- [`../HowTo/Add_A_Dungeon_Tile.md`](../HowTo/Add_A_Dungeon_Tile.md)
- [`../HowTo/Add_A_Trap.md`](../HowTo/Add_A_Trap.md)
- [`Data_Assets.md`](Data_Assets.md)
- [`../Architecture/Dungeon_Generation.md`](../Architecture/Dungeon_Generation.md)
