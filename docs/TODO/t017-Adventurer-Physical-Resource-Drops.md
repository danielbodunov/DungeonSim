# t017 — Adventurer Physical Resource Drops

## Tracking
- **ID:** t017
- **Status:** Awaiting Unity Validation
- **Milestone:** Physical Consequences & Dungeon Economy
- **Depends on:** t013; t011 validation

## Goal
Test a lightweight physical-resource economy in which adventurers can bring useful materials into the dungeon that become recoverable on death and can later support construction/upgrades.

## Initial Resource Direction
Start broad rather than building a full crafting inventory. Candidate categories:
- Construction Materials
- Trap Components
- Arcane Components

NPC archetype/level may influence what and how much they bring.

## Requirements
- Define a small resource-category data model separate from Dread and dungeon treasure.
- Allow adventurers to possess configurable starting resources/possessions.
- On death, those resources enter the same physical recovery/drop lifecycle as other recoverable loot.
- Preserve provenance as adventurer possessions rather than dungeon-owned bait.
- Expose enough data for later build-cost consumption and balancing.

## Acceptance Criteria
- At least two adventurer configurations can carry different physical resource payloads.
- Death creates correct recoverable resource contents.
- Escape removes those possessions from dungeon recovery opportunity without awarding them to the player.
- Resource drops remain distinct from Dread and treasure.

## Out of Scope
- Full procedural item generation
- Equipment stats
- Detailed crafting recipes
- Final resource taxonomy

## Git
Suggested branch: `feature/t017-adventurer-resource-drops`

## Implementation Status

- Added broad `ConstructionMaterials`, `TrapComponents`, and `ArcaneComponents` categories plus configurable resource ID, quantity, and unit-value payloads on each adventurer record.
- Generated adventurers alternate between two prototype loadouts. Authored/save/scenario records can provide different payloads directly, and level influences the prototype quantities.
- Starting resources enter the existing carried-loot custody ledger when a visit begins. Death, rediscovery, escape, and player recovery retain content kind, category, quantity, value, and adventurer-possession provenance.
- Successful escape clears resource custody and records the escaped item snapshots without creating a dungeon recovery drop or crediting player storage.
- Recovered dungeon storage exposes total and per-category resource quantities for t018 build-cost consumption. Resource value remains balancing metadata and does not mutate Dread.
- Save format version 10 persists adventurer payloads and resource metadata while treating older loot records as treasure for compatibility.

## Validation Notes

- Unity runtime and Editor compilation completed successfully on 2026-08-17. The only compiler warning was the pre-existing `TileSocketBakerWindow.visualizeSamples` CS0414 warning.
- Four focused EditMode tests passed for distinct prototype configurations, death-drop/storage metadata, treasure/resource separation, and successful-escape custody removal.
- `git diff --check` passed for all t017-owned files.

## Manual Unity Validation

1. Open a dungeon rated for at least two visitors and begin Exploration. In the NPC Runtime Debug Harness, verify one generated adventurer carries Construction Materials while another carries Trap and Arcane Components.
2. Kill each adventurer on supported dungeon ground. Verify each creates one physical recovery bag whose contents match that adventurer's resource IDs, categories, quantities, unit values, and `AdventurerPossession` origin.
3. Let an adventurer escape with its starting resources. Verify the escape outcome lists those resources, carried custody becomes empty, no recovery bag is created, dungeon recovered-resource totals do not increase, and Dread changes only through the existing Dread rules.
4. Recover a death bag during Expansion. Verify the recovery UI shows resource quantities/categories, the bag resolves once, and `GameplayLoopController` reports matching total and per-category recovered quantities.
5. Save and reload with an unrecovered resource bag and recovered resource inventory. Verify both retain category, quantity, unit value, origin, and drop provenance.

## Known Limitations

- Resource payloads are stack records with broad categories; individual item generation, equipment behavior, and crafting recipes remain out of scope.
- Existing adventurer records with empty payload lists remain empty. The two prototype loadouts are assigned when new roster records are generated.
