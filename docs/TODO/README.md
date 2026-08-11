# Implementation TODO

This directory is the working queue for prioritized, actionable tickets. Broader sequencing and future work belong in `docs/Roadmap/`; enduring behavior and architecture belong in `docs/Design/`.

## In Progress

None.

## Ready

None.

## Planned / Near-Term

- [t003 — Point-of-Interest Foundation](t003-Point-of-Interest-Foundation.md)
- [t004 — Treasure Prop + Treasure Socket](t004-Treasure-Prop.md)

Only the next few tickets should normally be elaborated here. Later planned work remains at roadmap level until earlier implementation provides enough information to specify it accurately.

## Awaiting Unity Validation

- [t002 - Dungeon Entrance / Adventurer Spawn Contract](t002-Dungeon-Entrance.md)

## Completed / Existing Work

- [Initial building and adventurer vertical slices](2026-08-09-initial-vertical-slices.md)
- [t001 — NPC Traversal Memory](t001-NPC-Traversal-Memory.md)

## Roadmaps

- [Core Gameplay Loop Roadmap](../Roadmap/Core-Gameplay-Loop.md) — vertical slices and planned tickets t002–t020.
- [Roadmap index and planning rules](../Roadmap/README.md)

## Issue Tracking

- [Known issues and follow-ups](Known_Issues_and_followups.md) records incidental issues discovered during active work without automatically expanding the current ticket's scope.

## Design References

- [NPC behavior](../Design/NPC_Behavior.md)
- [World generation and building](../Design/World_Generation_and_Building.md)

## Ticket Lifecycle

Use stable ticket IDs (`t###`). Never renumber or reuse an ID, even if a ticket is cancelled or priorities change.

Standard statuses:

- Planned
- Ready
- In Progress
- Awaiting Unity Validation
- Complete
- Blocked
- Cancelled

When roadmap work becomes actionable, create a focused Markdown ticket here containing:

- Tracking metadata (ID, status, milestone, dependencies/blockers)
- Summary and desired behavior
- Requirements
- Acceptance criteria
- Relevant systems / implementation context
- Ticket-specific constraints
- Manual Unity validation scenario where appropriate
- Explicit out-of-scope items

Branches, commits, PRs, known issues, and design discussions should reference the stable ticket ID when practical.
