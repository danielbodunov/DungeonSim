# DungeonSim Architecture Atlas

This section explains how the implemented Unity systems fit together. It complements the documents in [`../Design/`](../Design/README.md): Design describes intended behavior and rationale; Architecture describes the current implementation and the boundaries a developer should respect when changing it.

## Start here

- [`System_Map.md`](System_Map.md) — high-level dependency and data-flow diagrams.
- [`Dungeon_Generation.md`](Dungeon_Generation.md) — grid topology, tile resolution, placement validation, and building.
- [`Gameplay_Loop.md`](Gameplay_Loop.md) — phase ownership, progression, Dread, adventurer lifecycle, and UI coordination.
- [`NPC_Runtime.md`](NPC_Runtime.md) — route graph construction, movement, actions, traps, POIs, and loot outcomes.
- [`Props_and_Traps.md`](Props_and_Traps.md) — authored props, procedural structures, floor props, entrances, and cell traps.
- [`Save_System.md`](Save_System.md) — what is authoritative, what is reconstructed, and which systems participate in persistence.
- [`Lighting.md`](Lighting.md) — grid-driven dungeon light field and source/receiver relationships.

## Integration hubs

These scripts have a larger-than-average blast radius and should be treated as integration points rather than feature-local scripts.

| Script | Primary responsibility | Common dependents / collaborators |
|---|---|---|
| `TileGridGenerator` | Authoritative dungeon layout, local tile solving, placed-content validation, topology events | `TilePlacement`, `PropGenerator`, `NPCTraversal`, `DungeonLightingManager`, `GameSaveManager` |
| `GameplayLoopController` | Expansion/exploration phase, simulation speed, progression, Dread, adventurer lifecycle | `GameplayLoopUI`, `TilePlacement`, `TileGridGenerator`, `NPCTraversal`, save system |
| `NPCTraversal` | Runtime navigation graph, adventurer movement/state, exploration traversal, recoverable loot outcomes | `TileGridGenerator`, `PropGenerator`, `NPCCharacter`, `NPCActionResolver`, save system |
| `GameSaveManager` | Coordinates capture/restore across persistent systems | gameplay loop, grid, placement, NPC traversal |
| `PropGenerator` | Procedural prop/structure generation and generated-cell occupancy | grid, prop catalog, traversal |

Before editing one of these, open the relevant architecture page and identify whether the desired behavior can be implemented in a leaf class or data asset instead.

## Documentation rule

When a code change materially changes one of the flows documented here, update the matching Architecture or HowTo page in the same ticket. Avoid copying game-design prose into Architecture; link to the source Design document instead.

## Related documentation

- [`../HowTo/README.md`](../HowTo/README.md) — task-oriented guides for making common changes manually.
- [`../Reference/Script_Index.md`](../Reference/Script_Index.md) — script catalog by subsystem.
- [`../Reference/Data_Assets.md`](../Reference/Data_Assets.md) — important ScriptableObjects and authored data.
- [`../Design/World_Generation_and_Building.md`](../Design/World_Generation_and_Building.md) — intended building/world-generation direction.
- [`../Design/NPC_Behavior.md`](../Design/NPC_Behavior.md) — intended NPC behavior.
- [`../Design/Capability_Gated_Traversal.md`](../Design/Capability_Gated_Traversal.md) — traversal capability design.
