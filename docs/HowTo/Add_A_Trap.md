# Add a Trap

## Goal

Add a new dungeon trap while keeping placement, NPC resolution, and persistence responsibilities separated.

## Preferred implementation path

For a trap that occupies one dungeon cell and triggers when an NPC enters:

1. Create a new component deriving from `CellTrap`.
2. Put trap-local configuration and animation/timing in that subclass.
3. Implement `OnNpcEntered(NPCCharacter npc)` and delegate shared NPC outcome logic to `NPCActionResolver` where appropriate.
4. Create/configure the trap prefab.
5. Add it to the build content database as `ObjectPlacementType.Trap`.
6. Verify grid placement and save/load behavior.

`SpikeWallTrap` is the current reference implementation.

## Minimum code surface

Usually:

- new trap subclass;
- prefab/controller/animation assets;
- build database entry.

Avoid editing `TileGridGenerator` or `NPCTraversal` for a normal one-cell trap unless the new trap genuinely changes placement topology or traversal semantics.

## Trap ownership

`CellTrap` owns:

- grid reference;
- owning cell;
- NPC-entry contract.

The concrete trap should own:

- damage/effect configuration;
- cooldown/readiness;
- animation/timing;
- trap-specific state.

The grid should own **where the trap is placed**, not how it resolves its gameplay challenge.

## Full-cell trap design

For the current DungeonSim design direction, a trap should reserve at least one full cell. A wall-spike mechanism can live in the neighboring trap cell and orient toward the traversed/target cell rather than being squeezed into an arbitrary fragment of floor geometry.

A future crusher that requires mechanisms on opposite corridor sides is a **multi-cell trap**. Do not fake that as two unrelated one-cell traps if they must behave and persist as one logical structure; that requires explicit multi-cell structure ownership/footprint support.

## Placement considerations

Check:

- target cell is built;
- cell is not already reserved by conflicting placed content;
- orientation can be derived deterministically from player selection or valid surrounding topology;
- replacing/re-resolving a host tile does not silently orphan the trap.

## NPC behavior

A basic trap should use `OnNpcEntered` as its entry point. If the trap needs a more complex interaction (approach state, disarming, multi-stage encounter, line-of-sight trigger), prefer adding a focused trap/action behavior rather than embedding trap-specific conditions in `NPCTraversal`.

## Save/load

Trap placement is authoritative content. Confirm the save layer records enough stable information to reconstruct:

- trap type/prefab identity;
- cell/footprint;
- orientation if meaningful;
- any resolved persistent state that should survive loading.

Transient animation/cooldown state usually does not need persistence unless the design explicitly requires mid-cycle saving; current saving is constrained to the Expansion phase.

## Verification

1. Place trap in valid and invalid cells.
2. Remove it through the normal build workflow.
3. Enter the cell with an NPC and verify one trigger.
4. Verify pause/simulation-speed behavior if timing is used.
5. Test cooldown/reset.
6. Re-resolve neighboring tiles and ensure placement remains correct or is removed by an explicit compatibility rule.
7. Save/load and trigger the restored trap.

## Read next

- [`../Architecture/Props_and_Traps.md`](../Architecture/Props_and_Traps.md)
- [`../Architecture/NPC_Runtime.md`](../Architecture/NPC_Runtime.md)
- [`../Architecture/Save_System.md`](../Architecture/Save_System.md)
