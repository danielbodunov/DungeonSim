# t020 — Service-Cell Trap Placement & Automatic Orientation

## Tracking
- **ID:** t020
- **Status:** Complete
- **Milestone:** Strategic Construction
- **Depends on:** t019

## Goal
Make external service space the primary trap-placement interaction. Derive trap
orientation from the hovered service cell and a compatible adjacent corridor,
with candidate cycling only when more than one valid target exists.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Design/World_Generation_and_Building.md`
- `docs/Architecture/Save_System.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Requirements
- Discover valid cardinally adjacent corridor targets from the hovered service
  cell and derive surface, hazard direction, and pose automatically.
- Select a deterministic default and let `R` cycle only current valid candidates.
- Validate the entire candidate transaction without mutating live dungeon state.
- Persist trap orientation through save/load and test scenarios.
- Preview must communicate mechanism location, target corridor, hazard direction,
  selected surface, and validity.
- Keep attachment semantics generic enough for future directional traps.

## Acceptance Criteria
- A hovered eligible service cell automatically previews a compatible adjacent
  target with the correct orientation.
- `R` cycles only valid adjacent targets when multiple candidates exist.
- Invalid or nonreplaceable service cells are rejected before mutation.
- Saved/scenario-restored traps retain orientation.
- Trigger/damage direction matches the visual orientation.

## Out of Scope
- Arbitrary free-angle placement where discrete mounting orientations suffice
- Full input remapping and arbitrary multi-cell mechanisms
- Modular tile construction changes (t021)

## Git
Suggested branch: `feature/t020-rotatable-traps`

## Implementation Status

- Trap placement now treats the hovered cell as the service/mechanism cell and
  asks the grid for fully valid cardinally adjacent corridor candidates.
- Surface and hazard direction derive from each service-to-target offset. The
  preferred supported surface is considered first, followed by a stable
  Floor/Ceiling/LeftWall/RightWall order for deterministic selection.
- The `R` input action cycles only the current valid candidates. Moving to a new
  service cell resets selection to the deterministic default.
- Removed manual surface state and the build palette's CCW/CW buttons. The UI
  reports the selected target and surface, and shows the `R` affordance only
  when multiple candidates exist.
- The live preview places the mechanism in the hovered service cell, highlights
  the selected target corridor, and projects a validity-colored hazard line.
- Preview and committed instances share the same pose calculation. The stored
  `CellTrap.HazardDirection` therefore matches the visual direction into the
  target cell.
- Save format version 14 and DungeonTestScenario already persist the resolved
  target/surface state introduced by t019; candidate index remains transient.
- Preview is validation-only. The current minimum foundation reserves the whole
  unbuilt cell rather than resolving a dedicated support tile. Modular service
  tile conversion remains deferred to t021.

## Validation Notes

- `Assembly-CSharp` compiled with 0 warnings and 0 errors.
- `Assembly-CSharp-Editor` compiled with 0 errors and one pre-existing unused
  `TileSocketBakerWindow.visualizeSamples` warning.
- Manual Unity validation completed successfully in Unity on 2026-08-22.

## Manual Unity Validation

Validated in Unity on 2026-08-22.

1. Hover an eligible ground cell beside exactly one compatible corridor. Confirm
   the mechanism remains in the hovered cell and automatically faces the
   highlighted corridor.
2. Click and inspect the placed trap: `ServiceCell` is the hovered cell, `Cell`
   is the corridor, and surface/hazard direction match the preview.
3. Hover a service cell beside multiple compatible corridors. Confirm `R`
   cycles only those valid targets and the preview, label, and target indicator
   update together. Click and confirm the displayed candidate is committed.
4. Test unsupported adjacency, no adjacent corridor, built/fixed cells,
   entrances, floor props/treasure, generated content, traps, and existing
   service reservations. Confirm preview is invalid and clicking causes no
   topology, content, resource, or reservation mutation.
5. Place a trap and attempt another using the same service cell. Confirm the
   second placement is rejected.
6. Run an adventurer through the target corridor. Confirm triggering remains
   corridor-cell-based and damage direction matches the visual orientation.
7. Save/load and capture/reset a DungeonTestScenario. Confirm target, service
   cell, surface, pose, and hazard direction are unchanged.
