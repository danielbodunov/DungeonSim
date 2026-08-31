# RENDER-08 — Consolidated Ground Surface Rendering

## Tracking
- **ID:** RENDER-08
- **Status:** Planned
- **Milestone:** Rendering / Ground Optimization
- **Depends on:** Existing grid occupancy/build-obstacle state and current ground material/texture pipeline

## Goal
Replace per-cell ground rendering with a consolidated ground-surface representation while preserving the current ground material appearance and the gameplay grid as the authoritative source of occupancy.

The visual ground should fill playable-grid space that is not replaced or blocked by authoritative build obstacles or player-placed tiles/structures without requiring one renderer and one collider per ground cell.

## Design Intent
Gameplay cells and rendered ground do not need a one-to-one GameObject relationship.

The preferred baseline is a generated mesh containing quads only for currently exposed ground cells:

```text
Grid / Occupancy State
        |
        +-- build obstacle?
        +-- player construction?
        +-- exposed ground?
                |
                v
        Ground Surface Builder
                |
                v
        Consolidated Ground Mesh
                |
                v
          Single Renderer
```

A shader-driven occupancy mask remains an acceptable implementation alternative if it is demonstrably simpler or materially better for update cost, but the ticket requirement is consolidated rendering rather than a specific shader technique.

## Requirements
- Preserve the current ground material, texture family, point-filtering behavior, UV/atlas conventions, and intended pixel-art appearance.
- Stop requiring an individual `MeshRenderer` for every ordinary ground cell.
- For the normal playable ground using one material family, target a single consolidated `MeshRenderer`.
- Build the rendered surface from authoritative grid/occupancy state rather than creating a second independent source of gameplay truth.
- Omit ground visual geometry for cells that should not show ordinary ground because they are occupied by generated build obstacles or player-placed dungeon tiles/structures.
- Update/rebuild the consolidated surface only when relevant occupancy or visual state changes; do not regenerate it every frame.
- Preserve deterministic visual selection where ground-cell texture variation is currently deterministic.
- Audit uses of existing per-ground-cell colliders before removing them.
- If ground colliders are used only for cursor/grid selection, replace that dependency with grid-plane/world-position resolution rather than retaining per-cell colliders.
- If a physical ground collision surface is genuinely required, use a consolidated collision representation rather than one collider per ground cell where practical.
- Keep pathfinding, construction occupancy, obstacle state, and tile gameplay independent of the rendering mesh.

## Implementation Guidance
Prefer a generated mesh as the first implementation because it:
- emits geometry only for visible/exposed ground cells;
- retains ordinary material and UV behavior;
- avoids shader discard/masking cost for hidden cells;
- is cheap to rebuild when construction changes are relatively infrequent;
- naturally removes the per-cell renderer/GameObject requirement.

A large fixed mesh plus occupancy texture/buffer may be considered instead if profiling or implementation constraints justify it. Do not introduce both approaches in the same ticket unless required.

## Performance Validation
Record before/after counts in a representative dungeon for:
- ground-related GameObjects;
- `MeshRenderer` components;
- ground-related colliders;
- draw calls/batches where measurable;
- mesh vertex/triangle count;
- rebuild/update cost after placing and removing representative construction.

The purpose is to verify that consolidation materially reduces component/render-management overhead without causing a disproportionate rebuild cost.

## Acceptance Criteria
- A representative playable grid renders ordinary exposed ground with one consolidated ground renderer under the normal single-material configuration.
- The consolidated surface visually matches the current ground material/texture behavior closely enough that the optimization does not require reauthoring the ground art.
- Build-obstacle cells do not incorrectly display ordinary ground where the current design expects them to replace it.
- Player-placed tile/structure cells can remove/replace the corresponding ordinary ground visual without leaving stale geometry.
- Removing relevant construction restores the expected exposed ground visual.
- Ground rendering is derived from authoritative grid/occupancy data rather than becoming a parallel gameplay state system.
- No per-cell ground collider remains solely to determine the cursor's grid coordinate.
- If collision is required for another system, it is documented and consolidated where practical.
- Before/after renderer, collider, and draw/batch measurements are recorded.
- Existing construction, obstacle, save/load, and navigation behavior remains unchanged except for narrowly required rendering-integration changes.

## Out of Scope
- Rendering terrain outside the playable grid (RENDER-09)
- New exterior texture families (RENDER-09)
- Redesigning the ground pixel-art atlas
- Changing build-obstacle gameplay rules
- Changing player construction rules
- Full terrain heightfield support
- Runtime terrain deformation
- General mesh-combining infrastructure for unrelated props/tiles

## Manual Validation
1. Compare the same dungeon before and after consolidation and verify exposed ground appearance/UVs remain correct.
2. Inspect a dungeon containing generated build obstacles and verify ordinary ground is omitted where expected.
3. Place and remove representative player construction and verify the ground surface updates correctly.
4. Test cursor/grid selection across interior and edge cells after per-cell collider removal.
5. Exercise NPC movement and any physics-sensitive gameplay to confirm no hidden dependency on per-cell ground colliders was broken.
6. Capture before/after renderer, collider, and draw/batch counts.

## Post-Implementation Report
Record:
- final rendering approach (generated mesh, shader occupancy mask, or other)
- authoritative inputs used to build the surface
- renderer/material/submesh structure
- UV/atlas preservation method
- occupancy-change invalidation/rebuild method
- ground-collider audit result and replacement method if removed
- before/after performance/component measurements
- compatibility notes for RENDER-09

## Git
Suggested implementation branch: `render/render08-consolidated-ground`

Proceed according to `docs/AGENTS.md`.
