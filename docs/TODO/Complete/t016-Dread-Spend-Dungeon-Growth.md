# t016 — Dread Spend & Dungeon Growth Foundation

## Tracking
- **ID:** t016
- **Status:** Complete
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t011; t009 Dread foundation

## Goal
Give Dread a clear supernatural purpose by making it a progression/growth currency rather than the universal cost for mundane construction. Dread is the final gameplay and scripting name for the currency formerly known as Aura.

## Initial Direction
Dread represents supernatural dungeon power. Candidate sinks include generating/replenishing treasure bait, increasing dungeon level, unlocking tile families, increasing build depth, unlocking stronger traps, unlocking spawners, and activating supernatural structures.

## Requirements
- Rename the currency to Dread throughout gameplay, scripting, UI, debugging, saves, scenarios, and active design terminology.
- Establish an authoritative Dread spending API complementary to the existing harvest path.
- Implement the smallest progression purchase needed to prove harvest → spend → dungeon growth.
- Keep Dread distinct from physical construction resources.
- Spending must be validated, atomic, and auditable enough for debugging.
- Do not hard-code the entire future progression tree into this foundation.

## Acceptance Criteria
- Player can spend harvested Dread on at least one meaningful dungeon-growth action.
- Insufficient Dread rejects the purchase without partial effects.
- Successful spending updates persistent Dread exactly once.
- The growth effect is persistent/authoritative.
- Physical building costs are not automatically converted to Dread costs.

## Design Questions to Resolve During Ticket
- Final currency name
Answer: Dread
- Best first growth action: treasure generation, dungeon level, build depth, or unlock
Answer: treasure generation
- Whether progression is global, per-dungeon, or hybrid
Answer: Per-dungeon


## Out of Scope
- Full tech tree
- Final balancing
- Physical material economy (t017/t018)

## Git
Active branch: `feature/t016-aura-dungeon-growth`

## Implementation Status

- Renamed the runtime currency model from Aura to Dread, including harvest requests/records, expedition summaries, controller APIs, HUD labels, save-slot labels, and the NPC runtime debug harness. Legacy serialized names remain only in explicit migration attributes/JSON-key migration.
- Added `DreadSpendRequest`, `DreadSpendRecord`, and `GameplayLoopController.TrySpendDread`. A spend validates identity and balance, rejects duplicates before applying an effect, and records/debits only after the production growth operation succeeds.
- Treasure manifestation is the first per-dungeon growth purchase. The existing Treasure palette entry now starts a one-purchase placement mode for the configurable Dread cost, reusing the generic floor-prop preview, placement validation, and `PlaceFloorPropCell` production path.
- Invalid or unaffordable purchases leave Dread, floor props, and spend history unchanged. A successful manifestation places one normal `TreasureProp`, debits once, records its cell/prefab/object context, and exits placement mode.
- The Dread placement callback converts the hovered world position through `TileGridGenerator.TryWorldToCell`, matching the preview and normal floor-prop path even when Unity Grid coordinates differ from dungeon logical coordinates.
- Dread and successful spend audit records persist in save format 9 and DungeonTestScenario snapshots. Existing save JSON and serialized scenarios migrate from the previous currency field names.
- Other tile, trap, entrance, and floor-prop placement remains independent of Dread. Physical construction costs remain reserved for t017/t018.

## Validation Notes

- Runtime and Editor assemblies compile successfully outside Unity.
- Runtime compilation reports 0 warnings and 0 errors.
- Editor compilation reports only the pre-existing `TileSocketBakerWindow.visualizeSamples` CS0414 warning.
- Unity Play Mode validation was completed successfully by the user, including the corrected world-to-logical-cell conversion used by manifested treasure placement.

## Manual Validation

1. Enter Play Mode and verify every player/debug label says **Dread**, including the HUD, save slots, expedition outcomes, and NPC runtime debug harness.
2. Complete an adventurer death/visit and verify harvested plus pending visit Dread settles exactly as the former currency did.
3. During Expansion with at least the displayed cost, select **Manifest Treasure**, hover valid and invalid cells, then place on a valid empty occupied cell. Verify one normal treasure appears, Dread decreases by exactly the displayed cost, one spend record appears, and placement mode closes.
4. Attempt manifestation on an invalid/occupied cell. Verify the message explains the rejection and neither Dread nor spend history changes.
5. Spend until the balance is below the cost, press **Manifest Treasure** again, and verify no preview/placement starts and no partial state changes.
6. In the NPC runtime debug harness, inspect the spend context and use **Retry Latest Spend (Duplicate Test)**. Verify Dread and the dungeon remain unchanged while the rejected-duplicate count increments.
7. Save with manifested treasure and at least one spend record, change the state, and reload. Verify the Dread balance, treasure, and audit record return. Repeat with DungeonTestScenario capture/reset.
8. Load a pre-version-9 save containing the previous currency field and verify its balance appears as Dread.
9. Place ordinary tiles and traps and verify they do not consume Dread.

## Known Limitations

- Treasure manifestation is the only Dread sink in this foundation; there is no broader progression tree yet.
- The prototype cost is a serialized controller value rather than a full progression-definition asset.
