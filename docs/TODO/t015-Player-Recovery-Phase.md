# t015 — Player Recovery Phase

## Tracking
- **ID:** t015
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t013

## Goal
Give the player an explicit between-expedition cleanup/recovery interaction for physical resources left in the dungeon rather than automatically converting all aftermath into ledger values.

## Requirements
- During an appropriate non-expedition/build phase, allow the player to identify recoverable physical loot remaining in the dungeon.
- Player can inspect/select a drop and deliberately recover its contents.
- Recovery transfers contents through an authoritative economy/inventory boundary and resolves the world drop exactly once.
- Make the action and recovered value legible in the world/UI.
- Keep the first pass lightweight; recovery should not require a full character-controlled cleanup simulation.
- Define behavior for unrecovered drops when the next expedition begins.

## Acceptance Criteria
- A death drop can remain after an expedition and be selected by the player.
- Recovering it credits the appropriate physical resources/treasure and removes/resolves the drop.
- A resolved drop cannot be recovered twice.
- Unrecovered-drop behavior across phase transitions is deterministic and documented.

## Out of Scope
- Full inventory management UI
- Worker/minion cleanup jobs
- Complex salvage recipes

## Git
Suggested branch: `feature/t015-player-recovery`

## Implementation Status

- Expansion now presents a lightweight **Physical Loot Recovery** panel. It enumerates authoritative recovery drops, shows their cell, source adventurer, item identities, values, and dungeon/adventurer provenance, and lets the player move between, focus, and deliberately recover them.
- Selecting a bag activates a production runtime highlight on its physical `RecoverableLootWorldDrop`. **Focus** uses the existing generic gameplay-camera focus API; manual camera behavior remains unchanged.
- `GameplayLoopController.TryRecoverLootDrop` is the authoritative between-expedition transaction. It validates Expansion phase and drop availability, claims through `NPCTraversal.TryClaimRecoverableLoot`, transfers item snapshots into dungeon storage, records the accepted recovery by source drop ID, and never mutates Aura.
- Clicking a physical recovery bag during Expansion invokes that transaction directly. Shared gameplay-input ownership suppresses clicks from Editor tools, UI clicks are ignored, and an active build/removal tool retains click priority.
- Dungeon storage retains item ID, value, origin, optional original source cell, and recovery-drop provenance. UI totals distinguish returned dungeon treasure from adventurer-origin spoils without introducing a full inventory screen.
- Recovery inventory and audit records persist through save/load and DungeonTestScenario capture/reset. Save format version 8 also persists the next recovery-drop number so a loaded game cannot reuse an already resolved drop identity.
- Scenario preflight rejects a runtime snapshot whose same drop ID is both physically available and already credited to storage.
- Unrecovered drops remain authoritative world objects when the dungeon opens. The recovery panel and player selection hide during Exploration, while later adventurers can still discover and acquire those drops through the existing t014 POI flow.

## Validation Notes

- Runtime compilation completed with 0 warnings and 0 errors.
- Editor compilation completed with 0 errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` CS0414 warning.
- `git diff --check` completed without whitespace errors.
- Unity Play Mode validation completed successfully on 2026-08-16.

## Manual Validation

1. Have an adventurer die with treasure, allow the visit to finish, and verify the **Physical Loot Recovery** panel appears during Expansion with the correct bag, cell, source, item identity, value, and origin breakdown.
2. With two or more drops, use **Previous** and **Next**. Verify only the selected world bag has the amber ring. Use **Focus** and verify the gameplay camera smoothly frames that bag while normal manual-pan cancellation still works.
3. With no build/removal tool active, click a physical bag directly in the Game View. Verify the bag, POI, and authoritative recoverable record disappear; dungeon-storage item/value totals increase by exactly the displayed amounts; and Aura does not change. Confirm clicking UI or an Editor tool does not recover a bag.
4. Confirm the panel's **Recover Selected Drop** action remains available as an inspection/debug alternative and produces the same authoritative result.
5. Attempt to recover the same drop again, including a rapid repeated click if practical. Verify storage and the recovery audit increase only once.
6. Leave another bag unrecovered and open the dungeon. Verify the recovery panel/highlight disappear, the bag remains in place, and an adventurer can still discover/investigate it through normal t014 behavior. When Expansion returns, verify any still-unclaimed bag appears in the panel again.
7. Recover one bag, leave one bag in the world, save, change both states by recovering the remaining bag, and reload. Verify the first bag is stored, the second physical bag returns, and a subsequent death receives a new non-colliding drop ID.
8. Capture a DungeonTestScenario with one stored bag and one unrecovered bag, recover the remaining bag, then load/reset twice. Verify each application restores the same storage totals/audits and physical bags without duplicating either state.

## Known Limitations

- Recovery is an immediate between-expedition management action; worker travel and cleanup simulation remain out of scope.
- Dungeon storage is an itemized authoritative ledger with aggregate UI, not a general inventory-management interface.
