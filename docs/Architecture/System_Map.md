# System Map

This page is the fastest way to answer **“what else does this script touch?”** before making a manual change.

The arrows below show architectural dependency or data flow, not necessarily direct field references in every case.

## Whole-project overview

```mermaid
flowchart LR
    InputManager --> TilePlacement
    TilePlacement --> TileGridGenerator
    TilePlacement --> ObjectsDatabaseSO
    TilePlacement --> TileAdjacencyDatabase

    TileGridGenerator --> TileAdjacencyDatabase
    TileGridGenerator --> TileSocketProfile
    TileGridGenerator <--> PropGenerator
    PropGenerator --> PropCatalog

    TileGridGenerator --> CellTrap
    TileGridGenerator --> FloorProp
    TileGridGenerator --> DungeonEntrance
    TileGridGenerator --> DungeonPointOfInterest

    GameplayLoopController --> TilePlacement
    GameplayLoopController --> TileGridGenerator
    GameplayLoopController --> NPCTraversal

    NPCTraversal --> TileGridGenerator
    NPCTraversal --> PropGenerator
    NPCTraversal --> NPCCharacter
    NPCTraversal --> NPCActionResolver

    DungeonLightingManager --> TileGridGenerator
    DungeonLightSource --> DungeonLightingManager

    GameSaveManager --> GameplayLoopController
    GameSaveManager --> TileGridGenerator
    GameSaveManager --> TilePlacement
    GameSaveManager --> NPCTraversal

    classDef hub fill:#ffe6a7,stroke:#8a5a00,stroke-width:2px;
    class TileGridGenerator,GameplayLoopController,NPCTraversal,GameSaveManager hub;
```

## Dungeon building and generation

```mermaid
flowchart LR
    InputManager -->|click / cancel / mode input| TilePlacement
    ObjectsDatabaseSO -->|placeable definitions| TilePlacement
    TilePlacement -->|paint cells / toggle edges / place content| TileGridGenerator
    TileAdjacencyDatabase -->|candidate compatibility| TileGridGenerator
    TileSocketProfile -->|edge hashes + baked sockets| TileGridGenerator
    TileGridGenerator -->|layout changed| PropGenerator
    PropCatalog -->|procedural definitions| PropGenerator
    TileGridGenerator -->|placed content| CellTrap
    TileGridGenerator --> FloorProp
    TileGridGenerator --> DungeonEntrance
```

### Key ownership rule

`TileGridGenerator` owns the authoritative built-cell layout and placement validation context. Tile art/profile selection is a resolved representation of that state, not the only source of truth.

## NPC runtime

```mermaid
flowchart LR
    GameplayLoopController -->|phase / adventurer lifecycle| NPCTraversal
    TileGridGenerator -->|openings + current layout| NPCTraversal
    PropGenerator -->|generated ladders / structures| NPCTraversal
    NPCTraversal -->|moves and updates| NPCCharacter
    NPCTraversal -->|encounters / traps / actions| NPCActionResolver
    CellTrap -->|OnNpcEntered| NPCCharacter
    DungeonPointOfInterest -->|interaction targets| NPCTraversal
    RecoverableDungeonLoot --> NPCTraversal
```

NPC navigation is therefore downstream from both resolved tile openings and generated traversal structures. A change that modifies either may require route-graph rebuild behavior to remain correct.

## Persistence

```mermaid
flowchart TD
    GameSaveManager --> GameplayLoopController
    GameSaveManager --> TileGridGenerator
    GameSaveManager --> TilePlacement
    GameSaveManager --> NPCTraversal

    TileGridGenerator -->|capture / restore| Layout[Tile layout + connection intent]
    TileGridGenerator -->|capture / restore| Content[Traps + floor props + entrance]
    GameplayLoopController -->|capture / restore| Progression[phase-adjacent persistent gameplay state]
    NPCTraversal -->|capture / restore| NPCState[adventurers + recoverable loot]
```

When adding persistent gameplay content, check `GameSaveManager` before assuming a prefab or component will automatically survive load.

## Lighting

```mermaid
flowchart LR
    TileGridGenerator -->|LayoutChanged| DungeonLightingManager
    DungeonLightSource -->|SourcesChanged| DungeonLightingManager
    DungeonLightingManager -->|global light texture / shader globals| DungeonMaterials[Dungeon materials]
    DungeonLightingManager --> DungeonLightReceiver
```

The lighting manager listens to layout changes and maintains its own chunked light field. Avoid adding lighting state directly to tile-placement logic unless the placement system genuinely owns that state.

## Dependency strength legend

- **Direct coordinator dependency:** serialized/component reference or explicit call path.
- **Event dependency:** a subsystem reacts to an event such as `LayoutChanged` or `SourcesChanged`.
- **Data dependency:** a ScriptableObject or profile supplies authored configuration.
- **Reconstruction dependency:** one runtime system rebuilds derived state from another authoritative system.

For exact code-level details, use [`../Reference/Script_Index.md`](../Reference/Script_Index.md) together with IDE “Find References”.
