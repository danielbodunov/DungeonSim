# t012 — Visible Adventurer Carried Loot

## Tracking
- **ID:** t012
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t011 validation

## Goal
Make stolen treasure materially visible while an adventurer carries it so ownership changes are readable from the game world rather than only from debug state.

## Requirements
- Add a visual carried-loot representation driven by authoritative NPC treasure custody.
- Prefer a generic bag/bundle/container representation for the first pass rather than attaching arbitrary treasure prefab geometry directly.
- Visual appears when relevant loot is acquired and disappears when custody is cleared by escape, death, recovery, or other authoritative resolution.
- Support future mixed physical loot without coupling the visual system specifically to `TreasureProp`.
- Keep visual state derived from gameplay ownership; it must not become a second inventory authority.

## Acceptance Criteria
- Treasure pickup visibly changes the adventurer.
- Death/escape/custody clearing removes the carried visual at the correct time.
- Empty-handed adventurers show no loot visual.
- Multiple carried items can be represented without requiring one visible model per item in the first implementation.

## Out of Scope
- Full equipment/inventory visualization
- Physical death drops (t013)
- Player recovery interaction

## Manual Validation

1. Enter Play Mode and spawn an adventurer with no carried treasure. Verify no `Carried Loot Bundle` is visible beneath the NPC.
2. Let the adventurer investigate and take treasure. Verify one brown tied sack appears immediately on the adventurer and follows it through normal movement and ladders.
3. Let the same adventurer take additional treasure. Verify the same bundle remains, grows modestly to communicate fullness, and does not create one model per item.
4. Kill the adventurer while it carries treasure. Verify custody is cleared through the existing recovery outcome and the carried bundle does not remain in the world.
5. Let another adventurer escape with treasure. Verify the bundle disappears when escape finalization clears custody and no death recovery is created.
6. Exercise forced retreat/session cleanup and a new empty-handed visit. Verify the bundle clears and does not reappear until authoritative custody becomes non-empty again.

## Implementation Status

- Added `ICarriedLootPresentationSource`, a presentation-only aggregate contract that exposes item count and a change signal without granting mutation access or referencing `TreasureProp`.
- `NPCTraversalAgent` implements the contract from its authoritative carried-dungeon-treasure collection. Pickup, death, successful escape, forced retreat, and visit reset all notify through the same custody-derived path.
- Every production-spawned adventurer receives `NPCCarriedLootVisual`; no scene or NPC prefab assignment is required.
- The visual creates one generic tied sack from runtime primitives, positions it from the NPC renderer bounds, disables generated colliders, follows the NPC hierarchy, and remains inactive while the aggregate count is zero.
- Multiple items modestly scale the same bundle, capped after four represented items, rather than creating one model per item.
- Public read-only visual state (`IsVisible` and `RepresentedItemCount`) supports runtime inspection without becoming inventory state.

## Validation Notes

- Runtime assembly compiles with zero warnings and zero errors, including the new source through a temporary validation-only MSBuild include because Unity had not yet regenerated its project file.
- Editor assembly compiles with zero errors and the pre-existing `TileSocketBakerWindow.visualizeSamples` unused-field warning.
- Static mutation-path review confirms all current custody additions and clears publish the visual refresh signal.
- Manual Unity validation completed successfully on 2026-08-16.

## Known Limitations

- The first-pass sack is procedural prototype geometry and uses renderer bounds for placement. A future art pass can replace the presentation component's geometry without changing custody ownership.
- Bundle fullness scaling is intentionally coarse and capped; it communicates non-empty/multiple custody rather than exact item count.

## Git
Suggested branch: `feature/t012-visible-carried-loot`

Active branch: `feature/t012-visible-carried-loot`
