# t032 — Horizontal Camera Navigation Bounds

## Tracking
- **ID:** t032
- **Status:** Complete
- **Milestone:** Camera / Navigation UX
- **Depends on:** Existing playable-grid bounds

## Goal
Prevent the gameplay camera from panning indefinitely past the left, right, top, and bottom boundaries of the playable grid while preserving normal camera movement, zoom, and viewing of decorative terrain beyond those boundaries.

## Requirements
- Derive horizontal and vertical navigation limits from the authoritative playable-grid extent rather than rendered-ground size.
- Clamp the camera navigation/focus target along both board axes so panning cannot move indefinitely beyond the left, right, top, or bottom playable boundary.
- Keep the calculation correct if the board/grid origin is offset; do not hard-code world-space bounds for one scene.
- Account for the camera's visible horizontal and vertical footprint/zoom where practical so the viewport does not expose excessive empty space merely because the camera pivot itself is technically inside the grid.
- Handle cases where the entire playable width or height fits inside the viewport without oscillation or contradictory min/max clamps.
- Preserve existing camera zoom, follow/focus behavior, and input sensitivity unless a narrowly required compatibility adjustment is needed.
- Keep the bounds independent from RENDER-09 exterior/decorative ground extent.
- Expose a small configurable edge margin only if needed for framing; gameplay bounds remain the source of truth.
- Make the horizontal and vertical constraints configurable.

## Coordinate Contract
Prefer bounds derived from the board/grid coordinate mapping over hard-coding the intended horizontal and vertical directions to one scene's world-space layout.

The camera may show decorative exterior terrain near an edge, but the navigation target must remain constrained by the playable board.

## Acceptance Criteria
- Repeated horizontal pan input cannot move the camera/navigation focus indefinitely past the playable grid's left boundary.
- The same is true for the right boundary.
- Repeated vertical pan input cannot move the camera/navigation focus indefinitely past the playable grid's top or bottom boundary.
- Bounds work for grids whose origin/position differs from the default test scene.
- Bounds remain stable at the supported minimum and maximum zoom levels.
- A grid narrower or shorter than the viewport produces stable framing rather than jittering between impossible clamp limits.
- Decorative exterior terrain from RENDER-09, when present, does not expand the movement bounds.
- Existing non-horizontal camera controls continue to behave as before.
- Camera follow/focus behavior respects the same bounds or returns to a valid bounded state when control returns to the player.

## Out of Scope
- Camera zoom redesign
- Camera rotation redesign
- Cinematic camera behavior
- Rendering exterior terrain (RENDER-09)

## Manual Validation
1. Pan continuously toward the left edge at several zoom levels and confirm movement stops at a stable framing point.
2. Repeat at the right, top, and bottom edges.
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

## Implementation Report

- **Camera/controller owner:** `CameraFollow` clamps its orthographic camera target or perspective focus target before smoothing. Pan sensitivity, smoothing, zoom, and the t033 middle-mouse direction conversion are unchanged.
- **Playable-bound source:** `TileGridGenerator.TryGetPlayableWorldRect` derives a rectangle from the interior playable cells (`1..width - 2`, `1..height - 2`). Rendered ground and decorative exterior extents are not consulted.
- **Coordinate conversion:** The rectangle uses `GetCellWorldPosition`, `origin`, and signed `generationDirection`, including its cell spacing. This keeps offset and reversed grid layouts valid without scene-specific world limits.
- **Viewport/zoom compensation:** The four viewport-corner rays are projected onto `zoomFocusPlaneZ` at the target orthographic size or perspective dolly distance. Their minimum/maximum offsets inset the valid focus range. If a viewport axis is larger than the corresponding playable span, both impossible limits collapse to one balanced board center.
- **Configuration:** Horizontal and vertical constraints can be enabled independently. Viewport-footprint compensation can be disabled, and `navigationEdgeMargin` provides a non-negative X/Y framing margin outside the playable rectangle (default `0.5` world units per axis).
- **Focus/follow integration:** Manual pan, generic follow, and explicit focus all feed the same clamped target before camera smoothing. A focused object outside the playable range remains visible only as far as the configured board framing permits.
- **Automated checks:** Runtime and editor assemblies compile. `CameraNavigationBoundsTests` covers both-axis limits, an offset board, viewport-larger-than-board stability, per-axis configuration, margin behavior, and an offset/reversed grid's interior playable rectangle. The focused Unity EditMode run could not start because the project was already open in another Unity instance.
- **Manual validation:** Passed in Unity on 2026-08-31, as reported by the user.
