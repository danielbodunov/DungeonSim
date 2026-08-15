# t023 — Strategic Building Vertical Slice

## Tracking
- **ID:** t023
- **Status:** Planned
- **Milestone:** Strategic Construction
- **Depends on:** t018–t022 as applicable

## Goal
Validate that dungeon construction now creates meaningful spatial and economic decisions rather than feeling like unconstrained creative tile painting.

## Core Question
Does the player need to plan dungeon growth around traversal, future traps, service space, resources, bait, and expansion intent?

## Validation Areas
- Physical construction costs create meaningful tradeoffs without excessive friction.
- External trap/service-space requirements influence corridor planning.
- Trap orientation and compatibility are understandable from previews.
- Tile modularity supports the required construction decisions without becoming arbitrary geometry editing.
- The player can intentionally reserve space for future infrastructure.
- Expanding dungeon area competes meaningfully with investing in hazards/upgrades.

## Required Scenario Comparisons
- Compact corridor with no reserved trap space versus planned trap corridor.
- Spend resources on expansion versus spend them on a hazard.
- Attempt to retrofit a trap into an unplanned area.
- Build a small dungeon deliberately around future trap/service-space needs.

## Output
Document:
- validated strategic-building behavior;
- confusing or overly restrictive rules;
- economic/logistical shortcomings;
- prefab/art requirements exposed by the slice;
- recommended next tickets.

## Out of Scope
This is primarily a validation ticket. Avoid adding unrelated construction features simply to improve the result during validation.

## Git
Suggested branch: `validation/t023-strategic-building-slice`
