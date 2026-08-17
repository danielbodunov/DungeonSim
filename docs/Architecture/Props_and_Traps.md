# Props and Traps Architecture

## Content categories

DungeonSim currently separates several kinds of dungeon content because they have different ownership and topology requirements.

| Content | Typical owner | Topology-sensitive? | Persistent? |
|---|---|---:|---:|
| Cell trap | `TileGridGenerator` placement state + `CellTrap` subclass | Usually cell occupancy; effect may depend on orientation | Yes |
| Floor prop | `TileGridGenerator` placement state + `FloorProp` | Low; compatibility hook can add restrictions | Yes |
| Dungeon entrance | authoritative placed entrance state | Yes; must use compatible authored socket | Yes |
| Procedural prop/structure | `PropGenerator` | Often; can occupy cells or provide traversal | Reconstructed from authoritative inputs/seed unless separately promoted |
| POI | content component bound to a cell | Depends on host content | Through host/content state as applicable |

## Procedural props

`PropGenerator` owns generated structure state and uses `PropCatalog` definitions. `PropDefinition` supports:

- `Single` or `Chained` generation;
- spawn chance;
- whether the result occupies a cell;
- socket rotation;
- rotation offset;
- lane-specific bundles;
- role-based prefab fallback.

Generated structures are derived state. If the authoritative layout is restored, generated props can be discarded and regenerated.

## Cell traps

`CellTrap` is the base contract for a trap bound to one dungeon cell:

```text
Initialize(TileGridGenerator grid, Vector2Int cell)
OnNpcEntered(NPCCharacter npc)
```

`SpikeWallTrap` is an example concrete implementation. It owns damage, difficulty, dodge settings, animation timing, cooldown, and animation state. It delegates the actual NPC challenge/effect resolution to `NPCActionResolver`.

This is the preferred pattern: the grid knows where the trap is; the trap knows its local behavior; the action resolver knows how the NPC outcome is resolved.

## Floor props

`FloorProp` is deliberately less topology-sensitive than entrances, doors, or ladders. Its default compatibility requirement is simply that the owning cell is built. A subclass can override compatibility for both the live grid and a `PlacementValidationContext`.

Floor props may also expose child `DungeonPointOfInterest` components. `FloorProp.Initialize` binds those POIs to the grid/cell explicitly.

## Entrances and traversal structures

Entrances and traversal connectors should use authored sockets because their position/orientation has gameplay meaning. Do not treat a ladder or dungeon entrance as arbitrary decoration.

The current design direction further distinguishes player-placed strategic traversal from procedural decoration. See [`../Design/World_Generation_and_Building.md`](../Design/World_Generation_and_Building.md).

## Where to implement a feature

- New trap behavior only → new `CellTrap` subclass; avoid grid changes.
- New floor prop with a special placement rule → subclass `FloorProp` and override compatibility.
- New procedural decorative set → `PropCatalog` data plus prefab/socket authoring; touch `PropGenerator` only if generation semantics are new.
- New traversal structure → treat as cross-system work because `NPCTraversal` consumes it.
- New placement category with fundamentally new ownership → may require `ObjectsDatabaseSO`, `TilePlacement`, `TileGridGenerator`, and save changes.

## Persistence check

Before adding content, decide whether it is:

1. **authoritative player state** — must be explicitly saved/restored; or
2. **derived/generated state** — should be reproducible from saved authoritative inputs and generation seed.

Do not save a generated prefab instance merely because recreating it is inconvenient.

## Related guides

- [`../HowTo/Add_A_Trap.md`](../HowTo/Add_A_Trap.md)
- [`../HowTo/Add_A_Floor_Prop.md`](../HowTo/Add_A_Floor_Prop.md)
- [`../HowTo/Add_A_Procedural_Structure.md`](../HowTo/Add_A_Procedural_Structure.md)
- [`Save_System.md`](Save_System.md)
