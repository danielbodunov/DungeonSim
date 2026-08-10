# 0010: Selection, Inspection, and Camera Focus

- Status: Accepted
- Date: 2026-08-09

## Context

NPC, trap, and spawner state needs to be understandable without relying on debug views or searching the scene hierarchy.

## Decision

Selecting an NPC, trap, or spawner opens a details inspector and smoothly focuses the camera. NPC focus continues following that NPC until selection is cleared. Static traps and spawners receive one-time focus while remaining selected.

## Consequences

- Selectable objects need a shared inspection contract and selection priority.
- The camera needs an explicit focus-session API with saved free-camera state.
- Selection must clear safely when the target is destroyed or despawned.
- Placement/removal tools take input priority over ordinary inspection clicks.

See [Selection and Inspection](../Design/Visual_and_Interaction_Design.md#selection-and-inspection) and [Camera Focus and Follow](../Design/Visual_and_Interaction_Design.md#camera-focus-and-follow).
