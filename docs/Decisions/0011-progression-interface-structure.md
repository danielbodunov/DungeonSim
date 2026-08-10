# 0011: Progression Interface Structure

- Status: Accepted
- Date: 2026-08-09

## Context

The player needs immediate visibility into dungeon progression and construction spending, while linear dungeon expansion and optional buildable unlocks require different presentations.

## Decision

The upper HUD contains a dungeon-level progress bar and a top-right build-budget panel showing spendable Adventurer Aura. Dungeon tiers use a linear milestone menu, while Traps, Spawners, and Decor use a separate branching technology tree. The first implementation contains three dungeon tiers.

## Consequences

- Placement previews should show cost and remaining Aura in the build-budget panel.
- Tier and technology unlocks need separate persistent data models.
- Purchasing during Exploring is disabled, although read-only viewing may be allowed.
- Whether the level bar uses spendable Aura directly or lifetime progression remains open.

See [Progression HUD](../Design/Visual_and_Interaction_Design.md#progression-hud) and [Progression Menus](../Design/Visual_and_Interaction_Design.md#progression-menus).
