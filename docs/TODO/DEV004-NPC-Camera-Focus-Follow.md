# DEV004 — NPC Camera Focus & Follow

## Tracking

- **ID:** DEV004
- **Status:** Ready
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
3. Verify the camera smoothly focuses and follows NPC A.
4. Select NPC B and verify focus transitions correctly.
5. Clear selection and verify normal camera control returns.
6. Test manual camera input while following and verify the intended cancel/release behavior.
7. Kill/despawn the selected NPC and verify focus clears safely.

## Git

Suggested branch: `dev/DEV004-npc-camera-focus`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
