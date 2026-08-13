# t006 — Treasure Pickup & Ownership

## Tracking

- **ID:** t006
- **Status:** Complete
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t005 — NPC Treasure Discovery & Investigation
- **Blocks:** t007 — Adventurer Loot Drop & Dungeon Recovery

## Type

Feature

## Summary

Turn investigated treasure into physical/semantic loot carried by the adventurer. Treasure begins as dungeon-owned bait. When an adventurer successfully investigates and takes it, ownership transfers out of the dungeon and into that adventurer's carried loot state.

This ticket establishes ownership and custody. It does not yet decide the final outcome of that loot on death or successful escape.

## Desired Behavior

Treasure is a resource the dungeon risks in order to entice adventurers deeper. A treasure prop is dungeon-owned while available in the dungeon. Successful pickup removes it from its authored location/availability and records the corresponding loot as carried by the specific adventurer.

## Requirements

- Add a minimal carried-loot representation to the appropriate adventurer/visit state.
- Preserve the identity/value of dungeon treasure when it is picked up.
- Transfer treasure exactly once after successful t005 investigation/resolution.
- Mark/remove the world treasure from active dungeon availability using the existing treasure/POI state.
- Track that the carried treasure originated as dungeon-owned bait so later death/escape outcomes can handle it correctly.
- Expose carried loot sufficiently for debugging and later decision systems.
- Reset visit-local custody correctly between visits without silently restoring treasure that was actually removed from the dungeon.

## Acceptance Criteria

- A new adventurer begins with no carried dungeon treasure.
- Successfully investigating a treasure transfers that treasure into the adventurer's carried loot exactly once.
- The treasure is no longer available in its dungeon socket after pickup.
- Re-entering the cell cannot pick up the same treasure again.
- Carried loot retains enough identity/value information for later recovery or loss.
- Merely discovering or beginning investigation does not transfer ownership.
- No player currency or Aura is awarded by treasure pickup.

## Constraints

- Do not implement death-drop/recovery; t007 owns that outcome.
- Do not implement successful-escape loss; t008 owns that outcome.
- Do not implement inventory slots, encumbrance, rarity, equipment, or itemization.
- Do not convert treasure into Aura or player currency.
- Treasure remains bait/value, not a reward automatically generated for the dungeon.

## Manual Test Scenario

1. Start an adventurer with empty carried loot.
2. Reach and successfully investigate a known treasure.
3. Verify the treasure leaves active dungeon availability and appears in that adventurer's carried loot once.
4. Revisit the cell and verify no duplicate pickup occurs.
5. Start investigation but interrupt/cancel before completion if supported; verify ownership does not transfer.
6. Verify no Aura/player currency changes merely because treasure was taken.

## Out of Scope

- Loot recovery after death
- Loot loss after escape
- Adventurer-owned starting equipment
- Soul/Aura harvesting
- Treasure-driven retreat decisions
- Inventory UI

## Implementation Status

- Added a visit-local `CarriedDungeonTreasure` custody snapshot containing the
  treasure identity, value, source cell, and dungeon-bait origin.
- Exposed the carried list, count, and total value on `NPCTraversalAgent`; the
  serialized runtime list is also visible in the Inspector for debugging and
  is available to the existing death/visit-completion callbacks before despawn.
- Extended the generic POI completion contract with the investigating agent so
  `TreasureProp` can transfer custody without adding treasure-specific logic to
  traversal's discovery flow.
- Treasure resolution and custody transfer occur together exactly once after a
  successful investigation. Interrupted investigation performs neither action.
- A fresh visit clears visit-local custody without resetting resolved world
  treasure, and pickup does not modify Aura or player currency.
- Runtime compilation, focused source validation, and the manual Unity scenario
  were completed successfully on 2026-08-12.

## Git

Suggested branch: `feature/t006-treasure-ownership`

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
