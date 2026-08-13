# t007 — Adventurer Loot Drop & Dungeon Recovery

## Tracking

- **ID:** t007
- **Status:** Planned
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
- Avoid automatically converting recovered loot into Aura.
- Preserve treasure identity/value sufficiently for future reuse, economy, or re-baiting decisions.

## Acceptance Criteria

- An adventurer carrying dungeon treasure who dies drops/returns that value to a recoverable dungeon-side state.
- The same death cannot duplicate the loot drop.
- The dead adventurer no longer owns the dropped loot.
- An adventurer who dies carrying nothing produces no phantom dungeon treasure.
- The representation can distinguish dungeon-origin treasure from future adventurer-origin loot.
- No successful-escape behavior is implemented here.
- No Aura/soul payout is coupled to the loot recovery implementation.

## Constraints

- Do not implement detailed equipment or inventories.
- Do not generate arbitrary loot tables.
- Do not implement player collection UI unless minimal debug interaction is required to validate recovery.
- Do not settle recovered loot directly into Aura.
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
- Soul/Aura harvesting
- Detailed corpse interaction
- Equipment generation
- Inventory UI
- Loot rarity

## Git

Suggested branch: `feature/t007-loot-recovery`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
