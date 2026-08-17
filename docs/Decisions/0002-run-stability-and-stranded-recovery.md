# 0002: Run Stability and Stranded Recovery

- Status: Accepted
- Date: 2026-08-09

## Context

Editing traversal during a run can invalidate an NPC's familiar return route. Dynamic blockers or future encounter systems may still create an unreachable state.

## Decision

Traversal objects cannot be added, moved, or removed during an active adventurer run. If an NPC has no familiar return route, it enters a visible stranded state, retries until a configurable timeout, then is forcibly returned outside without learning or traversing an unknown route.

## Consequences

- Build controls need an active-run lock and an explanation when unavailable.
- NPCs need a stranded timer, route retries, and a forced-return outcome.
- The timeout and any recovery or Dread penalty remain tuning decisions.

See [NPC Behavior](../Design/NPC_Behavior.md#familiar-return-routing).
