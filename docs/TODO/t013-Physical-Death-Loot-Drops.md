# t013 — Physical Death Loot Drops

## Tracking
- **ID:** t013
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t012; existing t007 recovery records

## Goal
Represent recoverable adventurer death loot as a persistent world object at the death location instead of only an abstract recovery record.

## Requirements
- Materialize authoritative recoverable loot as a world-space loot pile/bag/drop at the recorded death cell/position.
- The world object references/summarizes the existing recoverable loot data rather than becoming a competing ownership ledger.
- Preserve item identity, value, origin, and source-cell information needed by later recovery/rediscovery systems.
- The drop remains present until an authoritative consumer claims/resolves it.
- Integrate with save/load if the drop can survive across saved game state.
- Make the object discoverable/interactable through a generic POI-compatible path suitable for t014/t015.

## Acceptance Criteria
- NPC death with carried loot creates one visible drop at the death location.
- Empty-handed death creates no phantom drop.
- Duplicate death processing creates no duplicate physical drop.
- Drop contents match the authoritative t007 recovery record.
- The drop can be queried by later NPC/player systems without special-casing the original dead NPC.

## Out of Scope
- Other adventurers taking the loot (t014)
- Player recovery UI (t015)
- Detailed item models for every contained resource

## Git
Suggested branch: `feature/t013-physical-death-loot`

## Manual Validation

1. Enter Play Mode with an adventurer and an available treasure prop.
2. Let the adventurer take the treasure, then kill it in a different valid dungeon cell.
3. Verify one visible `Recoverable Loot [recovered-loot-####]` bag appears at the death position, matches the carried bag's scale, and remains after the dead NPC despawns.
4. In the NPC Runtime Debug Harness, verify the recovery record reports `world present`, and use **Select Drop** to select the matching world object in the Hierarchy.
5. Inspect the world object's `DungeonPointOfInterest`; verify its type is `RecoverableLoot`, its target ID matches the recovery drop ID, and its cell matches the death cell.
6. Send another adventurer through the drop's cell and verify it visibly pauses for the drop's one-second investigation period without taking or resolving it; acquisition remains t014 scope.
7. Compare the debug record's item ID, value, origin, and source cell with the treasure that was carried.
8. Kill an empty-handed adventurer and exercise any repeated death/cleanup debug path. Verify neither case creates an extra recovery record or physical bag.
9. Save during Expansion, move or otherwise distinguish the physical bag, load the save, and verify exactly one bag is recreated at its recorded position with matching contents.

## Implementation Status

- The t007 `RecoverableLootDrop` remains the authoritative ownership record. `RecoverableLootWorldDrop` stores only the drop ID and resolves all item/count/value data through `NPCTraversal`.
- Accepted death recovery materializes one procedural loot bag at the record's exact world position and binds a `RecoverableLoot` POI to the recorded death cell. Empty and duplicate death paths remain governed by the existing t007 claim.
- `NPCTraversal.TryClaimRecoverableLoot` is the single resolution boundary: it removes the authoritative record and its world view together and returns the preserved item snapshots to the caller.
- Save format version 7 captures deep copies of recoverable drop/item records. Loading validates drop identity, contents, unique IDs in the saved record set, and placed death cells before rebuilding physical views.
- The runtime debug harness reports physical-view count/state and can select a bag in the Hierarchy.
- Carried and dropped loot use the same shared procedural geometry, material, fullness scaling, and base size so custody changes do not visually resize the bundle.
- Recoverable-loot POIs use a one-second prototype investigation duration, making discovery observable without implementing t014 acquisition or resolving the drop.
- Death cells are resolved from the exact death world position before the recovery record and POI are created. This prevents mid-route deaths from displaying a bag in one cell while registering its POI in the previously completed traversal cell.

## Validation Notes

- Runtime compilation completed with 0 warnings and 0 errors using a temporary validation import for the new Unity source file; the temporary file was removed afterward.
- Editor compilation completed with 0 errors and the existing `TileSocketBakerWindow.visualizeSamples` CS0414 warning.
- Static review confirmed physical views contain no copied item ledger, all resolution routes through `NPCTraversal`, and generated visual primitive colliders are removed so drops do not affect navigation ground probes.
- Manual Unity Play Mode validation completed successfully on 2026-08-16, including physical creation, consistent carried/drop scale, POI investigation, duplicate/empty-handed handling, debug selection, and save/load restoration.

## Known Limitations

- t013 uses a procedural generic bag and does not display individual item models.
- The POI is discoverable now, but NPC acquisition and player recovery behavior remain intentionally deferred to t014 and t015.
