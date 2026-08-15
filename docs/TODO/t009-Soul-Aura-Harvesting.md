# t009 — Soul / Aura Harvesting Foundation

## Tracking

- **ID:** t009
- **Status:** Complete
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t005
- **Blocks:** t010 — Expedition Outcomes

## Summary

Establish the first explicit connection between adventurer experiences in the dungeon and the sinister force's Aura/soul-energy progression resource.

Death should provide meaningful Aura, but the architecture should allow other qualifying dungeon experiences to generate energy later without requiring a rewrite.

## Requirements

- Define a narrow authoritative Aura-harvest event/service/path using the existing Aura economy where appropriate.
- Award a configurable prototype Aura amount for adventurer death inside the dungeon.
- Prevent duplicate harvest from repeated death callbacks.
- Preserve source/context information sufficient to add other harvest events later.
- Expose harvested Aura clearly enough for debugging.
- Keep loot recovery and Aura harvesting as separate consequences.

## Acceptance Criteria

- Adventurer death inside the dungeon produces the configured Aura harvest exactly once.
- The existing persistent Aura total changes through its established authoritative mutation path.
- Repeated death/cleanup events cannot duplicate the harvest.
- Loot recovered from the adventurer does not itself automatically become Aura.
- The implementation can later support non-death harvest sources without changing the core currency API.

## Constraints

- Do not implement a large fear/pain/emotion simulation.
- Do not add many harvest event types yet; death is the prototype source.
- Do not couple Aura amount to treasure value.
- Do not implement reputation/notoriety.

## Manual Test Scenario

Record the current Aura total, kill one adventurer inside the dungeon, and verify the configured amount is added exactly once. Trigger cleanup/repeated defeat paths and verify no duplicate award. Confirm recovered treasure remains separate from Aura.

### Focused Unity Steps

1. Enter Play Mode, open **Tools > NPC Runtime Debug Harness**, and select a living adventurer during an active dungeon visit.
2. In **Aura Harvesting**, record Current Aura, Pending Visit Aura, and Selected NPC Death Harvest.
3. Kill the selected adventurer through the normal harness Kill action.
4. Verify one `AdventurerDeath` record appears with the expected amount, adventurer identity, runtime agent, level, dungeon opening, death cell, and harvest ID.
5. Verify Current Aura increased by the recorded death harvest plus any Pending Visit Aura that settled when the visit ended.
6. Click **Retry Latest Harvest (Duplicate Test)**. Verify Current Aura does not change and the record's rejected duplicate count increments.
7. Compare a death carrying dungeon treasure with a death carrying nothing. Verify the death harvest follows adventurer level/configuration only, while recovered loot appears independently under Dungeon Recovery.
8. Finish the visit, save, and reload. Verify the authoritative Aura total persists.

## Implementation Status

- Added a typed `AuraHarvestRequest`/`AuraHarvestRecord` path owned by `GameplayLoopController`, the existing persistent Aura economy owner.
- Death uses the existing configurable level-scaled `baseDefeatAura` and `defeatLevelExponent` balance while crediting that amount as an explicit harvest instead of folding it into pending visit Aura.
- Each request supplies an idempotent harvest ID plus source type, source ID/name, runtime agent, level, dungeon opening, cell, and world position.
- Repeated requests with the same harvest ID are rejected without changing Aura and increment an observable duplicate-attempt counter.
- `NPCTraversal` now exposes an additive detailed defeat event while retaining its existing `AdventurerDied` compatibility event.
- `NPCTraversalAgent` records whether death occurred during an active dungeon visit so out-of-visit deaths cannot harvest Aura.
- Loot recovery completes through its existing path before the independent Aura-harvest notification. Harvest amount never reads carried treasure or recovery value.
- The NPC runtime debug harness shows current/pending Aura, the selected NPC's configured death harvest, accepted harvest records, source context, and duplicate validation controls.
- Save data remains unchanged because the existing `adventurerAura` field already persists the authoritative total.

## Validation Notes

- Runtime C# compilation passes with zero warnings and zero errors.
- Editor C# compilation passes with zero errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` unused-field warning.
- Static inspection confirms death harvest amount is derived only from adventurer level and existing Aura configuration, not carried/recovered treasure.
- Manual Unity validation completed successfully on 2026-08-14.

## Known Limitations

- Harvest records are current-runtime debug/event history and are intentionally not added to save data in this foundation ticket; the authoritative Aura total remains persisted.
- `AdventurerDeath` is the only explicit harvest source in this ticket. Future sources can submit through the same `TryHarvestAura()` currency API.

## Future Direction

Later tickets may consider smaller Aura generation from meaningful expedition events such as exploration, fear, traps, injury, combat, or magic use. Those should be introduced only when they improve the management loop and can be balanced coherently.

## Git

Suggested branch: `feature/t009-soul-aura-harvesting`

Active branch: `feature/t009-soul-aura-harvesting`

Proceed according to `docs/AGENTS.md`.
