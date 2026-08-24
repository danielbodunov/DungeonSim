# Add a Dungeon Tile

## Goal

Add a new visual tile candidate without changing the logical cell model.

## Likely files/assets involved

- New tile prefab under `Assets/Resources/Dungeon/`.
- One or more rotation-specific `TileSocketProfile` assets under `Assets/Resources/TileProfiles/`.
- `Assets/TileAdjacencyDatabase.asset` if the baking/registration workflow requires updating it.
- Tile socket/profile editor tools under `Assets/Scripts/Editor/` for authoring and baking.

Runtime scripts usually should **not** need modification for a new tile that fits existing categories and socket semantics.

## Authoring sequence

1. Build the prefab to the established cell scale and pivot/orientation conventions.
2. Author/bake edge sockets for the prefab.
3. Author any prop sockets the tile is intended to expose.
4. If the tile supports modular construction, add the
   `TileConstructionSurfaces` root component and authored surface anchors.
5. Bake/register rotated `TileSocketProfile` variants.
6. Confirm each profile has the intended `TileCategory` (`Narrow`, `Wide`, `Transition`, `Starter`, etc.).
7. Verify north/south/east/west hashes and match data are generated as expected.
8. Add the profile to the adjacency/solver data through the existing editor workflow.
9. Test placement in several neighborhoods rather than only in isolation.

## Rotation-safe texture authoring

For gravity-oriented pixel art, assign a material using
`DungeonSim/Rotation Safe Tile Atlas` and author the 2x2 surface-role atlas
defined in [Prefab and Asset Conventions](../Reference/Prefab_Conventions.md#rotation-safe-surface-textures).

In Blender, keep face normals correct and paint structural/contact AO into the
red vertex-color channel (`1` clear, `0` occluded). Do not rotate atlas regions
to compensate for a particular profile. Export FBX with vertex colors and
stable real-world tile scale. In Unity, enable vertex-color import and configure
the atlas as Point, no mipmaps, uncompressed, and Clamp. Tune world tiling,
metallic, and smoothness on the material; edit red vertex AO on the mesh.

Preview the same source prefab through R0, R1, R2, and R3. Confirm that back-wall
details stay upright, up/down-facing geometry selects floor/ceiling art, side
walls select side art, and vertex AO remains attached to the geometry.

Construction module changes that affect an opening, collider, or traversal are
not visual variants. Mark them `RequiresTopologyResolution` and represent the
result through an appropriate tile profile.

## Important model rule

A tile prefab is a **resolved representation of a logical cell or compatible footprint**, not the source of player topology intent. Width intent and connection intent live separately from the selected prefab/profile.

Do not encode a unique gameplay rule only in the mesh shape if the solver, NPC navigation, or save system also needs to understand it.

## Prop sockets

Use authored prop sockets when content needs a stable pose or topology-aware location, such as an entrance or traversal connector. Ordinary floor props do not necessarily need a topology-sensitive socket.

## Verification matrix

Test the new tile in at least these conditions when applicable:

- end/cap;
- straight corridor;
- corner;
- T-junction;
- four-way junction;
- adjacent narrow/wide or transition cases;
- explicit open edge;
- explicit closed edge;
- local re-resolution after adding/removing a neighbor.

Also verify:

- NPC routes use only genuinely open passages;
- lighting does not pass through a visually/logically closed wall;
- any baked prop sockets rotate correctly;
- placed persistent content survives a local profile re-resolution when it remains compatible.

## When runtime code is required

Stop and treat the task as architecture work if the new tile requires:

- a new cell/footprint ownership model;
- new connection semantics beyond current edge intent;
- multi-cell occupancy that the grid must understand;
- a new socket type with gameplay meaning;
- special-case solver rules that cannot be represented in profile data.

Read [`../Architecture/Dungeon_Generation.md`](../Architecture/Dungeon_Generation.md) before making those changes.
