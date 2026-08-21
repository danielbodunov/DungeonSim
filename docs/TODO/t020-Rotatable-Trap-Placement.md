# t020 — Rotatable Trap Placement

## Tracking
- **ID:** t020
- **Status:** Awaiting Unity Validation
- **Milestone:** Strategic Construction
- **Depends on:** t019

## Goal
Make trap orientation an explicit strategic placement decision so one trap definition can mount to compatible floor/wall/ceiling surfaces and project its hazard in the intended direction.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Design/World_Generation_and_Building.md`
- `docs/Architecture/Save_System.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Requirements
- Add preview rotation/orientation controls for trap placement.
- Validate the rotated trap against attachment surface, service space, hazard direction, and affected dungeon volume.
- Persist trap orientation through save/load and test scenarios.
- Preview should clearly communicate mechanism location and hazard direction.
- Rotation semantics should be generic enough for future directional traps.

## Acceptance Criteria
- Player can rotate a supported trap through its valid mounting orientations.
- Invalid orientations/attachments are rejected before placement.
- Saved/scenario-restored traps retain orientation.
- Trigger/damage direction matches the visual orientation.

## Out of Scope
- Arbitrary free-angle placement where discrete mounting orientations suffice
- Modular tile construction changes (t021)

## Git
Suggested branch: `feature/t020-rotatable-traps`

## Implementation Status

- Added clockwise and counterclockwise build-palette controls while a trap is
  selected. Rotation cycles through the discrete surfaces supported by the trap
  definition.
- The explicitly selected surface is passed through shared grid validation and
  placement. Invalid tile compatibility or service space is rejected without
  falling back to another orientation.
- Added a live trap preview at the external mechanism location. The mechanism
  and target-cell indicator tint green/red for valid/invalid placement, and a
  matching hazard-direction line projects from the mechanism into the affected
  corridor.
- Preview and committed instances share the same pose calculation. The stored
  `CellTrap.HazardDirection` therefore matches the visual direction into the
  target cell.
- Save format version 14 and DungeonTestScenario already persist the resolved
  attachment surface introduced by t019; t020 now commits the player's explicit
  selection through that authoritative field.
- Rotation remains four-way and surface-based; arbitrary angles and modular tile
  construction changes remain out of scope.

## Validation Notes

- `Assembly-CSharp` compiled with 0 warnings and 0 errors.
- Manual Unity validation remains pending.

## Manual Unity Validation

1. Select SpikeWall and use CCW/CW. Confirm the label cycles Floor, LeftWall,
   Ceiling, and RightWall and the mechanism moves around the target cell.
2. Confirm the hazard line always points from the external mechanism into the
   highlighted corridor cell.
3. Rotate toward a built, fixed, reserved, or unsupported service region.
   Confirm the preview turns red and clicking does not place a trap.
4. Rotate to a valid surface, place the trap, and run an adventurer through the
   target cell. Confirm damage triggers from the visually indicated direction.
5. Save/load and capture/reset a DungeonTestScenario. Confirm the trap returns
   on the selected surface with the same hazard direction.
