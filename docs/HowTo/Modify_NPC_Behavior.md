# Modify NPC Behavior

## Goal

Change adventurer behavior while minimizing unrelated impact to navigation, building, and persistence.

## First classify the change

| Change | Start here |
|---|---|
| Movement/path choice | `NPCTraversal` |
| Character-owned state/stat | `NPCCharacter` |
| Trap/action outcome | `NPCActionResolver` or trap/action component |
| Interaction target | `DungeonPointOfInterest` / host content |
| Carried/recoverable loot | loot-specific components + `NPCTraversal` integration |
| Exploration phase lifecycle | `GameplayLoopController` |

Avoid starting in `NPCTraversal` simply because “an NPC does it.” It is primarily the route/runtime coordinator and can become a high-blast-radius dumping ground if feature-local rules are added there.

## Navigation changes

Read [`../Architecture/NPC_Runtime.md`](../Architecture/NPC_Runtime.md) before changing route construction. Navigation is derived from resolved tile openings plus traversal structures. If you add a new movement capability, decide whether it changes:

- graph construction;
- edge eligibility;
- agent-specific edge filtering;
- action performed at an edge/POI.

Capability-specific behavior should not require duplicating the whole route graph unless necessary.

## Action/interaction changes

Prefer this flow:

```mermaid
flowchart LR
    NPCTraversal --> Target[Trap / POI / encounter]
    Target --> NPCActionResolver
    NPCActionResolver --> NPCCharacter
```

Keep animation/presentation on focused components where possible.

## Simulation time

If the behavior should pause and scale with dungeon simulation speed, use `DungeonSimulationState` rather than raw Unity time APIs.

## Persistence

Ask whether the new state must survive between expansion/exploration cycles and save/load. Stable progression belongs in an owning model/controller; transient route/action state usually should be rebuilt.

## Verification

At minimum test:

- straight corridor;
- junction choice;
- traversal connector;
- trap/POI interaction if relevant;
- pause and multiple simulation speeds;
- death/escape outcome if relevant;
- a second exploration run after returning to Expansion;
- save/load if persistent state changed.

## Design reference

Keep implementation aligned with [`../Design/NPC_Behavior.md`](../Design/NPC_Behavior.md) and [`../Design/Capability_Gated_Traversal.md`](../Design/Capability_Gated_Traversal.md).
