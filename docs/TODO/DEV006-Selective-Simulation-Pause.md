# DEV006 — Selective Simulation Pause

## Tracking

- **ID:** DEV006
- **Status:** Complete
- **Milestone:** Developer Tooling — Observation & Control
- **Depends on:** DEV005

## Goal

Replace the debug behavior that pauses the entire Unity environment with a selective dungeon-simulation pause. NPCs, traps, and other dynamic gameplay entities should stop progressing while camera movement, Editor/debug UI, selection, and static-world inspection remain active.

## Desired Separation

### Presentation / Observation

Remains active while simulation is paused:

- Gameplay camera
- NPC selection/highlighting
- Runtime debug harness
- Editor tooling
- Static dungeon geometry
- Read-only state inspection

### Gameplay Simulation

Stops progressing while paused:

- NPC AI and traversal
- NPC investigations/tasks
- Traps and trap timers
- Dynamic gameplay entities/interactables
- Other systems explicitly participating in dungeon simulation

## Requirements

- Introduce a central authoritative simulation pause state rather than relying on Unity's global Editor pause.
- Do not use `Time.timeScale = 0` as the long-term implementation of this feature.
- Establish a clear convention/API for systems that participate in dungeon simulation.
- Update current NPC traversal/AI and trap behavior to respect simulation pause.
- Identify other currently active dynamic gameplay entities that should participate and include them where necessary for a coherent pause.
- Camera and debug tooling must remain usable while paused.
- NPC selection and inspection must remain available while paused.
- Resume should continue from the existing simulation state without resetting routes, timers, stamina, trap state, or NPC goals.
- Debug actions that intentionally mutate state may remain available while paused where safe and useful.
- Future dynamic gameplay systems should have an obvious way to participate in simulation pause.

## Acceptance Criteria

- Activating the debug simulation pause stops NPC movement and AI progression.
- Investigation/task timers do not progress while paused.
- Trap timers/behavior do not progress while paused.
- Relevant dynamic gameplay entities stop progressing consistently.
- Camera pan/zoom/focus remains functional.
- NPCs remain selectable and inspectable in the runtime debug harness.
- Appropriate debug state-manipulation actions remain usable.
- Resuming continues the prior NPC/trap state rather than restarting it.
- Static dungeon content is unaffected.
- The implementation does not depend on globally pausing the Unity Editor.

## Architecture Direction

Introduce a small production-side simulation-state abstraction, for example a `DungeonSimulationState` or equivalent authoritative service exposing whether gameplay simulation is paused.

Do not immediately rewrite every project `Update()` around a custom clock. Start with systems that genuinely participate in simulation today and establish the convention future systems should follow.

An interface such as `ISimulationPausable`, a shared state query, or another bounded pattern is acceptable if it preserves clear ownership and does not require debug code to know every entity individually.

## Future Direction — Out of Scope

The same architecture may later support simulation-speed controls such as:

- 0.25x
- 0.5x
- 1x
- 2x
- single-step/tick advancement

Do **not** implement simulation-speed controls in DEV006. Record any architectural considerations discovered during implementation as follow-up work.

## Out of Scope

- Global Unity Editor pause replacement outside DungeonSim
- Full deterministic simulation clock
- Simulation-speed controls
- Turn-based mode
- Gameplay tactical-pause UI

## Manual Validation

1. Run an NPC through a dungeon containing traps and treasure.
2. Activate simulation pause while the NPC is moving.
3. Verify movement/AI stops while camera and debug selection remain usable.
4. Pause during an investigation and verify its remaining time does not progress.
5. Pause while a trap/timer is active and verify its state does not progress.
6. Inspect and manipulate an NPC through the debug harness while paused.
7. Resume and verify all simulation systems continue from their previous state.
8. Repeat pause/resume several times to check for duplicated coroutines, skipped state, or timer jumps.

## Implementation Status

- Added `DungeonSimulationState` as the production-side authority for selective pause state, pause notifications, pause-aware delta time, and simulation-only coroutine waits.
- Preserved `GameplayLoopController.IsPaused`, `SetPaused()`, and `TogglePause()` as compatibility-facing APIs while routing their state through the new authority.
- Selective pause no longer writes `Time.timeScale = 0`; existing gameplay-speed behavior remains separate and unchanged.
- Exploration duration and adventurer spawning stop through the gameplay loop's existing pause guard.
- NPC route movement, ladder movement, return movement, investigations, general task stamina drain, and fall-recovery delay now preserve their exact in-progress state while paused.
- Spike-wall trigger phases, damage delay, reset duration, cooldown, readiness, and animator playback now pause and resume without restarting the trap cycle.
- The NPC runtime debug harness exposes the current simulation state and pause/resume controls while keeping selection, camera focus, inspection, and debug mutation actions available.
- The existing runtime debug-panel button is labeled Pause/Resume Simulation to make its selective scope explicit.
- The current dynamic-system audit identified the gameplay loop, NPC traversal agents, and spike-wall traps as simulation participants. Lighting, camera, UI feedback, placement previews, and static dungeon content remain active presentation/tooling systems.

## Validation Notes

- Runtime C# compilation passes with zero warnings and zero errors.
- Editor C# compilation passes with zero errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` unused-field warning.
- Static inspection confirms no remaining `Time.timeScale = 0` assignment and no direct scaled-time waits in current NPC or trap simulation paths.
- Manual Unity validation completed successfully on 2026-08-14.

## Known Limitations

- Participation is explicit: future dynamic gameplay systems must query `DungeonSimulationState.IsPaused`, consume `DungeonSimulationState.DeltaTime`, or use its simulation wait helpers.
- Existing X1/X2/X3 gameplay-speed controls still use Unity time scale. General speed controls and single-step simulation remain out of scope.

## Git

Suggested branch: `dev/DEV006-selective-simulation-pause`

Active branch: `tool/dev006-selective-simulation-pause` (`dev/` is unavailable because a branch named `dev` occupies that Git ref namespace.)

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
