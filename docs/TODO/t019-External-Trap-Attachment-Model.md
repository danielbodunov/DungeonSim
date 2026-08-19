# t019 — External Trap Attachment Model

## Tracking
- **ID:** t019
- **Status:** Planned
- **Milestone:** Strategic Construction
- **Depends on:** t011; preferably t018

## Goal
Change traps from arbitrary occupants of traversable dungeon cells into mechanisms that require compatible construction/service space outside the playable corridor volume.

## Start Here
- `docs/Architecture/Props_and_Traps.md`
- `docs/Design/World_Generation_and_Building.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Design Principle
A trap should feel physically installed. Spikes entering a corridor from a floor, wall, or ceiling should have a corresponding mechanism/attachment region outside that traversable space.

## Requirements
- Define generic trap attachment surfaces/regions for floor, wall, and ceiling orientations.
- A trap placement occupies/reserves external/service space rather than simply claiming the traversable cell as its mechanism location.
- Preserve a clear relationship between mechanism, affected corridor/tile, trigger area, and hazard direction.
- Placement validation must reject incompatible or unavailable attachment/service space.
- Do not make the system spike-trap-specific.
- Record implications for tile prefab construction/modularity discovered during implementation.

## Acceptance Criteria
- A spike-style trap can be authored/placed through a compatible floor/wall/ceiling attachment model.
- The mechanism exists outside the traversable dungeon volume while its hazard affects that volume.
- Placement fails when required service space is unavailable.
- Existing traversal remains based on the dungeon cell, not the external mechanism position.

## Out of Scope
- Full modular tile rebuild
- All future trap types
- Trap rotation UX polish (t020)

## Git
Suggested branch: `feature/t019-external-trap-attachments`
