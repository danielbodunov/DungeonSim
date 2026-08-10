# 0008: Vertical Tier Section Shape

- Status: Proposed
- Date: 2026-08-09

## Context

Later progression should require unlocking deeper dungeon sections, with more building space available at deeper tiers.

## Proposal

The first implementation contains three dungeon tiers. Tier 1 starts with a section three vertical floors high, Tier 2 adds a four-floor section, and Tier 3 adds a five-floor section. The data model should permit later Tier 4 and Tier 5 sections if playtesting supports extending the progression.

## Consequences

- Progression gets a clear spatial milestone at each tier.
- Initial content scope is limited to three tiers instead of requiring five complete tiers immediately.
- Horizontal bounds, costs, pacing, and performance need a prototype before the section heights are accepted.

See [Building Limits and Progression](../Design/World_Generation_and_Building.md#building-limits-and-progression).
