# 0011: Progression Interface Structure

- Status: Amended by t016
- Date: 2026-08-09

## Context

The player needs immediate visibility into dungeon progression and construction spending, while linear dungeon expansion and optional buildable unlocks require different presentations.

## Decision

The upper HUD exposes banked Dread as supernatural progression currency. A separate build-budget presentation will show physical construction resources when that economy is introduced. Dungeon tiers use a linear milestone menu, while Traps, Spawners, and Decor use a separate branching technology tree. The first implementation contains three dungeon tiers.

## Consequences

- Placement previews should show the relevant physical resource cost; supernatural manifestations should show Dread cost.
- Tier and technology unlocks need separate persistent data models.
- Purchasing during Exploring is disabled, although read-only viewing may be allowed.
- Whether the level bar uses spendable Dread directly or lifetime progression remains open.

See [Progression HUD](../Design/Visual_and_Interaction_Design.md#progression-hud) and [Progression Menus](../Design/Visual_and_Interaction_Design.md#progression-menus).
