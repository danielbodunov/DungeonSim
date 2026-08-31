# RENDER-09 — Exterior / Out-of-Bounds Ground Rendering

## Tracking
- **ID:** RENDER-09
- **Status:** Planned
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
