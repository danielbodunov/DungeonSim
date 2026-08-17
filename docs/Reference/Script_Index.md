# Script Index

This is a developer-facing catalog of `Assets/Scripts`. It is organized by responsibility rather than alphabetically so related code is easier to navigate.

> This is a curated architectural index, not a compiler-generated dependency graph. Use IDE Find References for exact call sites.

## Dungeon topology, placement, and tile authoring

### `TileGridGenerator`
**Role:** Authoritative dungeon topology / placement integration hub.  
**Reads:** adjacency/profile data, placement requests, content compatibility.  
**Owns:** built-cell layout, width/connection intent, local resolution transactions, placed-content coordination.  
**Collaborates with:** `TileAdjacencyDatabase`, `TileSocketProfile`, `PropGenerator`, `CellTrap`, `FloorProp`, `DungeonEntrance`, POIs.  
**Referenced by:** `TilePlacement`, `GameplayLoopController`, `NPCTraversal`, `GameSaveManager`, `DungeonLightingManager`.  
**Risk:** High blast radius.

### `TilePlacement`
**Role:** Player-facing dungeon placement/editing coordinator.  
**Reads:** `InputManager`, `ObjectsDatabaseSO`, grid/adjacency data.  
**Writes through:** `TileGridGenerator`.  
**Modes include:** regular placement, trap removal, entrance removal, edge editing.  
**Risk:** Medium; input/UX changes can alter building semantics.

### `TileAdjacencyDatabase`
**Role:** Authored/baked compatibility database used by the tile solver.  
**Risk:** Data-model changes affect resolution broadly.

### `TileSocketProfile`
**Role:** One rotated tile candidate/profile.  
**Contains:** source prefab, category, rotation, cardinal edge hashes/matches, baked prop sockets.

### `TilePlaceholder`
**Role:** Runtime/editor representation associated with unresolved/placed tile cells.

### `TilePortalGizmo`, `TileSocketGizmo`
**Role:** Authoring/debug visualization for tile connectivity/socket data.

### `PortalEdgeAnalyzer`
**Role:** Utility for analyzing tile/portal edge information.

### `ObjectsDatabaseSO`
**Role:** Build-palette data (`DungeonTile`, `Trap`, `Entrance`, `FloorProp`) with stable object ID, size, prefab, and placement type.

### `InputManager`
**Role:** Converts player input into click/right-click/exit style events consumed by placement and interaction systems.

### `GridVisualFollower`
**Role:** Grid-related visual helper.

## Props, traversal structures, traps, and interactions

### `PropGenerator`
**Role:** Procedural prop/structure coordinator.  
**Reads:** `PropCatalog` and tile/socket state from `TileGridGenerator`.  
**Owns:** generated occupancy, spawned generated props, structure runs, generation version/seed.  
**Consumed by:** `NPCTraversal` for generated traversal structures.  
**Risk:** Medium-high when generation semantics or traversal structures change.

### `PropCatalog`
**Role:** ScriptableObject catalog of procedural structure definitions, lane variants, role bundles, occupancy, and generation mode.

### `PropSocketAuthoring`, `PropSocketTypes`
**Role:** Authored socket data/components used to place/generate structured content.

### `CellTrap`
**Role:** Base one-cell trap contract.  
**Owns:** bound grid/cell identity.  
**Entry point:** `OnNpcEntered(NPCCharacter)`.

### `SpikeWallTrap`
**Role:** Concrete cell-trap example.  
**Owns:** damage/difficulty, dodge settings, cooldown, animation cycle.  
**Delegates:** NPC outcome to `NPCActionResolver`.

### `FloorProp`
**Role:** Ordinary placed floor content.  
**Extension points:** compatibility validation and resolved save state.  
**Also:** binds child POIs to owning grid/cell.

### `DungeonEntrance`
**Role:** Authoritative placed dungeon entrance and entry location.

### `DungeonPointOfInterest`
**Role:** NPC-visible/interactable location bound to dungeon context.

