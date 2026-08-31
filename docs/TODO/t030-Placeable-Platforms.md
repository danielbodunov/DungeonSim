# t030 — Placeable Platforms

## Tracking
- **ID:** t030
- **Status:** Planned
- **Milestone:** Vertical Construction / Platforms
- **Depends on:** t029

## Goal
Add a player-placeable platform as the first production use of the elevated walkable-surface model, proving construction, occupancy, persistence, presentation, and navigation-surface representation at multiple elevations.

## Requirements
- Add at least one representative placeable platform definition/prefab using the shared construction pipeline.
- Give the platform an explicit horizontal footprint, occupied vertical volume, and walkable top surface.
- Use the t029 elevated-surface/occupancy model rather than storing platform state in a separate ad hoc system.
- Validate placement against:
  - horizontal footprint conflicts;
  - vertical clearance;
  - generated build obstacles and incompatible construction;
  - required support/attachment rules.
- Keep initial support rules simple and deterministic; do not build a structural-engineering simulation.
- Provide normal valid/invalid construction preview feedback.
- Allow removal through the existing construction-removal workflow where applicable.
- Persist placed platforms through save/load and scenario capture/reset.
- Expose the top surface to navigation as an elevated walkable surface even if no valid vertical route exists until t031 is implemented.
- Prevent the platform visual mesh/collider from becoming the sole authority for whether the surface exists.

## Initial Platform Contract
The first platform may use a fixed authored elevation/height increment. The ticket does not require arbitrary-height placement.

The important proof is that a platform can occupy one or more horizontal cells while exposing walkable space at a different elevation from the ordinary floor.

## Acceptance Criteria
- The player can preview and place the representative platform only where its footprint, clearance, and support rules are valid.
- Invalid placement does not mutate dungeon state or spend resources beyond existing construction-policy behavior.
- A placed platform has a stable elevated walkable surface represented by authoritative construction/traversal data.
- A lower surface can continue to exist at the same X/Z location when vertical clearance permits.
- Platform placement cannot overlap incompatible obstacles/structures.
- Platform removal restores the relevant construction/traversal state cleanly.
- Save/load restores platform placement, orientation if applicable, elevation, and walkable-surface data.
- Scenario capture/reset reproduces the platform state.
- Existing ordinary construction remains unaffected.
- The platform does not require ladder traversal to be considered structurally valid unless an explicit reachability rule is added later.

## Out of Scope
- Ladder placement or climbing (t031)
- Production stairs/ramps
- Arbitrary/freeform platform heights
- Structural load simulation
- Automatic platform path-solvability enforcement
- Railings/fences beyond optional visual prefab content
- Final platform art set/variants

## Manual Validation
1. Preview the platform over valid and invalid locations and inspect feedback.
2. Place platforms over representative lower-floor configurations and verify vertical occupancy is correct.
3. Attempt overlap with obstacles and incompatible construction.
4. Inspect the elevated navigation/walkable surface generated for the platform.
5. Remove the platform and verify no stale occupancy/navigation state remains.
6. Save/load and scenario-reset a dungeon containing platforms.

## Post-Implementation Report
Record:
- platform definition/prefab and authored dimensions
- support/attachment rule used
- vertical occupancy and walkable-surface integration
- placement/removal integration
- save/scenario changes
- navigation representation
- requirements intentionally deferred to t031 or future vertical-construction work

## Git
Suggested implementation branch: `feature/t030-placeable-platforms`

Proceed according to `docs/AGENTS.md`.
