# t024 — Rotation-Safe Tile Textures

## Tracking
- **ID:** t024
- **Status:** Complete
- **Milestone:** Strategic Construction
- **Related:** t021 — Modular Tile Construction Surfaces

## Goal
Define and implement a texture-authoring/rendering contract that lets one dungeon tile prefab be reused across its existing 90-degree `TileSocketProfile` rotations without visually rotating gravity- or world-oriented pixel textures into incorrect orientations.

## Problem
Dungeon Sim resolves one source prefab through multiple 90-degree profile rotations. This is correct for topology and geometry reuse, but a conventional mesh-UV workflow rotates the texture with the prefab. As a result, a face that was authored as a side wall may become a floor, while directional wall/floor patterns, grime, chains, trim, or other pixel-art details rotate into visually incorrect orientations.

The texture system must support tile-profile rotation without changing the authoritative grid, socket, or rotation model.

## Direction
Keep geometry/profile rotation authoritative and solve texture orientation in the visual layer.

The first implementation should establish a simple production-safe convention rather than a fully generalized material system. Candidate approaches may include shader-side UV counter-rotation, world-/surface-oriented sampling, surface-role selection from transformed normals, or explicit rotation metadata. The chosen approach should remain compatible with atlas-based pixel textures.

Authored structural AO should initially use vertex colors and rotate with the mesh. Do not couple contact AO to the base texture orientation.

## Requirements
- Audit how current `TileSocketProfile.rotation` is applied to instantiated tile prefabs and confirm the four supported orientations.
- Define which texture information should remain world/gravity oriented versus rotate with the local geometry.
- Preserve the existing 90-degree tile-profile rotation and adjacency behavior.
- Support the planned pixel-art atlas workflow without requiring a separately authored texture set for every prefab rotation.
- Ensure floors, ceilings, side walls, and back-wall surfaces can retain a consistent visual orientation after profile rotation.
- Establish a vertex-color AO convention for structural/contact darkening, initially reserving the red vertex-color channel for AO.
- Document the Blender/FBX/Unity authoring expectations needed for new dungeon tiles.
- Prototype the solution on at least one representative Narrow or Wide tile using all four profile rotations.

## Acceptance Criteria
- One representative tile uses the same source prefab and atlas artwork across R0/R1/R2/R3 without directional base texture art appearing unintentionally rotated.
- A surface that changes structural role through profile rotation (for example, side wall to floor) receives an appropriate orientation/surface treatment without requiring a duplicate prefab per rotation.
- Back-wall artwork remains visually upright/consistent across supported rotations.
- Vertex-color contact AO follows the rotated geometry correctly and can be adjusted independently of the base atlas texture.
- Pixel sampling remains crisp with no filtering/bleeding introduced by the rotation solution.
- Existing socket hashes, profile compatibility, traversal, and placement behavior are unchanged.
- The resulting texture/UV/vertex-color authoring contract is documented for future tile creation.

## Investigation Notes
Before committing to a shader architecture, compare the smallest viable options:

1. Counter-rotate atlas UVs using the resolved profile rotation.
2. Derive orientation from world-space position/normal for selected surface classes.
3. Use canonical UVs plus compact per-surface metadata for atlas cell and orientation.

Prefer the least complex option that handles Dungeon Sim's actual four-rotation tile model and does not block later material variation or detail overlays.

## Out of Scope
- Full environment art pass
- Screen-space AO
- Runtime damage/decal system
- Arbitrary 3D tile pitch/roll beyond the existing profile rotations
- Replacing the current dungeon topology or adjacency solver
- Building a generalized texture-painting editor

## Git
Suggested branch: `feature/t024-rotation-safe-tile-textures`

## Implementation Notes

- Audited runtime and editor preview instantiation: both rotate the shared source
  prefab clockwise around Z by `rotation * -90` degrees. Socket/profile data was
  not changed.
- Compared the proposed approaches. UV counter-rotation cannot handle a face
  changing from wall to floor without additional role data; explicit per-face
  metadata is more authoring overhead than the current tile set needs. The
  prototype therefore derives surface role and projection from transformed
  world normals/positions.
- Added a URP Shader Graph with four-role pixel-atlas projection and half-texel
  cell insets. The active target is now Unlit: authored atlas color is the
  full-light appearance, while red-channel vertex AO and stylized lighting are
  strictly multiplicative.
- Prototyped the contract on the existing `Narrow_Corner_L` source prefab, whose
  four profile assets already reference the same prefab for R0-R3.
- Documented the atlas layout and Blender/FBX/Unity authoring contract in the
  prefab reference and tile how-to.
- Replaced the fixed quadrant prototype with Sprite-driven `DungeonSurfaceFamily`
  assets backed by the sliced production `DungeonAtlas.png`.
- Added an editor lookup generator, a 17-column RGBAFloat family/role/variant
  encoding, UV2.x semantic slots, stable seeded selection, and per-instance
  family mappings applied through `MaterialPropertyBlock`.
- Added `DungeonStone` and `Brickwork` sample families and migrated
  `Narrow_Corner_L` to the default family-aware appearance component.
- Added configurable `DungeonGroundSurfaceFamily` depth bands with validation,
  a baked depth-to-band/variant lookup, per-instance top reference and layer
  height, and deterministic band variants through the shared atlas material.
- Added `DefaultGround` (Top 0, Mid 1-2, Fill 3+) using the current generic
  Sprite slice names, and migrated `Ground_Full_X` to ground appearance metadata.
- Ground addressing now resolves each logical 32x32 projected cell independently.
  Current geometry defaults to three logical cells per tile; `floor()` drives
  cell/depth selection and `frac()` drives only Sprite-local sampling.
