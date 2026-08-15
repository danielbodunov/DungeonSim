# t018 — Build Cost Foundation

## Tracking
- **ID:** t018
- **Status:** Planned
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t017

## Goal
Introduce meaningful construction logistics so dungeon building is no longer effectively unlimited creative placement.

## Requirements
- Add authoritative physical-resource costs to a deliberately small subset of build actions.
- Validate affordability before placement and spend resources only after successful placement.
- Removal/refund behavior must be explicit and deterministic.
- Surface costs and insufficient-resource feedback in the build UI.
- Keep Aura separate from ordinary physical construction costs.
- Design the cost API so tiles, traps, upgrades, and future infrastructure can share it.

## Acceptance Criteria
- At least one dungeon construction action consumes physical resources.
- Insufficient resources prevent placement without mutation/spending.
- Successful placement spends the configured cost exactly once.
- Removal/refund follows documented rules.
- Save/load preserves resource balance and constructed state.

## Out of Scope
- Final economy balance
- Full crafting system
- Trap external-space placement (t019+)

## Git
Suggested branch: `feature/t018-build-cost-foundation`
