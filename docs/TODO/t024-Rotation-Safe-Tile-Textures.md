# t024 — Rotation-Safe Tile Textures

## Tracking
- **ID:** t024
- **Status:** Awaiting Unity Validation
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
- Added a URP Lit Shader Graph with four-role 2x2 pixel-atlas projection,
  half-texel cell insets, standard realtime-light support, and independent
  red-channel vertex AO. The former dungeon-light sampling is intentionally not
  part of the environment material.
- Prototyped the contract on the existing `Narrow_Corner_L` source prefab, whose
  four profile assets already reference the same prefab for R0-R3.
- Documented the atlas layout and Blender/FBX/Unity authoring contract in the
  prefab reference and tile how-to.

## Unity Validation

1. Let Unity import `RotationSafeTileAtlas.png`, its material, and shader; confirm
   there are no shader/compiler errors.
2. Inspect the atlas import settings: Point filter, mipmaps disabled, compression
   None, and Clamp wrap mode.
3. Open a generation/test scene that can place `Narrow_Corner_L` profiles and
   display R0, R1, R2, and R3 instances of that same source prefab.
4. Confirm wall/back-wall details remain upright, floor and ceiling treatments
   follow world gravity, and no seams bleed between atlas quadrants.
5. Paint or temporarily edit red vertex color on representative geometry;
   confirm Lit Ambient Occlusion rotates with the mesh while atlas orientation
   does not change.
6. Re-run tile socket/profile validation and an NPC traversal smoke test. Confirm
   socket hashes, profile compatibility, adjacency, and traversal are unchanged.
