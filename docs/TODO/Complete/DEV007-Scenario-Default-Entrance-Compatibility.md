# DEV007 — Scenario Default Entrance Compatibility

## Tracking
- **ID:** DEV007
- **Status:** Complete
- **Milestone:** Developer Tooling — Scenario Reliability
- **Blocks:** DEV008

## Goal
Allow dungeon test scenarios that rely on the normal/default entrance to capture and load successfully without requiring a separately placed manual entrance.

## Requirements
- Distinguish an effective valid dungeon entrance from a manually authored entrance record.
- Scenario validation must accept layouts whose normal/default entrance contract is valid even when no manual entrance was placed.
- Scenario apply/load must preserve or recreate the effective entrance through the same production entrance logic used by gameplay.
- Manual entrance scenarios must continue to work.
- Do not create a second entrance merely because the default entrance already exists.
- Preserve prevalidation guarantees: an invalid scenario must not partially mutate the live dungeon.

## Acceptance Criteria
- Capture/load succeeds for a scenario that uses only the default entrance.
- Capture/load succeeds for a scenario with a manual entrance.
- The loaded scenario contains exactly one effective entrance unless the scenario intentionally represents no entrance.
- NPC spawn/return behavior uses the restored entrance normally.
- Invalid entrance data still fails preflight without mutating the current dungeon.

## Out of Scope
- Changing gameplay entrance semantics
- Multiple entrances
- Entrance replacement rules (DEV008)

## Manual Validation
Capture and reload one default-entrance scenario and one manual-entrance scenario; verify NPC spawn/return and scenario reset in both.

### Focused Unity Steps

1. Enter Play Mode with a built layout using only the normal gameplay/default entrance. Open **Tools > Dungeon Test Scenarios** and capture it.
2. Verify the pending summary reports entrance mode `Default`, save it as a new scenario, mutate the dungeon, then load and reset the scenario.
3. After each load/reset, verify exactly one effective entrance exists, an NPC spawns through it, and **Force Return Home** returns through the same entrance.
4. Place a manual entrance, capture a second scenario, and verify its pending mode is `Manual`.
5. Mutate the dungeon, load/reset the manual scenario, and verify exactly one manual entrance is restored and NPC spawn/return uses it.
6. Create invalid entrance data in a disposable scenario copy (for example a missing manual record, a default entrance cell outside the built layout, or a manual entrance combined with a built-in layout entrance). Verify load is rejected before the current dungeon changes.

## Implementation Status

- Scenario assets now distinguish `Default`, `Manual`, and intentional `None` entrance modes. `Default` is serialized as zero so existing scenario assets without a manual entrance migrate compatibly without being rewritten.
- Manual compatibility detection requires actual prefab identity rather than a non-null record because Unity serializes a null inline entrance class as an empty `(0,0)` object. Empty placeholders therefore remain governed by the explicit `Default` mode.
- New default-entrance captures retain the effective entrance cell separately from the manual entrance record. This gives preflight validation an occupancy reservation without pretending the fallback was manually authored.
- Default entrance preflight validates the captured cell, built layout, authored-content conflicts, built-in entrance-marker ambiguity, fallback prefab resolution, and the presence of the active production `NPCTraversal` assigned to that grid before any restore API runs. The traversal may live on a separate manager GameObject.
- Manual scenarios continue through the existing entrance-prefab/socket placement validator and now reject a layout that would combine the manual entrance with built-in entrance content.
- Apply still restores tiles and authored content through the existing production APIs, regenerates props, and asks `NPCTraversal.EnsureDefaultEntrance()` to preserve or recreate the normal entrance.
- `EnsureDefaultEntrance()` now recognizes an already effective manual or built-in entrance before creating a fallback, while retaining the existing fallback-repositioning behavior after layout changes.
- The scenario window reports both selected and pending-capture entrance modes explicitly.

## Validation Notes

- Runtime assembly compiles with zero warnings and zero errors.
- Editor assembly compiles with zero errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` unused-field warning.
- Static mutation-boundary review confirms all new entrance-mode, cell, prefab, topology, and occupancy rejection paths run before `RestoreTileLayout()`.
- Manual Unity validation completed successfully on 2026-08-15.

## Known Limitations

- An intentional no-entrance scenario is accepted only when it has no built dungeon cells. With built cells, normal gameplay owns and creates a default entrance; suppressing that behavior would change gameplay entrance semantics.
- Legacy entrance-less scenarios have no captured default cell. They use the compatible `Default` mode and receive general layout/prefab preflight, while newly captured scenarios also validate and reserve the exact effective cell.

## Git
Suggested branch: `tool/dev007-scenario-default-entrance`

Active branch: `tool/dev007-scenario-default-entrance`

Proceed according to `docs/AGENTS.md`.
