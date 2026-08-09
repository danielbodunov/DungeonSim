# 0003: Multi-Cell Structure Transactions

- Status: Accepted
- Date: 2026-08-09

## Context

Large structures occupy several logical cells and should not require the player to pre-build each underlying cell separately.

## Decision

A large structure is one placeable and one purchase. Its displayed cost includes every required cell, and placement builds and reserves its complete footprint atomically.

## Consequences

- Preview and validation must cover the full oriented footprint.
- Save data uses one structure identity plus its anchor and reserved cells.
- Failed validation or payment cannot leave partial cells or charges behind.

See [Multi-Cell Structures](../Design/World_Generation_and_Building.md#multi-cell-structures).
