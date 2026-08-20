# t018 — Build Cost Foundation

## Tracking
- **ID:** t018
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t017

## Goal
Introduce meaningful construction logistics so dungeon building is no longer effectively unlimited creative placement.

## Start Here
- `docs/Design/World_Generation_and_Building.md`
- `docs/Architecture/Save_System.md`

Begin with these documents and their directly related code. Broaden investigation only when required by a demonstrated dependency.

## Requirements
- Add authoritative physical-resource costs to a deliberately small subset of build actions.
- Validate affordability before placement and spend resources only after successful placement.
- Removal/refund behavior must be explicit and deterministic.
- Surface costs and insufficient-resource feedback in the build UI.
- Keep Dread separate from ordinary physical construction costs.
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

## Implementation Status

- Added a shared `BuildCost` transaction model keyed by physical resource
  category so tiles, traps, upgrades, and future infrastructure can use the same
  affordability, spend, and refund API.
- Added authoritative balances for Construction Materials, Trap Components, and
  Arcane Components. Each category begins with a reserve of 5, and recovered
  physical-resource loot credits the matching balance without affecting Dread.
- Building a previously unbuilt dungeon cell costs exactly 1 Construction
  Material. Existing-cell width re-resolution remains free.
- Affordability is checked before tile placement. A failed or unaffordable
  placement changes neither the dungeon layout nor resource balances, while a
  successful placement spends once after the layout transaction commits.
- Successfully removing a built dungeon cell refunds exactly 1 Construction
  Material. Failed removal provides no refund.
- The expansion HUD displays all three physical-resource balances, tile palette
  entries show their Construction Material cost, and build transaction failures
  and results are surfaced in the build UI.
- Save format version 12 persists all three balances alongside constructed
  state. Older saves initialize missing balances from the five-unit reserve plus
  matching recovered physical resources. Scenario capture/restore also retains
  the balances.
- The finalized transaction, refund, Dread-separation, and save migration rules
  are documented in the building-design and save-system documents.

## Validation Notes

- `Assembly-CSharp` compiled with 0 warnings and 0 errors.
- Targeted `git diff --check` completed successfully for the t018-owned files.
- Manual Unity validation was confirmed complete by the user on 2026-08-20.

## Manual Unity Validation

Completed successfully on 2026-08-20.

## Known Limitations

- Dungeon cells are the only action with a configured physical-resource cost in
  this foundation. Trap Components and Arcane Components are available through
  the shared wallet API and UI but do not yet have build sinks.
- Final costs, reward rates, crafting, and broader economy balance remain future
  work.
