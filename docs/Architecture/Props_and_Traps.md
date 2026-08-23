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

## Externally attached cell traps

`CellTrap` is the base contract for a trap affecting one dungeon cell while its
mechanism occupies adjacent external service space:

```text
Initialize(TileGridGenerator grid, TrapAttachmentPlacement attachment)
OnNpcEntered(NPCCharacter npc)
```

`TrapAttachmentDefinition` on the trap prefab declares allowed and preferred
Floor, Ceiling, LeftWall, and RightWall surfaces. `TileSocketProfile` may narrow
the surfaces supported by a concrete tile profile. A legacy profile with no mask
authored currently permits every surface.

The resolved `TrapAttachmentPlacement` keeps three identities separate:

- `TargetCell`: traversable corridor cell whose NPC-entry event triggers the trap;
- `ServiceCell`: adjacent, unbuilt cell reserved by the external mechanism;
- `Surface`: determines the relationship and hazard direction from mechanism to
  corridor.

The service cell must be an interior, unbuilt, non-fixed cell with no generated
prop or other trap-service reservation. Building into reserved service space is
rejected. The resolved surface is saved and scenario-captured so reconstruction
does not choose a different orientation based on restore order.

During placement, the hovered cell is the prospective service cell. The grid
examines its cardinal neighbors and returns only fully valid corridor targets.
For each candidate, the service-to-target offset determines the attachment
surface, hazard direction, and mechanism pose. The preferred supported surface
is considered first, followed by Floor, Ceiling, LeftWall, and RightWall, so the
default is deterministic. `R` cycles only the valid candidates for the current
service cell; moving to another cell resets the selection.

The mechanism clone remains in the hovered service cell, a second indicator
identifies the selected target corridor, and a colored line shows the hazard
direction. Preview validation does not mutate topology or reservations. Both
preview and committed instances use the same grid-owned candidate and pose
resolution, keeping visual rotation and `CellTrap.HazardDirection` aligned.

The current placement transaction still reserves the whole existing unbuilt
cell as service space. t021 adds `TileConstructionSurfaces` to separate authored
floor, ceiling, cardinal-wall, and trap-service anchors/modules from the tile's
logical traversal profile. This allows trap-support visuals to target controlled
prefab regions without editing a monolithic mesh. Shrinking service occupancy
below one logical cell remains separate placement/conflict work.

`SpikeWallTrap` is an example concrete implementation. Its prefab supports all
four attachment surfaces and currently prefers Floor when more than one is
available. It owns damage, difficulty, dodge settings, animation timing,
cooldown, and animation state. It delegates the actual NPC challenge/effect
resolution to `NPCActionResolver`.

### Tile prefab/modularity implications

`TileConstructionSurfaces` is an optional prefab-root contract with stable slot
IDs, a semantic surface kind, a prefab-local anchor, controlled module variants,
and trap-attachment compatibility. Visual-only variants can be activated through
the contract. A slot marked `RequiresTopologyResolution` cannot be swapped by
that API; openings and other traversal changes must instead re-resolve the
authoritative `TileSocketProfile`. The representative `Narrow_Straight_I` tile
contains floor, ceiling, four cardinal-wall, and trap-service slots while
retaining its existing mesh, colliders, prop sockets, and baked profiles.

The cell-sized service reservation remains the conservative conflict model.
Smaller explicit construction volumes and conflicts belong to t022 rather than
being inferred from marker transforms.

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
