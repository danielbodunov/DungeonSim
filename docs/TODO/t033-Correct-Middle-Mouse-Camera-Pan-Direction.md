# t033 — Correct Middle-Mouse Camera Pan Direction

## Tracking
- **ID:** t033
- **Status:** Planned
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
Record:
- input/controller code changed
- axis/sign correction made
- sensitivity/smoothing compatibility
- interaction with t032 if present
- manual validation results

## Git
Suggested implementation branch: `fix/t033-middle-mouse-pan-direction`

Proceed according to `docs/AGENTS.md`.
