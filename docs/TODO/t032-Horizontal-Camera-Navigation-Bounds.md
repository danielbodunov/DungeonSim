# t032 — Horizontal Camera Navigation Bounds

## Tracking
- **ID:** t032
- **Status:** Planned
- **Milestone:** Camera / Navigation UX
- **Depends on:** Existing playable-grid bounds

## Goal
Prevent the gameplay camera from panning indefinitely past the left and right boundaries of the playable grid while preserving normal camera movement, zoom, and viewing of decorative terrain beyond those boundaries.

## Requirements
- Derive horizontal navigation limits from the authoritative playable-grid extent rather than rendered-ground size.
- Clamp the camera navigation/focus target along the board's horizontal axis so panning cannot move indefinitely beyond the left or right playable boundary.
- Keep the calculation correct if the board/grid origin is offset; do not hard-code world-space bounds for one scene.
- Account for the camera's visible horizontal footprint/zoom where practical so the viewport does not expose excessive empty space merely because the camera pivot itself is technically inside the grid.
- Handle cases where the entire playable width fits inside the viewport without oscillation or contradictory min/max clamps.
- Preserve existing camera zoom, vertical/depth navigation, follow/focus behavior, and input sensitivity unless a narrowly required compatibility adjustment is needed.
- Keep the bounds independent from RENDER-09 exterior/decorative ground extent.
- Expose a small configurable edge margin only if needed for framing; gameplay bounds remain the source of truth.

## Coordinate Contract
Prefer board/grid-local horizontal bounds over assuming that the intended left/right direction is always raw world X.

The camera may show decorative exterior terrain near an edge, but the navigation target must remain constrained by the playable board.

## Acceptance Criteria
- Repeated horizontal pan input cannot move the camera/navigation focus indefinitely past the playable grid's left boundary.
- The same is true for the right boundary.
- Bounds work for grids whose origin/position differs from the default test scene.
- Bounds remain stable at the supported minimum and maximum zoom levels.
- A grid narrower than the viewport produces stable framing rather than jittering between impossible clamp limits.
- Decorative exterior terrain from RENDER-09, when present, does not expand the movement bounds.
- Existing non-horizontal camera controls continue to behave as before.
- Camera follow/focus behavior respects the same bounds or returns to a valid bounded state when control returns to the player.

## Out of Scope
- Top/bottom or depth-axis camera limits unless required to preserve existing behavior
- Camera zoom redesign
- Camera rotation redesign
- Cinematic camera behavior
- Rendering exterior terrain (RENDER-09)

## Manual Validation
1. Pan continuously toward the left edge at several zoom levels and confirm movement stops at a stable framing point.
2. Repeat at the right edge.
3. Test a board/grid with a non-default world position/origin.
4. Test a board narrower than the viewport.
5. If exterior ground exists, confirm it can remain visible without becoming additional navigable camera space.
6. Exercise existing focus/follow and normal pan controls after hitting a bound.

## Post-Implementation Report
Record:
- camera/controller owner changed
- playable-bound source used
- board/world coordinate conversion used
- viewport/zoom compensation method
- edge-margin behavior if any
- focus/follow integration
- manual validation results

## Git
Suggested implementation branch: `feature/t032-camera-horizontal-bounds`

Proceed according to `docs/AGENTS.md`.
