# t017 — Adventurer Physical Resource Drops

## Tracking
- **ID:** t017
- **Status:** Planned
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
