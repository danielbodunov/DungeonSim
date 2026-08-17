# Gameplay Loop Architecture

## Purpose

`GameplayLoopController` is the high-level owner of the prototype dungeon loop. It coordinates phase state, simulation speed, dungeon progression, Dread, adventurer spawning/lifecycle, and persistent scenario state.

## Primary collaborators

- `TilePlacement` — building input is enabled/disabled based on phase.
- `TileGridGenerator` — current dungeon state and placement context.
- `NPCTraversal` — adventurer exploration runtime.
- `GameplayLoopUI` — presentation and player controls.
- `GameSaveManager` — persistence coordinator.

## Phase model

Current phases:

- `Expansion` — player building/editing state.
- `Exploring` — adventurer simulation state.

The phase boundary matters because multiple systems assume the dungeon topology is stable while an adventurer run is active.

```mermaid
stateDiagram-v2
    [*] --> Expansion
    Expansion --> Exploring: open/start run
    Exploring --> Expansion: run resolves / return to building
```

## Owned persistent state

The gameplay loop currently owns or coordinates persistent scenario concepts including:

- dungeon open count;
- Dread;
- dungeon level;
- selected simulation speed;
- adventurer roster;
- Dread harvest/spend history;
- expedition outcomes;
- recovered loot and recovery history.

When introducing a new progression variable, decide whether it belongs here, on a content object, or in a dedicated subsystem. Do not use `GameplayLoopController` as a generic global state bucket.

## Simulation-time boundary

Gameplay systems that need pausing/speed scaling should prefer the existing `DungeonSimulationState` abstraction rather than directly using `Time.deltaTime` when the behavior is meant to follow dungeon simulation speed.

## Extension guidance

Good candidates for feature-local scripts:

- a new adventurer stat or behavior: `NPCCharacter` / NPC action layer;
- a new trap effect: trap subclass / `NPCActionResolver`;
- a new persistent economy: dedicated model/controller that the gameplay loop coordinates;
- UI-only presentation: `GameplayLoopUI` or a focused UI component.

Edit `GameplayLoopController` when the feature changes the phase lifecycle, progression ownership, or the contract between building and exploration.

## Cross-system checks

If changing phase transitions or adventurer-run boundaries, verify:

1. building input cannot mutate topology at an unsafe time;
2. NPC traversal starts/stops from a valid route graph;
3. procedural structures are stable while NPCs use them;
4. pause/simulation speed behavior remains consistent;
5. save eligibility still matches the desired safe state.

## Related docs

- [`NPC_Runtime.md`](NPC_Runtime.md)
- [`Save_System.md`](Save_System.md)
- [`../Design/Core_Game_Direction.md`](../Design/Core_Game_Direction.md)
- [`../Design/NPC_Behavior.md`](../Design/NPC_Behavior.md)
