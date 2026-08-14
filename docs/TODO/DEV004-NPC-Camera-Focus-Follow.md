# DEV004 — NPC Camera Focus & Follow

## Tracking

- **ID:** DEV004
- **Status:** Complete
- **Milestone:** Developer Tooling — Observation & Control
- **Blocks:** DEV005

## Goal

When an NPC is selected through the runtime debug harness, allow the gameplay camera to smoothly focus, zoom, and follow that NPC. Implement the underlying focus/follow capability as production-capable camera functionality so it can later support normal player-facing NPC observation without being rewritten.

## Requirements

- Add a reusable camera focus/follow API owned by the normal gameplay camera system rather than the Editor debug window.
- The API should accept a generic target, such as a `Transform`, rather than depending on `NPCTraversalAgent` or debug-only types.
- Selecting an NPC in the runtime debug harness should request camera focus/follow.
- Focus should smoothly transition toward a configurable viewing distance/zoom rather than hard snapping.
- While focused, the camera should continue following the moving target.
- Clearing debug selection should release focus and restore normal camera control.
- Define intentional behavior for manual camera input while following. Prefer a clean cancel/release behavior over fighting the player's input.
- Handle target destruction/despawn safely by clearing focus.
- Keep the debug harness as a caller/adapter only; do not place production camera behavior inside `Editor/` code.

## Acceptance Criteria

- Selecting a running NPC from the debug harness focuses and follows it.
- The transition is smooth and uses configurable focus framing/distance.
- The camera tracks the NPC as it moves through the dungeon.
- Clearing selection returns the camera to ordinary control without a jump or stuck follow state.
- NPC death/despawn does not leave an invalid camera target.
- Manual camera input behaves according to the documented follow-cancel policy.
- The underlying focus API can be invoked by future gameplay code without referencing the debug harness.

## Architecture Direction

Camera focus/follow is **production-capable functionality exposed through developer tooling**. It is expected to become useful for player-facing NPC selection/observation later.

Prefer semantics such as:

`FocusTarget(Transform target)`

`ClearFocus()`

rather than debug-specific APIs such as `DebugFollowNPC()`.

Do not build the eventual gameplay NPC-selection UI in this ticket.

## Out of Scope

- Player-facing NPC selection UI
- Cinematic camera systems
- Multiple-target framing
- Camera bookmarks
- Spectator camera modes
- Selective simulation pause

## Manual Validation

1. Enter Play Mode with several NPCs.
2. Select NPC A from Game View using the runtime debug harness.
3. Verify the camera smoothly centers and zooms toward NPC A, then follows both horizontal and vertical movement.
4. Select NPC B and verify focus transitions correctly.
5. Click empty space in Game View, then repeat with the harness **Clear** button. Verify both release focus at the current view without a jump or stuck movement.
6. Focus an NPC again and use wheel zoom in both directions. Verify viewing distance changes smoothly while the camera continues following the NPC.
7. Use keyboard pan and middle-mouse drag individually. Verify each releases follow and immediately controls the camera while the NPC remains selected for inspection.
8. Select NPC B again after a manual cancellation and verify focus can be reacquired.
9. Kill or allow the selected NPC to despawn and verify focus clears safely while normal camera controls continue working.
10. Disable **Enable Game View NPC Selection** while focused and verify selection, highlight, and camera focus all clear.

## Implementation Status

- Extended the production `CameraFollow` component with the generic `FocusTarget(Transform)` and `ClearFocus()` APIs. No runtime camera behavior depends on NPC or Editor-only types.
- Added configurable focus zoom, world-space framing offset, transition/follow smoothing time, and manual-pan cancellation policy as serialized camera settings.
- Perspective cameras smoothly dolly toward the focus distance while tracking the target framing point. Orthographic cameras smoothly change size and track target X/Y while retaining camera depth.
- Mouse-wheel zoom adjusts the active focused distance without interrupting tracking. Keyboard pan or middle-mouse drag cancels focus before applying that same input, and control resumes from the current view rather than restoring an old camera pose.
- A destroyed target is detected through the production component's normal update and releases focus without retaining an invalid transform.
- The NPC Runtime Debug Harness only adapts selection to the generic camera API. Selecting through Game View or Hierarchy requests focus; empty-space selection, **Clear**, disabling selection mode, closing the window, and leaving Play Mode release it.
- Existing legacy `followTarget` behavior remains available when the explicit focus API is inactive.

## Known Limitations

- Focus uses one target and a fixed configured framing offset/zoom. Multi-target framing, bookmarks, and cinematic composition remain outside DEV004.
- Manual pan cancellation does not clear the NPC debug selection; it only hands camera control back to the user. Re-selecting an NPC requests focus again.

## Validation Performed

- Runtime and Editor assemblies compile successfully.
- Manual Unity validation completed successfully on 2026-08-14.
- Smooth focus and follow, NPC switching, focused wheel zoom, selection clearing, manual-pan cancellation, focus reacquisition, target death/despawn, and disabling selection mode were validated in Unity.

## Git

Suggested branch: `dev/DEV004-npc-camera-focus`

Active branch: `tool/dev004-npc-camera-focus` (`dev/` is unavailable because a branch named `dev` already occupies that Git ref namespace.)

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