- Ground variants now have non-negative relative weights. The generator bakes a
  256-entry weighted-choice row per band for deterministic per-cell selection
  without dynamic per-fragment weight-array loops.
- Ordinary Back Wall, Floor, Ceiling, and Side Wall family variants now use the
  same weighted Sprite entries and pre-baked deterministic choice rows.
- `_GlobalLightIntensity` remains the true global presentation scalar consumed
  by `DungeonVisualLightingController`
  is its sole runtime writer and maps Expansion to `1.0`, Exploring to `0.55`,
  unresolved/default to `1.0`, and optional debug override to `1.0`.
- Phase targets blend over `0.3` unscaled seconds. Debug override is available
  through `SetDebugOverride`/`SetDebugBrightness`; automatic debug switching is
  deferred because no authoritative runtime debug-state event currently exists.
- Replaced the unstable Shader Graph implementation with the handwritten URP
  `RotationSafeTileAtlas.shader`. Its output is `AtlasRGB *
  _GlobalLightIntensity * QuantizedDungeonLightMultiplier * VertexColorR`; it has no
  metallic, smoothness, specular, reflection, GI, emission, or normal-diffuse
  path.
- The shader has no main-directional-light or main-light-shadow receiver path.
  `_DungeonLightTexture + _DungeonAmbientColor` is converted to saturated Rec.709
  luminance, quantized as `round(lightAmount * (steps - 1)) / (steps - 1)`, and
  remapped through `lerp(_MinLight, 1, quantized)`; `_MinLight` defaults to
  `0.25`. The result multiplies atlas RGB, preserving authored dark details.
- Colored field values currently affect luminance only; restrained
  multiplicative tint is a possible follow-up. NPC/local light-field sources do
  not cast realtime Unity shadows. The `ShadowCaster` pass remains for future
  compatibility; stylized point/spot-light shadows require a separate ticket.
  
## Validation Notes
- Manual Unity validation was confirmed complete by the user on 2026-08-27

## Unity Validation

1. Let Unity compile scripts and import `RotationSafeTileAtlas.shader`;
   confirm there are no graph, shader, or C# errors.
2. Run `Tools > Dungeon > Rebuild Surface Family Lookup`. Confirm it generates
   `Assets/Resources/DungeonSurfaceLookup.asset` without validation errors and
   assigns `DungeonAtlas` plus the lookup to the shared material.
3. Inspect `DungeonAtlas` import settings: Multiple/32x32 slices, Point filter,
   mipmaps disabled, compression None, and Clamp wrap mode.
4. Open a generation/test scene that can place `Narrow_Corner_L` profiles and
   display R0, R1, R2, and R3 instances of that same source prefab.
5. Confirm role changes select the correct family role, sprite art stays upright,
   variants differ by projected cell but remain stable between frames, and no
   neighboring Sprite rect bleeds.
6. Change the appearance seed and Primary family, then confirm another instance
   of the same prefab can retain the original family without material cloning.
7. On a UV2-authored test surface, map Primary and Accent to different families
   and confirm rotation changes role without changing its semantic slot.
8. Reassign one family variant to another sliced Sprite, rebuild, and confirm the
   sampled rect changes without manual coordinates or mesh UV changes.
9. Paint or temporarily edit red vertex color on representative geometry;
   confirm its multiplicative AO follows the mesh while atlas orientation does
   not change.
10. Re-run tile socket/profile validation and an NPC traversal smoke test. Confirm
   socket hashes, profile compatibility, adjacency, and traversal are unchanged.
11. Inspect `DefaultGround`, then rebuild the lookup. Confirm depth 0 uses Top,
    depths 1-2 use Mid, and depth 3+ uses Fill on `Ground_Full_X` instances.
12. Change Mid to depths 1-4 and Fill to 5+, rebuild, and confirm behavior changes
    without editing the Shader Graph or creating materials.
13. Add a second Sprite variant to a ground band and confirm different projected
    cells vary deterministically while remaining stable between frames.
14. Validate two regions with different Ground Top Y/reference Transform values;
    confirm each computes depth relative to its own configured top.
15. With `Logical Cells Per Tile` set to 3, confirm one vertical polygon renders
    Top/Mid/Mid and the next three cells render Fill; the Top grass band must not
    repeat within the first tile.
16. Add weighted variants (for example 7/2/1), rebuild, and inspect a broad area.
    Confirm approximate distribution, independent horizontal/vertical logical
    cells, deterministic results after looking away/back, and no flicker.
17. Set one weight to zero and confirm it is never selected. Set all weights to
    zero and confirm the warning plus first-valid-Sprite fallback. Confirm a
    negative serialized weight blocks lookup generation.
18. Repeat weighted-variant validation on a regular `DungeonSurfaceFamily` role.
    Confirm the selected wall/floor/ceiling variants follow their relative
    weights per projected logical cell and remain stable across frames.
19. Enter Play Mode in Expansion and confirm `_GlobalLightIntensity` resolves to
    `1.0`. Open the dungeon and confirm it blends to `0.55`, then returns to
    `1.0` when Expansion resumes, including when entering Play Mode in a known phase.
20. Call `SetDebugOverride(true)` and confirm the debug target wins; disable it
    and confirm the current phase target returns. Inspect representative tile
    renderers throughout and confirm the shared material never becomes `(Instance)`.
21. Disable or remove the scene directional light and confirm representative
    tiles render identically. Move an NPC or configured `DungeonLightSource` and
    confirm nearby tiles update through `_LightSteps` discrete brightness bands.
    Confirm near-black atlas recesses remain proportionally darker than adjacent
    stone in every band and that local sources do not produce realtime shadows.
