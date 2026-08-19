# t020 — Rotatable Trap Placement

## Tracking
- **ID:** t020
- **Status:** Planned
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
