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

### Construction surfaces

Add `TileConstructionSurfaces` to the prefab root when a tile exposes modular
construction. Each slot must have:

- a stable, prefab-unique ID;
- a semantic kind: floor, ceiling, cardinal wall, or trap-service region;
- an anchor inside the prefab hierarchy;
- an explicit topology impact;
- zero or more controlled, authored module variants;
- a trap-attachment mask when the slot supports a trap mechanism.

Variants are prefab-owned GameObject roots, not arbitrary meshes supplied at
runtime. `VisualOnly` variants may be selected directly. Mark walls/openings or
any collider change that affects passage as `RequiresTopologyResolution`; the
direct selection API rejects those swaps because `TileSocketProfile` and edge
intent remain authoritative for traversal.

`Narrow_Straight_I` is the representative migrated prefab. Its markers coexist
with the legacy monolithic visual hierarchy, so migration does not require
destructive mesh editing.

Migration sequence:

1. Add the component to the tile root without moving existing sockets/colliders.
2. Create prefab-local anchors for floor, ceiling, cardinal walls, and any
   trap-service region the tile supports.
3. Assign stable IDs and trap masks. Do not infer identity from child order.
4. Move only genuinely replaceable visual children under controlled module
   roots; retaining a legacy monolithic visual is valid during migration.
5. Mark topology-affecting modules as requiring profile resolution.
6. Re-bake profiles only if logical openings, sockets, or traversal changed.
7. Validate all rotated profiles and existing NPC routes.

## Tile profiles

Location: `Assets/Resources/TileProfiles/`

Profiles may be rotation-specific and should be generated/maintained through the tile socket baking workflow rather than manually edited without a clear reason.

A profile should correctly identify:

- source prefab;
- tile category;
- rotation;
- all four edge hashes/matches;
- baked prop sockets.

### Rotation-safe surface textures

`TileSocketProfile.rotation` remains authoritative for geometry and topology.
Runtime instances use clockwise Z rotations (`R0 = 0`, `R1 = -90`,
`R2 = -180`, `R3 = -270` degrees). Do not duplicate a prefab merely to keep
directional texture art upright.

Tiles using `RotationSafeTileAtlas` sample the sliced 32x32 sprites in
`Assets/Materials/DungeonAtlas.png`. Sprite names and objects are authoring
metadata only. `DungeonSurfaceFamily` assets explicitly assign Sprite variants
to back-wall, floor, ceiling, and side-wall roles; artists never enter atlas
coordinates manually.

The shader classifies the role from the transformed world normal. This makes a
face that changes role after profile rotation sample the correct quadrant, while
world Y remains up for side and back walls. Base color, directional masonry,
grime, chains, and trim belong in the atlas and remain world/gravity oriented.
Geometry-specific wear, contact shadowing, and structural AO rotate with the
mesh and must not be baked into directional base art.

Reserve vertex-color **red** for structural AO: `1` is unoccluded and `0` is
fully occluded. The Shader Graph feeds that channel into the Lit Ambient
Occlusion block independently of the atlas. Green, blue, and alpha are currently
unassigned. Unity's FBX importer must retain vertex colors.

`Tools > Dungeon > Rebuild Surface Family Lookup` validates the family assets
and derives normalized rectangles from `Sprite.rect`. The generated RGBAFloat
lookup is 257 texels wide. Each family has eight rows: a rectangle row and a
weighted-choice row for each of the four roles. Rectangle-row column zero stores
variant count/total weight and columns 1-16 store `(x, y, width, height)`.
The following row stores 256 pre-baked weighted-choice indices.
Missing roles, foreign textures, non-32x32 sprites, and lists over 16 variants
produce explicit generation errors.

`DungeonSurfaceAppearance` maps the Primary, Secondary, Accent, and Special
semantic slots to family lookup indices through a `MaterialPropertyBlock`, plus
an explicit visual seed. Stable hashing of seed, projected surface cell, family,
and current role selects weighted variants without frame dependence. Each role's
variants use the same relative, non-negative weight rules as ground bands.
UV2.x encodes the semantic slot as 0-3; absent UV2 data resolves to Primary.
World normal—not UV2—continues to determine the current surface role.

Mesh channel contract:

- world position: rotation-safe projected coordinates;
- world normal: dynamic surface role;
- UV0: manual/non-world-projected fallback;
- UV1: reserved for lightmaps;
- UV2.x: Primary=0, Secondary=1, Accent=2, Special=3;
- UV2.yzw: reserved;
- vertex color R: structural AO; G/B/A: reserved.

The shader maps sprite-local coordinates into the selected rect and offsets the
range to the first/last texel centers using the actual atlas texel size. The
atlas must use Point filtering, disabled mipmaps, no compression, and Clamp.
`Narrow_Corner_L` is the representative family-aware prefab across R0-R3.

