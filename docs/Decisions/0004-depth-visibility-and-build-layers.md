# 0004: Depth Visibility and Build Layers

- Status: Accepted
- Date: 2026-08-09

## Context

Visual Z-depth planes and vertical dungeon floors are separate concepts but could otherwise be conflated in camera and progression rules.

## Decision

Foreground and background depth planes remain visible simultaneously. Build-layer limits refer only to vertical dungeon floors and do not count visual Z-depth planes.

## Consequences

- Rendering needs clear depth cues and occlusion handling without requiring a plane-isolation view.
- Progression bounds use the vertical floor coordinate independently from depth-plane identity.
- Whether light and attacks cross depth transitions remains unresolved.

See [Background Depth](../Design/World_Generation_and_Building.md#background-depth--z-planes).
