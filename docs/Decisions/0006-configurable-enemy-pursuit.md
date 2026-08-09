# 0006: Configurable Enemy Pursuit

- Status: Accepted
- Date: 2026-08-09

## Context

Some structure-spawned enemies should guard a small area, while others should pursue explorers more broadly.

## Decision

Enemy or encounter definitions configure their pursuit boundary. Initial modes are unrestricted pursuit, a radius around the spawn, and confinement to the current vertical floor. Z-depth-plane restrictions are modeled separately.

## Consequences

- Pathfinding must filter candidate routes against the active leash policy.
- Debugging should display the spawn origin, leash boundary, and rejected transitions.
- The radius metric and behavior on reaching a leash boundary still require tuning.

See [Multi-Cell Structures](../Design/World_Generation_and_Building.md#multi-cell-structures).
