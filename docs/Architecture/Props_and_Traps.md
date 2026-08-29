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

t022 expands that result into three explicit cell collections:

- `MechanismCells`: physical mechanism occupancy, including `ServiceCell`;
- `InfrastructureCells`: additional reserved support/clearance cells;
- `HazardCells`: affected corridor volume, including `TargetCell`, which does
  not reserve external construction space.

`TrapAttachmentDefinition` authors additional cells in attachment-local integer
coordinates. Local X is lateral to the hazard direction and local Y points from
the primary service cell toward the target. The grid rotates these offsets for
Floor, Ceiling, LeftWall, and RightWall placements. Empty footprint arrays retain
the one-service-cell/one-target-cell behavior of existing trap prefabs.

The service cell must be an interior, unbuilt, non-fixed cell with no generated
prop or other trap-service reservation. Building into reserved service space is
rejected. The resolved surface is saved and scenario-captured so reconstruction
does not choose a different orientation based on restore order.

Every mechanism and infrastructure cell follows those service-space rules and
is reserved as one atomic footprint. Hazard cells must be built corridor cells,
but do not block traversal or reserve construction by themselves. A footprint
may not overlap its own hazard volume. Existing trap footprints participate in
live placement validation, and scenario/save validation accumulates the same
reservations in restore order before any authoritative save mutation.

Construction into a trap target or any reserved footprint cell requires explicit
trap removal first. Local cell or connection re-resolution is also rejected when
the resulting tile profile would no longer support the trap's saved attachment
surface.

The removal tool follows physical ownership: the player selects the trap's
primary `ServiceCell`, which resolves back to the authoritative target-cell trap
record. Clicking the affected corridor cell does not remove the external
mechanism.

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
Additional mechanism, infrastructure, and hazard cells receive cell indicators
from the same resolved placement result. Trap grid indicators use the hovered
cell indicator's Z plane. The hazard-direction line instead uses the target
cell center's Z plane so it aligns with the affected corridor volume.

### Trap construction presentation

`TrapConstructionPresentation` is derived from the resolved
`TrapAttachmentPlacement`; it is not placement authority. A trap definition may
provide a target-surface module prefab, a mechanism-cell module prefab, and an
infrastructure-cell module prefab. The SpikeWall uses this generic contract.
When neither an authored surface variant nor a presentation prefab is available,
the validation implementation creates a shared-material fallback module so the
complete footprint remains readable.

The prospective presentation is built for preview under a `DontSave` root. It
does not change live tile modules, ground renderers, topology, or reservations.
Committed placement uses the same positions and authored presentation inputs.
Mechanism and infrastructure cells hide their ordinary generated ground
renderers while the trap owns them, but remain logically unbuilt and
non-traversable. Authoritative reservation rules resolve competing ownership
before presentation is created.

If the target tile exposes a compatible `TileConstructionSurfaces` slot marked
`VisualOnly`, preview retrieves the requested variant without changing live
active states. Renderers under the currently selected module are suppressed
transiently, with their individual enabled states recorded and restored as soon
as preview changes or ends. Preview then clones only the requested module under
the transient preview root. Its authored world transform, scale, meshes, and
materials are preserved while colliders, rigidbodies, and behaviours are made
non-interactive. Commit selects
the same live variant and records the previously active variant for removal. A
usable authored variant suppresses the target prefab/fallback, preventing a
duplicate target treatment. Missing variants fall back to the explicit target
prefab and then the generated fallback module. A slot marked
`RequiresTopologyResolution` is never selected through this path, so target
presentation cannot silently change connections or traversal.

Removing or clearing a trap restores the recorded target variant and re-enables
the ground renderers hidden by that trap. Save/load and scenario reset rebuild
the presentation through normal trap placement; no presentation GameObjects are
saved.

Production trap art should provide modular presentation prefabs sized to the
logical cell and use shared materials. Target modules align to the construction-
surface anchor. Mechanism and infrastructure modules use cell centers and must
not add traversal colliders or modify `TileSocketProfile`.

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