`Assets/Materials/DungeonAtlas_Mask.png` is the packed material companion
to `DungeonAtlas.png`. It must have identical dimensions and tile layout. The
shader samples it with the already-resolved base-atlas UV; do not author a
second orientation or addressing scheme. Its linear channels are R emission,
G roughness, B metallic, and A reserved. Import it as Default/linear with Point
filtering, Clamp wrapping, mipmaps disabled, and compression disabled. Alpha is
currently ignored and must not be repurposed without updating this contract.

The initial validation atlas contains localized emission, low-roughness wet
stone, metal, and ordinary rough stone regions. Unpainted areas are neutral
`(R=0, G=1, B=0, A=1)`. Shader defaults disable mask evaluation, emission, and
specular; the representative shared material may retain validated enabled values.

The atlas represents authored full-light color. The URP Unlit output multiplies
that color by global presentation brightness, vertex-color red AO, and a
quantized propagated dungeon-light multiplier. The shader derives non-negative
Rec.709 luminance from HDR `_DungeonLightTexture + _DungeonAmbientColor`, applies
the `_LightExposure` response, quantizes the shaped result, and remaps it from
`_MinLight` to 1. Local-only excess energy may add bounded multiplicative
overbright. Defaults are `_LightSteps = 4`, `_MinLight = 0.25`, exposure `1`,
overbright threshold `0.9`, overbright response `1.25`, maximum overbright `1.75`,
normal color influence `0.35`, and hot-core color influence `0.8`. Strong local
HDR energy may additionally apply an atlas-luminance-masked additive wash; its
black/full points protect authored dark pixels before source-colored lift is
added. Vertex AO and global presentation brightness also constrain that wash.
Previous/current propagated fields are interpolated over the dynamic refresh
interval, and visible sampling snaps to the manager's world-grid lighting pixels
per cell. The snapped location resolves to an exact propagation texel center
using separate active samples-per-cell metadata, so high visible densities such
as 16 or 32 remain independent from Smooth2x/Smooth4x field resolution. Local RGB
tint is normalized independently from ambient and applied
multiplicatively through `_LightColorInfluence`, default 0.35.
Optional mask-driven surface emission and stylized specular layer onto this
lighting result. Specular uses roughness/metallic mask channels, propagated local
light energy, the world normal/view direction, and an art-directed shared light
direction. It can blend independently between smooth and quantized response.
There is still no normal map, GI/reflection contribution, smooth normal-diffuse
response, or realtime local-light shadowing.

### Configurable ground depth bands

Ground stratification uses `DungeonGroundSurfaceFamily`, separate from the
Primary/Secondary/Accent/Special surface-slot mapping. Each inspector-visible
band has a display name, inclusive minimum and maximum discrete depth, an
optional unbounded flag, and weighted sliced Sprite variants. Ranges must begin at zero,
remain contiguous and non-overlapping, and end in exactly one unbounded band.
Weights are relative non-negative values: `7/2/1` is equivalent to `70/20/10`,
and zero disables an entry. If every weight is zero, generation warns and uses
the first valid Sprite as a deterministic fallback.

The default `DefaultGround` family currently maps the re-sliced atlas assets as:

- Top, depth 0: `DungeonAtlas_0` (the former `Ground_Layer_1` region);
- Mid, depths 1-2: `DungeonAtlas_2` (former `Ground_Layer_2`);
- Fill, depth 3+: `DungeonAtlas_6` (former `Ground_Layer_3`).

The lookup generator bakes a second RGBAFloat texture. Each ground family owns
one depth-map row followed by two rows per band. Columns 0-255 of the
depth row map discrete depth to band; column 256 contains the deep fallback.
The first band row stores variant count/total weight in column zero and
normalized Sprite.rect values in columns 1-16. The second stores 256 pre-baked
weighted-choice indices. The shader hashes logical cell, depth, family row, and
visual seed, reads a choice index, then reads its rect. It does not loop over the
artist-authored weights per fragment.

`DungeonGroundSurfaceAppearance` supplies the lookup start row, visual seed,
ground-top world Y, logical cells per tile (default 3), and dungeon-tile world
size (default 1) through a `MaterialPropertyBlock`. Their ratio is the logical
ground-cell scale. `floor(projected * scale)` selects a cell while
`frac(projected * scale)` supplies only its Sprite-local UV. Depth is floored
from `(topY - surfaceWorldY) / cellWorldSize`. An optional
reference Transform supports a region-owned top elevation; otherwise the
explicit value is used. Different columns/regions can therefore use independent
top references without material instances. When a generator already owns a
discrete elevation, call `Configure(topWorldY, tileWorldSize, cellsPerTile, seed)` rather than
encoding depth in mesh channels.

## Traps

Location: `Assets/Resources/Traps/`

A normal one-cell trap prefab should:

- contain one `CellTrap` subclass;
- treat the grid cell as its logical occupancy;
- keep trap-local animation/effect components inside the prefab;
- expose deterministic orientation through placement rather than depending on arbitrary scene rotation.

Traps reserve an external service cell and affect a neighboring traversed target
cell. Compatible tile prefabs may expose construction-surface anchors for
mechanism/support visuals; logical occupancy and triggering remain grid-owned.

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
