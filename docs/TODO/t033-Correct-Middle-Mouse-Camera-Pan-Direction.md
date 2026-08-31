# t033 — Correct Middle-Mouse Camera Pan Direction

## Tracking
- **ID:** t033
- **Status:** Awaiting Unity Validation
- **Milestone:** Camera / Input UX
- **Depends on:** Existing middle-mouse camera-pan input

## Goal
Correct the inverted middle-mouse click-and-drag camera-pan behavior so dragging the view feels like grabbing and moving the board/world in the expected direction.

## Expected Interaction
Use a grab-style visual contract rather than defining the fix only in terms of raw input signs:

- dragging the mouse to the right moves the viewed board/world to the right on screen;
- dragging the mouse to the left moves the viewed board/world to the left on screen;
- equivalent behavior applies to the other supported pan axis.

The underlying camera/focus transform therefore moves opposite the screen-space content motion as appropriate for the current camera implementation.

## Requirements
- Reverse/correct the current middle-mouse drag direction on the affected axes.
- Preserve existing pan sensitivity, acceleration/smoothing, button binding, and input lifecycle unless a change is strictly required for correctness.
- Do not alter unrelated keyboard, edge-scroll, follow/focus, zoom, or rotation controls.
- Keep camera bounds compatible with t032 when both tickets are implemented.
- Apply the correction at the narrowest input-to-pan conversion point rather than compensating elsewhere in camera movement code.

## Acceptance Criteria
- Middle-mouse drag right produces rightward on-screen movement of the board/world.
- Middle-mouse drag left produces leftward on-screen movement of the board/world.
- The other supported drag axis follows the same grab-style interaction convention.
- Pan speed/sensitivity remains materially unchanged apart from direction.
- Starting and releasing a middle-mouse drag does not introduce a jump in camera position.
- Existing non-middle-mouse camera input is unchanged.
- If t032 is present, corrected dragging still clamps correctly at horizontal board boundaries.

## Out of Scope
- Rebinding middle mouse
- Adding alternate pan bindings
- Camera smoothing redesign
- Camera-bound implementation itself (t032)
- Touch/gesture camera controls

## Manual Validation
1. Start from the center of the board and drag right, left, and along the other supported pan axis.
2. Compare movement speed to the pre-fix behavior and confirm only direction changed.
3. Release and restart dragging from several cursor positions and confirm no jump occurs.
4. Exercise keyboard/other camera inputs and verify they are unaffected.
5. If t032 is present, drag against both horizontal bounds and verify the corrected direction and clamp interact correctly.

## Post-Implementation Report

- Updated only the middle-mouse input-to-pan conversion in
  `Assets/Scripts/CameraMovement.cs` (`CameraFollow`). Keyboard movement, wheel
  zoom, focus/follow, orthographic/perspective updates, and smoothing remain
  unchanged.
- Changed the default mouse conversion from `(mouseDelta.x, -mouseDelta.y)` to
  `(-mouseDelta.x, -mouseDelta.y)`. The camera/focus target therefore moves
  opposite cursor motion on both supported axes, producing grab-style on-screen
  board movement.
- Preserved the exact `panMouseSensitivity` multiplier. The existing
  `invertMiddlePan` field is now a stable explicit whole-drag override instead
  of being toggled on each middle-button press; no press/release state changes
  the pan sign or target position.
- Added `CameraPanDirectionTests` covering positive/negative X and Y,
  sensitivity-scaled deltas, zero Z movement, and magnitude-preserving inversion.
- Runtime and editor assemblies compile successfully. The only editor warning
  is the pre-existing unused `TileSocketBakerWindow.visualizeSamples` field.
- t032 remains Planned and has no bounds code on this branch. The correction is
  upstream of camera target movement, so a later target clamp can consume the
  corrected delta without compensating signs.

Manual Unity drag-direction, restart/no-jump, non-middle-input, and eventual
t032-bound validation remain pending.

## Git
Suggested implementation branch: `fix/t033-middle-mouse-pan-direction`

Proceed according to `docs/AGENTS.md`.
