# t022 — Trap Space & Compatibility Validation

## Tracking
- **ID:** t022
- **Status:** Complete
- **Milestone:** Strategic Construction
- **Depends on:** t019; t020; t021 as required

## Goal
Make strategic trap planning reliable by validating the complete physical space a trap requires, not merely whether its target corridor cell is available.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Architecture/Save_System.md`
- `docs/Design/World_Generation_and_Building.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Requirements
- Formalize trap footprint/service-space reservations.
- Detect conflicts with neighboring dungeon construction, other trap mechanisms, incompatible surfaces, and reserved infrastructure.
- Keep hazard volume and mechanism/service footprint distinct.
- Preview all relevant conflicts before committing placement.
- Ensure save/load/scenario reconstruction uses the same compatibility rules.

## Acceptance Criteria
- Overlapping trap mechanisms are rejected.
- A corridor can remain traversable while its external service space is reserved by a trap.
- Future construction that would invalidate occupied service space is rejected or handled by an explicit removal/replacement flow.
- Preview and committed placement use shared validation rules.

## Out of Scope
- Final resource costs
- Trap effectiveness balancing
- Automated optimal-layout suggestions

## Git
Suggested branch: `feature/t022-trap-space-validation`

## Implementation Status

- `TrapAttachmentDefinition` now authors attachment-local additional mechanism,
  infrastructure, and hazard offsets. Existing prefabs retain their legacy
  one-service/one-target footprint when arrays are empty.
- `TrapAttachmentPlacement` resolves and carries distinct mechanism,
  infrastructure, and hazard collections. Only mechanism and infrastructure
  cells participate in service reservations.
- Shared grid validation rejects out-of-bounds, built, fixed, generated-content,
  authoritative-content, duplicate, overlapping, and hazard/service-conflicting
  footprints before placement.
- Preview and commit resolve through the same placement result. Additional
  footprint and hazard cells receive preview indicators. Trap cell indicators
  share the main cell indicator's Z plane, while the hazard line uses the target
  corridor cell center's Z plane.
- Trap removal is service-cell-first: selecting the primary external service
  cell removes its target-owned trap record, while selecting the target corridor
  does not remove it.
- Construction into a target or any service-footprint cell is rejected until
  the trap is explicitly removed. Tile and connection re-resolution also reject
  assignments that invalidate the occupied attachment surface.
- Save loading prevalidates the complete trap set against the prospective saved
  tile layout before mutation. Dungeon scenarios accumulate the same complete
  footprint reservations during authored-content validation.
- Added editor coverage for rotated footprint offsets, legacy default behavior,
  and separation of reserved cells from hazard volume.

## Validation Notes

- `Assembly-CSharp` and `Assembly-CSharp-Editor` compile successfully; the editor
  build retains the pre-existing unused
  `TileSocketBakerWindow.visualizeSamples` warning.
- Manual Unity validation was confirmed complete by the user on 2026-08-23

## Manual Unity Validation

1. Place a SpikeWall from an eligible service cell. Confirm the corridor remains
   traversable and the trap triggers from its target cell.
2. Attempt a second trap using the same service cell. Confirm preview is invalid
   and placement creates no second trap.
3. Temporarily author an additional mechanism and infrastructure offset on a
   copied trap prefab. Confirm all reserved cells are previewed and overlap with
   construction, fixed ground, generated content, or another footprint fails.
4. Author an additional hazard offset toward a built corridor. Confirm it is
   previewed but does not reserve the corridor as service space. Confirm an
   unbuilt hazard cell makes placement invalid.
5. Attempt to build in a trap footprint or replace its target tile. Confirm the
   edit is rejected with an explicit-removal message; remove the trap and retry.
6. Toggle a nearby connection or perform a local tile edit that would select a
   profile incompatible with the trap surface. Confirm the edit is rejected and
   prior topology remains unchanged.
7. Save/load and capture/reset a scenario containing adjacent traps. Confirm all
   footprints reconstruct without overlap and invalid authored overlap is
   rejected before partial restoration.
8. Run `TrapAttachmentTests` in EditMode.
9. Activate trap removal and click the target corridor; confirm the trap remains.
   Click its primary service cell and confirm the trap is removed.
