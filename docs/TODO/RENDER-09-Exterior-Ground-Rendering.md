# RENDER-09 — Exterior / Out-of-Bounds Ground Rendering

## Tracking
- **ID:** RENDER-09
- **Status:** Complete
- **Milestone:** Rendering / World Presentation
- **Depends on:** RENDER-08

## Goal
Extend ground presentation beyond the playable grid so the dungeon does not visually terminate at the gameplay boundary, while keeping exterior terrain decorative and independent from playable-grid authority.

Support the option for exterior ground to use the same ground texture family as the playable area or a distinct exterior texture/material family.

## Design Contract
Keep playable extent and visual extent separate:

```text
Visual Ground Extent
+-----------------------------------+
|            exterior               |
|     +-----------------------+     |
|     |     PLAYABLE GRID     |     |
|     | build / path / select |     |
|     +-----------------------+     |
|            exterior               |
+-----------------------------------+
```

Exterior ground exists for presentation only. It must not create playable cells merely because it is visible.

## Requirements
- Build on the consolidated ground approach established by RENDER-08 rather than reintroducing per-cell exterior renderers.
- Add configurable visual padding beyond the playable grid on the relevant board edges.
- Render the exterior as a consolidated surface or similarly low-overhead representation.
- Allow the exterior region to use:
  - the normal playable-ground texture/material family; or
  - an explicitly assigned exterior texture/material family.
- Keep the playable grid as the authority for construction, occupancy, selection, navigation, spawn rules, and gameplay coordinates.
- Exterior visual space must not become buildable or navigable unless a later feature explicitly expands the playable grid.
- Exterior rendering must not expand camera-navigation bounds introduced by t032.
- Preserve clean transitions at the playable-grid edge without requiring hidden gameplay cells.
- Keep exterior visual configuration serializable/configurable with the appropriate board/world-generation owner rather than scattering hard-coded distances in rendering code.

## Renderer Boundary
A separate consolidated exterior renderer is acceptable and may be preferable when it cleanly separates:
- playable exposed-ground geometry;
- decorative out-of-bounds geometry;
- different material/texture families.

The optimization requirement is to avoid a renderer-per-exterior-cell model, not to force playable and exterior terrain into the same renderer when separate material behavior makes that counterproductive.

## Acceptance Criteria
- Visible ground extends beyond the left/right playable-grid boundaries and any other configured edges without adding gameplay cells.
- Exterior extent/padding is configurable.
- Exterior terrain can use the same visual family as playable ground.
- A different exterior texture/material family can be assigned without altering playable-ground appearance.
- Exterior terrain uses a consolidated renderer/surface rather than one renderer per decorative cell.
- Cursor selection cannot resolve exterior terrain as a valid playable cell.
- Construction cannot be placed outside the playable grid because of the added visuals.
- NPC navigation/pathfinding does not gain nodes solely from exterior rendering.
- Camera movement limits continue to use the playable-grid boundary rather than the visual-ground boundary.
- RENDER-08 ground replacement/rebuild behavior remains intact inside the playable grid.

## Out of Scope
- Procedurally expanding the playable grid
- Building outside the playable grid
- Exterior NPC navigation
- Exterior encounters/content spawning
- Biome-generation systems
- Complex terrain elevation outside the board
- Infinite terrain streaming
- Camera-bound implementation itself (t032)

## Manual Validation
1. Configure visible exterior padding and inspect all relevant board edges.
2. Verify the exterior is visually continuous with the playable ground when using the same texture family.
3. Assign a distinct exterior texture/material family and verify the playable region is unchanged.
4. Attempt cursor selection and construction outside the playable grid and confirm both remain invalid.
5. Run NPC navigation near board edges and confirm no exterior navigation space is created.
6. With t032 present, pan to both horizontal boundaries and confirm decorative exterior terrain remains visible while the camera cannot continue indefinitely into it.

## Post-Implementation Report
Record:
- visual-extent configuration owner
- exterior renderer/mesh strategy
- material/texture-family selection mechanism
- playable/exterior boundary representation
- selection/navigation/build safeguards
- interaction with t032 camera bounds
- performance/component counts for the exterior surface

## Git
Suggested implementation branch: `render/render09-exterior-ground`

Proceed according to `docs/AGENTS.md`.

## Implementation Report

- **Visual-extent owner:** `TileGridGenerator` owns serialized enablement, left/right/top/bottom padding (four cells per edge by default), and optional exterior material and `DungeonGroundSurfaceFamily` overrides.
- **Renderer strategy:** `DungeonExteriorGroundSurface` clones the normal ground template into a separate derived presentation root and emits every decorative cell into one mesh and one renderer. It creates no exterior collider. For grid size `W x H` and padding `L/R/T/B`, the exterior contains `(W + L + R) * (H + T + B) - W * H` quads, with four vertices and two triangles per quad.
- **Appearance selection:** With no overrides, the cloned template retains the playable ground material and family. A material override, family override, or both may be assigned without changing the playable consolidated renderer.
- **Boundary representation:** Exterior centers are aligned to the grid lattice but are generated only for coordinates outside the authoritative `[0, W)` and `[0, H)` ranges. Padding names retain world left/right/top/bottom meaning even when grid generation directions are negative.
- **Gameplay safeguards:** Exterior generation does not resize or mutate grid arrays, create playable cells, register occupancy, add path nodes, or create colliders. Pointer and construction validation continue through `TryWorldToPlayableCell`; generated obstacles continue to require `IsPlayableCell`.
- **Camera compatibility:** t032 remains sourced exclusively from `TileGridGenerator.TryGetPlayableWorldRect`; exterior padding is not consulted by camera navigation.
- **Validation:** Runtime and editor assemblies compile. `ExteriorGroundSurfaceTests` covers directional padding, exact exterior-cell count, consolidated mesh counts, exclusion from world-to-grid authority, unchanged playable bounds, distinct material/family assignment, the single-renderer contract, and zero active exterior colliders. Unity validation was completed in the project.
- **Manual validation:** Completed in Unity, including the configured distinct exterior appearance and the decorative-only playable-grid boundary behavior.

### Obstacle ground-suppression correction

- `GeneratedBuildObstacleDefinition.suppressOrdinaryGround` defaults to `false`. Normal obstacles (including boulders, bones, crates, and pillars) retain consolidated ground underneath; terrain-replacing definitions such as pits, holes, chasms, or lava openings can explicitly enable it.
- `TileGridGenerator.ShouldRenderOrdinaryGround` consults the resolved obstacle definition for each footprint cell. This is derived presentation only: construction/service-space blocking, obstacle occupancy, save records, navigation, and placement authority are unchanged. No ground prefab child was added to `Boulder_01`, and no scene or prefab edits are part of this correction.
- Focused regression coverage in `ConsolidatedGroundSurfaceTests` checks the default, both suppression settings across a rotated two-cell footprint, all four construction/service-space blocking combinations, unchanged occupancy and cell state, and unaffected neighboring ground.
- Correction validation: `dotnet build Assembly-CSharp-Editor.csproj` passed for runtime and editor assemblies (including tests), with one existing `CS0414` warning in `TileSocketBakerWindow`. A Unity EditMode run targeting `ConsolidatedGroundSurfaceTests`, `GeneratedBuildObstacleTests`, and `ExteriorGroundSurfaceTests` was attempted but aborted because this project is already open in another Unity instance; no test execution result is claimed for the correction. Run those suites in the open Editor, then visually confirm ground beneath a normal obstacle and suppression beneath an opted-in terrain-replacing obstacle.
