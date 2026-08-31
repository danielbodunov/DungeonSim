# t029 — Placeable Traversal Structure Foundation

## Tracking
- **ID:** t029
- **Status:** Planned
- **Milestone:** Vertical Construction / Traversal
- **Depends on:** Existing construction placement/occupancy model; t021/t023 construction-surface conventions

## Goal
Establish the shared construction and traversal data model needed for player-placeable elevated walkable surfaces and explicit vertical connectors without implementing platforms or ladders as one-off special cases.

This ticket prepares the system for t030 platforms and t031 ladders.

## Design Intent
Vertical construction introduces two concepts that should remain distinct:

```text
Walkable Surface
- occupies physical space
- has a walkable elevation/plane
- may overlap the same X/Z grid coordinate at a different elevation

Traversal Connection
- explicitly links two walkable surfaces/elevations
- defines how an agent may transition between them
```

Do not assume one X/Z grid coordinate can own only one walkable location once elevated construction exists.

## Requirements
- Define an authoritative representation for constructed walkable surfaces with discrete/controlled elevation.
- Allow multiple walkable surfaces to exist at the same horizontal grid coordinate when their vertical occupancy does not conflict.
- Distinguish physical/volumetric occupancy from the walkable surface exposed by a structure.
- Define an explicit traversal-connection representation capable of linking a lower and upper walkable surface.
- Extend construction validation so future traversal structures can validate:
  - horizontal footprint;
  - vertical clearance;
  - support/attachment requirements;
  - conflicts with existing structures/obstacles.
- Keep the model compatible with save/load and scenario reset/capture.
- Keep navigation integration explicit: elevated surfaces and vertical links must be representable without flattening them back into a single 2D cell state.
- Provide enough preview/debug information to inspect elevation, occupied volume, walkable surface, and connection endpoints during later platform/ladder work.
- Extend existing construction/grid owners where appropriate rather than building a parallel vertical-construction state system.

## Initial Scope Boundary
This ticket should establish contracts and minimal validation infrastructure, not production traversal content.

A small debug/test representation may be used to prove:
- two walkable elevations can share an X/Z location;
- their occupied volumes remain distinguishable;
- a future vertical connector can identify a valid lower and upper endpoint.

## Acceptance Criteria
- The construction data model can represent a walkable surface above another horizontal grid location without overwriting the lower surface's state.
- Physical occupancy/clearance can be queried separately from walkability.
- A traversal connection can reference two valid walkable endpoints at different elevations.
- Placement validation has a clear extension point for support, attachment, and vertical-clearance rules.
- Save/load representation can preserve elevated surface and connection data without collapsing it to a single 2D cell value.
- Existing ordinary floor/tile construction continues to behave as before.
- Existing pathfinding/navigation behavior remains unchanged until elevated content actually exposes new nodes/connections.
- Debug/manual inspection can distinguish lower surface, upper surface, occupied volume, and connection endpoints.

## Out of Scope
- Production platform placement (t030)
- Production ladder placement/climbing (t031)
- Stairs
- Elevators
- Freeform/non-grid construction
- Arbitrary continuous-height building
- Final climbing animation
- Broad NPC pathfinding rewrite unrelated to representing elevated nodes/links

## Manual Validation
1. Create a minimal test arrangement with lower and upper walkable surfaces sharing horizontal grid space.
2. Verify neither surface overwrites the other's authoritative state.
3. Verify vertical-clearance/occupancy queries distinguish valid and conflicting arrangements.
4. Create a test traversal connection between two elevations and inspect both endpoints.
5. Save/load or scenario-reset the test representation and verify elevation/connection data is preserved.
6. Confirm existing normal construction and navigation remain unchanged.

## Post-Implementation Report
Record:
- authoritative elevated-surface representation
- vertical occupancy/clearance representation
- traversal-connection representation
- construction validation extension points
- save/scenario serialization changes
- navigation hooks introduced
- debug/visualization support
- requirements deferred to t030/t031

## Git
Suggested implementation branch: `feature/t029-traversal-structure-foundation`

Proceed according to `docs/AGENTS.md`.
