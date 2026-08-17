# t007 — Adventurer Loot Drop & Dungeon Recovery

## Tracking

- **ID:** t007
- **Status:** Complete
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t006 — Treasure Pickup & Ownership
- **Blocks:** t008 — Successful Escape & Lost Treasure

## Type

Feature

## Summary

Define the death-side outcome for carried loot. When an adventurer dies inside the dungeon, treasure stolen from the dungeon becomes recoverable rather than disappearing, and the system establishes a future-compatible place for loot the adventurer brought with them to be dropped as spoils.

## Desired Behavior

Killing an adventurer should create material value for the dungeon in addition to future soul harvesting. Dungeon-owned treasure being carried by that adventurer is recoverable, and the loot-drop representation should be able to accommodate adventurer-origin loot later without requiring a full inventory/equipment system now.

## Requirements

- Detect the authoritative adventurer defeat/death outcome.
- Transfer carried dungeon treasure into a recoverable dungeon-side loot state exactly once.
- Clear the dead adventurer's custody after the drop is created.
- Represent dropped/recoverable loot in a way that can later include adventurer-origin valuables/equipment without implementing full itemization now.
- Make recovery observable and testable through debug/runtime state.
- Avoid automatically converting recovered loot into Dread.
- Preserve treasure identity/value sufficiently for future reuse, economy, or re-baiting decisions.

## Acceptance Criteria

- An adventurer carrying dungeon treasure who dies drops/returns that value to a recoverable dungeon-side state.
- The same death cannot duplicate the loot drop.
- The dead adventurer no longer owns the dropped loot.
- An adventurer who dies carrying nothing produces no phantom dungeon treasure.
- The representation can distinguish dungeon-origin treasure from future adventurer-origin loot.
- No successful-escape behavior is implemented here.
- No Dread/soul payout is coupled to the loot recovery implementation.

## Constraints

- Do not implement detailed equipment or inventories.
- Do not generate arbitrary loot tables.
- Do not implement player collection UI unless minimal debug interaction is required to validate recovery.
- Do not settle recovered loot directly into Dread.
- Keep death outcome integration narrow and compatible with existing NPC defeat logic.

## Manual Test Scenario

1. Have an adventurer pick up known dungeon treasure through t006.
2. Cause that adventurer to die before returning to the entrance.
3. Verify the carried treasure becomes recoverable by the dungeon exactly once.
4. Verify the dead NPC no longer reports custody of it.
5. Repeat with an adventurer carrying no treasure and verify no false loot is created.
6. Trigger any repeated death/cleanup callbacks and verify no duplicate recovery occurs.

## Out of Scope

- Successful escape
- Soul/Dread harvesting
- Detailed corpse interaction
- Equipment generation
- Inventory UI
- Loot rarity

## Implementation Notes

- `NPCTraversalAgent.OnCharacterDied` remains the authoritative integration point. Before the existing `AdventurerDied` notification and despawn, `NPCTraversal` claims death-loot processing for that agent exactly once.
- A claimed death snapshots carried custody into a `RecoverableLootDrop`, adds the drop to dungeon-side recovery, and then clears the agent's carried list. Empty or null-only custody is cleared without creating a phantom drop.
- Each `RecoverableLootItem` preserves item identity and value, distinguishes `DungeonTreasure` from `AdventurerPossession`, and retains the original dungeon cell when applicable. Each drop also records its dungeon cell, world position, source adventurer name, and stable session-local drop ID.
- Every processed death also creates an `AdventurerDeathLootOutcome`, including empty-handed deaths. The audit retains a session-local agent ID/name, death location, custody item/value totals before and after processing, recovered totals/drop ID, whether custody cleared, and any rejected duplicate-processing attempts.
- `NPCTraversal` exposes recoverable drop/item/value totals, read-only drop and death-outcome lists, and creation/outcome events. No recovery path references Dread, currency, successful escape, item generation, or inventory systems.
- The existing NPC Runtime Debug Harness shows dungeon recovery and **Death/Custody Outcomes** even after the dead NPC despawns.

## Validation Performed

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo` completed with 0 warnings and 0 errors after including the new runtime source in the generated project for the focused compile.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` completed with 0 errors. The one warning is pre-existing in `TileSocketBakerWindow.cs` (`CS0414`).
- Focused death-path review confirmed recovery is invoked synchronously before the existing death event and despawn, custody is cleared only after a non-empty drop is registered, and the per-agent claim prevents repeated processing.
- Scope review confirmed no recovery code changes Dread, successful-return behavior, item generation, equipment, inventory, or player collection UI.
- Follow-up runtime and Editor compilation after adding the death/custody audit completed with 0 runtime warnings/errors and 0 Editor errors; the same pre-existing Editor warning remains.
- Manual Unity validation was completed on 2026-08-14. Loot transfer, custody clearing, death/custody auditing, empty-handed death, duplicate prevention, Dread separation, and successful-return non-recovery behavior passed.

## Manual Unity Validation Performed

1. Load or build a scenario with reachable treasure of a known ID/value, spawn an adventurer, and open `Tools > NPC Runtime Debug Harness`.
2. Let the adventurer investigate and take the treasure. Verify **Carried Dungeon Treasure** shows the expected identity, value, and source cell while **Dungeon Recovery** remains empty.
3. Kill the adventurer before it returns. Verify **Dungeon Recovery** gains exactly one drop at the death cell with the expected item, value, `DungeonTreasure` origin, and original source cell.
4. Under **Death/Custody Outcomes**, verify the dead agent record reports the expected custody before processing, zero items/value afterward, `cleared True`, and the matching recovery drop ID.
5. Continue or reset normal runtime cleanup and verify recovery totals remain unchanged. If a duplicate callback is deliberately triggered, verify the same outcome's duplicate-attempt count increases without another drop.
6. Kill an adventurer carrying nothing. Verify no new drop/item/value is created, while a death outcome records zero custody before and after with `no drop` and `cleared True`.
7. Compare equal-level deaths with and without carried treasure. Verify any existing defeat Dread award is unchanged by treasure identity/value and no recovered loot is converted into Dread.
8. Let an adventurer return successfully while carrying treasure and verify t007 creates no recovery drop; successful-escape settlement remains unimplemented for t008.

## Known Limitations

- Recoverable loot is a runtime dungeon-side data record in t007; saving/loading it and consuming or re-baiting it are deferred until those gameplay decisions are defined.
- No physical corpse/drop object or player collection interaction is created.

## Git

Implementation branch: `feature/t007-loot-recovery`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
