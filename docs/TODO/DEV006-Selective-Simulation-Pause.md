# DEV006 — Selective Simulation Pause

## Tracking

- **ID:** DEV006
- **Status:** Planned
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

## Git

Suggested branch: `dev/DEV006-selective-simulation-pause`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
