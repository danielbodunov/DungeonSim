# DEV002 — Reusable Dungeon Test Scenarios

## Tracking
- **ID:** DEV002
- **Status:** Planned
- **Depends on:** DEV001
- **Blocks:** DEV003; t007

## Goal
Allow useful dungeon test layouts to be captured once and recreated reliably from Editor tooling instead of rebuilding them manually for every test.

## Requirements
- Introduce a reusable scenario asset containing test setup data.
- Capture tiles, rotations, entrance, traps, treasure, and other supported authored state needed to recreate the test.
- Add Editor actions to capture the current dungeon, load a scenario, reset it, save a new scenario, and intentionally update an existing scenario.
- Reconstruct scenarios through normal production placement/game APIs wherever practical.
- Store a scenario name, description, and intended test purpose.

## Acceptance Criteria
- A manually constructed dungeon can be captured as a scenario asset.
- Loading recreates its layout and supported content consistently.
- Reset restores the authored initial state.
- Treasure, traps, and entrance survive capture/reload.
- Repeated tests no longer require rebuilding the layout manually.

## Constraints
Do not create a parallel dungeon implementation or automated test runner in this ticket.

## Git
Suggested branch: `dev/DEV002-test-scenarios`
