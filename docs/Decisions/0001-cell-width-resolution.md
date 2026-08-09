# 0001: Cell Width Resolution

- Status: Accepted
- Date: 2026-08-09

## Context

Dense painted layouts can resolve into wide rooms when the player intended narrow corridors. The grid also needs a stable logical scale for saving, sockets, and navigation.

## Decision

Wide and Narrow represent different usable footprints within the same logical cell size. Cells store Auto, Wide, or Narrow intent separately from the resolved tile profile. Auto intent persists and may be deterministically re-resolved after nearby edits; explicit Wide and Narrow choices remain locked.

## Consequences

- Saving needs both width intent and resolved profile data.
- Auto re-resolution needs a bounded neighborhood and deterministic ordering.
- Transition profiles or compatibility rules are still needed where Wide and Narrow connect.

See [World Generation and Building](../Design/World_Generation_and_Building.md#wide-and-narrow-cell-control).
