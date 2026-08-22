# t024 — Rotation-Safe Tile Textures

## Tracking
- **ID:** t024
- **Status:** Planned
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
