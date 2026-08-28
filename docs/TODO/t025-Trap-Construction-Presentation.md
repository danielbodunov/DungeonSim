# t025 — Trap Construction Presentation

## Tracking
- **ID:** t025
- **Status:** Completed
- **Milestone:** Strategic Construction
- **Depends on:** t019–t022

## Goal
Make trap construction physically readable in the dungeon by visually representing the corridor surface changes and external service-space footprint required by a placed trap.

## Problem
The trap system now has authoritative target cells, service cells, multi-cell mechanism/infrastructure footprints, compatibility validation, and reservations. However, the dungeon presentation does not yet clearly communicate that occupied service space. A large trap can reserve multiple otherwise-solid ground cells while those cells continue to look like ordinary terrain.

`t021` also introduced addressable `TileConstructionSurfaces`, but no integration layer currently applies those surface variants as a result of trap placement.

## Direction
Treat trap presentation as a derived visual consequence of authoritative `TrapAttachmentPlacement` state.

The target corridor remains traversal/topology authority. Visual-only construction surfaces may change to authored trap-compatible variants, while reserved external cells receive a distinct non-traversable service/infrastructure presentation.

```text
TrapAttachmentPlacement
        ↓
Target corridor surface
→ authored trap opening / grate / slit / hatch variant

Mechanism + infrastructure cells
→ replace ordinary ground presentation with visible service-space occupancy

Hazard cells
→ gameplay effect/preview only; do not become service reservations
```

## Requirements
- Use the SpikeWall as the representative implementation.
- Add a generic trap presentation contract rather than SpikeWall-specific grid code.
- Allow a trap definition/presentation definition to declare the visual requirement for its target surface.
- Integrate with `TileConstructionSurfaces` for visual-only target-surface swaps where appropriate.
- Show reserved mechanism/infrastructure cells as physically occupied space rather than unchanged solid ground.
- Keep service/infrastructure cells non-traversable unless another authoritative system explicitly changes topology.
- Preview the same prospective surface/service presentation that will be committed.
- Do not mutate the live dungeon merely to generate the preview.
- Removing a trap must restore the prior/default visual surface and ground/service presentation when no other authority requires it.
- Save/load and DungeonTestScenario restoration must reconstruct the same presentation from authoritative trap state rather than relying on transient scene objects.

## Target-Surface Presentation
`t021` construction surfaces should be used for changes that do not alter topology, for example:

- `Floor_Default` → `Floor_TrapOpening`
- `Ceiling_Default` → `Ceiling_TrapOpening`
- `LeftWall_Default` → `LeftWall_TrapOpening`
- `RightWall_Default` → `RightWall_TrapOpening`

Avoid creating trap-specific copies of entire tile prefabs. Surface/module composition should prevent combinatorial prefab variants.

Anything that genuinely changes an opening, traversal edge, or topology remains `RequiresTopologyResolution` and must not be silently swapped through the visual-only API.

## External Service-Space Presentation
Mechanism and infrastructure cells should visibly communicate that ordinary ground has been displaced or excavated for machinery.

The initial implementation may use authored service-cell presentation prefabs/modules rather than introducing a full new traversable tile type.

Conceptually preserve the distinction:

```text
Traversable Dungeon Cell
→ authoritative dungeon topology

Trap Service / Infrastructure Cell
→ occupied construction volume, non-traversable

Unbuilt Ground
→ normal solid terrain presentation
```

## Acceptance Criteria
- Placing a floor-mounted SpikeWall visibly changes the target corridor floor to an appropriate trap-opening presentation.
- Its primary service cell visibly contains/displays the mechanism space instead of ordinary ground.
- Additional mechanism/infrastructure footprint cells also visibly communicate their occupation.
- Multi-cell trap footprint size can be understood by looking at the dungeon without enabling debug overlays.
- Preview shows the prospective final surface/service presentation before placement.
- Preview and committed result match.
- Trap removal restores the appropriate prior/default presentation.
- Save/load restores the same presentation.
- Scenario capture/reset restores the same presentation.
- NPC traversal and trap triggering remain based on the authoritative target corridor and are unaffected by visual-only swaps.
- A topology-sensitive construction-surface variant cannot be applied through this presentation path.

## Out of Scope
- Full dungeon art pass
- Procedural mesh carving
- Voxel terrain editing
- Full service-corridor gameplay
- New trap mechanics unrelated to validating the presentation system
- Topology-changing construction transactions

## Post-Implementation Notes

- `TrapConstructionPresentation` derives both preview and committed visuals
  from `TrapAttachmentPlacement`; presentation does not own occupancy.
- A trap definition declares optional target-surface, mechanism-cell, and
  infrastructure-cell prefabs. Shared-material fallback modules keep SpikeWall
  and multi-cell footprints testable before final art is authored.
- Compatible t021 slots select the requested target variant only when the slot
  is `VisualOnly`. The previously selected/default variant is restored on
  removal; topology-sensitive slots are rejected by the variant API.
- Committed service cells temporarily hide their ordinary ground renderers.
  Existing placement reservations resolve overlap/competition before this
  override, and removal/clear restores the renderers.
- Save files and scenarios retain only authoritative trap placement state.
  Their normal placement reconstruction rebuilds all transient visuals.
- Production prefabs should be modular, cell-sized, use shared materials, avoid
  traversal colliders, and align target modules to construction-surface anchors.

## Unity Validation

Validated in Unity on 2026-08-28.

1. Enter Expansion and choose SpikeWall. Hover a valid service cell and confirm
   target-surface and service-cell presentation appear before placement.
2. Place the trap. Confirm the target treatment matches preview, ordinary ground
   is displaced in the service cell, and the target corridor still traverses and
   triggers normally.
3. Configure temporary additional mechanism/infrastructure offsets and confirm
   every reserved cell is visible without debug overlays.
4. Remove the trap from its primary service cell. Confirm the target's prior
   surface and all displaced ground presentations return.
5. Place again, save during Expansion, alter/remove it, then load. Confirm the
   same orientation, target treatment, and complete service footprint return.
6. Capture/reset a `DungeonTestScenario` containing the trap and verify the same
   reconstruction. Confirm no presentation preview objects remain after leaving
   Play mode.

## Git
Suggested branch: `feature/t025-trap-construction-presentation`