### `TreasureProp`
**Role:** Treasure-related placed content.

## NPC runtime and outcomes

### `NPCTraversal`
**Role:** Runtime navigation/exploration integration hub.  
**Reads:** tile openings from `TileGridGenerator`, generated traversal from `PropGenerator`.  
**Owns:** route graph, active agents/traversal state, navigation counts, recoverable loot/death/escape runtime outcomes.  
**Risk:** High blast radius.

### `NPCCharacter`
**Role:** Adventurer-owned runtime state and character behavior surface.

### `NPCActionResolver`
**Role:** Shared gameplay resolution for NPC actions/challenges such as traps.

### `NPCTraversalDebug`
**Role:** Runtime navigation debugging/visualization.

### `NPCActionFeedbackUI`, `NPCStatusBars`
**Role:** NPC interaction/status presentation.

### `NPCCarriedLootVisual`, `CarriedDungeonTreasure`
**Role:** Carried-loot state/presentation.

### `RecoverableDungeonLoot`, `RecoverableLootWorldDrop`, `LootBundleVisualFactory`
**Role:** Recoverable dungeon loot state and world representation.

### `AdventurerNameGenerator`, `AdventurerPhysicalResource`
**Role:** Adventurer-specific supporting data/behavior.

### `ExpeditionOutcome`
**Role:** Exploration outcome data/model.

## Gameplay loop and progression

### `GameplayLoopController`
**Role:** High-level gameplay integration hub.  
**Owns/co-ordinates:** Expansion/Exploring phases, simulation speed, dungeon rating/progression, Dread, adventurer lifecycle and persistent scenario records.  
**Collaborators:** placement, grid, NPC traversal, UI, save manager.  
**Risk:** High blast radius.

### `GameplayLoopUI`
**Role:** Presentation and controls for the gameplay loop.

### `DungeonSimulationState`
**Role:** Shared simulation pause/time-scale abstraction for systems that should follow dungeon simulation time.

### `DreadHarvest`, `DreadSpend`
**Role:** Dread economy records/state.

### `DungeonTestScenario`
**Role:** Prototype/test scenario orchestration.

## Persistence

### `GameSaveManager`
**Role:** Cross-system persistence coordinator.  
**Participants:** gameplay loop, grid, placement, NPC traversal.  
**Risk:** High blast radius when adding persistent state.

### `DungeonSaveData`
**Role:** Serializable save-data model.

## Lighting

### `DungeonLightingManager`
**Role:** Chunked world-space dungeon light field.  
**Reads:** grid topology and active light sources.  
**Events:** rebuilds from `TileGridGenerator.LayoutChanged` and `DungeonLightSource.SourcesChanged`.

### `DungeonLightSource`
**Role:** Static/dynamic light emitter registered with dungeon lighting.

### `DungeonLightReceiver`
**Role:** Consumer/helper for dungeon lighting output.

## Camera / presentation utilities

### `CameraMovement`
**Role:** Camera navigation.

## Editor tooling

Files under `Assets/Scripts/Editor/` are editor-only authoring/debug utilities and should not become runtime dependencies.

- `AdventurerPhysicalResourceTests`
- `DungeonTestScenarioWindow`
- `GameplayInputOwnershipEditorBridge`
- `ModularPrefabGeneratorWindow`
- `NPCRuntimeDebugHarnessWindow`
- `PropCatalogEditor`
- `PropSocketAuthoringEditor`
- `PropSocketAuthoringWindow`
- `TileSocketBakerWindow`

## Recommended navigation pattern

When manually implementing a feature:

1. Find the leaf/content script here.
2. Open the corresponding page under [`../HowTo/`](../HowTo/README.md).
3. Only then inspect an integration hub if the feature cannot be expressed through existing data/hooks.
4. Check [`../Architecture/Save_System.md`](../Architecture/Save_System.md) whenever new authoritative state is introduced.
