# DEV007 — Scenario Default Entrance Compatibility

## Tracking
- **ID:** DEV007
- **Status:** Ready
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

## Git
Suggested branch: `tool/dev007-scenario-default-entrance`

Proceed according to `docs/AGENTS.md`.
