# 0007: Boss Session Lockout

- Status: Accepted
- Date: 2026-08-09

## Context

Bosses should be repeatable without being farmed continuously during one open-dungeon session.

## Decision

After a boss is defeated and its treasure is taken, its room entrance seals for the remainder of the current open-dungeon session. The boss can return in a later session and awards approximately the same base reward rather than diminishing after each victory.

## Consequences

- The encounter needs separate defeated, treasure-claimed, sealed, and reset states.
- Save/load must preserve session lockout correctly.
- The exact reset trigger and reward modifiers remain open.

See [Boss Rooms and Bosses](../Design/World_Generation_and_Building.md#boss-rooms-and-bosses).
