# Implementation TODO

This directory is the working queue for prioritized, actionable tickets. Broader sequencing belongs in `docs/Roadmap/`; enduring game/system direction belongs in `docs/Design/`.

## In Progress

None.

## Ready

None.

## Planned / Near-Term — Sinister Dungeon Expedition

- [t007 — Adventurer Loot Drop & Dungeon Recovery](t007-Expedition-Vertical-Slice-Validation.md)
- [t008 — Successful Escape & Lost Treasure](t008-Successful-Escape-Lost-Treasure.md)
- [t009 — Soul / Aura Harvesting Foundation](t009-Soul-Aura-Harvesting.md)
- [t010 — Expedition Outcomes](t010-Expedition-Outcomes.md)
- [t011 — Sinister Dungeon Vertical Slice Validation](t011-Sinister-Dungeon-Vertical-Slice.md)

These tickets establish the revised core fantasy before more sophisticated exploration/economy work: treasure is dungeon-owned bait, adventurers can steal it, death can recover loot and harvest Aura, and escape can carry dungeon value away.

## Awaiting Unity Validation

- [DEV001 — Generic Prop & Treasure Placement](DEV001-Generic-Prop-Treasure-Placement.md)

## Completed / Existing Work

- [Initial building and adventurer vertical slices](2026-08-09-initial-vertical-slices.md)
- [t001 — NPC Traversal Memory](t001-NPC-Traversal-Memory.md)
- [t002 — Dungeon Entrance / Adventurer Spawn Contract](t002-Dungeon-Entrance.md)
- [t003 — Point-of-Interest Foundation](t003-Point-of-Interest-Foundation.md)
- [t004 — Treasure Prop + Treasure Socket](t004-Treasure-Prop.md)
- [t005 — NPC Treasure Discovery & Investigation](t005-NPC-Treasure-Investigation.md)
- [t006 — Treasure Pickup & Ownership](t006-NPC-Carried-Treasure.md)

## Roadmaps

- [Core Gameplay Loop Roadmap](../Roadmap/Core-Gameplay-Loop.md)
- [Roadmap index and planning rules](../Roadmap/README.md)

## Design References

- [Core game direction](../Design/Core_Game_Direction.md) — player fantasy, treasure-as-bait, Aura, adventurer outcomes, emergent stories, and future raid-mode boundary.
- [NPC behavior](../Design/NPC_Behavior.md)
- [World generation and building](../Design/World_Generation_and_Building.md)

## Issue Tracking

- [Known issues and follow-ups](Known_Issues_and_followups.md) records incidental issues discovered during active work without automatically expanding the current ticket's scope.

## Ticket Lifecycle

Use stable ticket IDs (`t###`). Never renumber or reuse an ID once assigned, even when roadmap priorities change.

Standard statuses:

- Planned
- Ready
- In Progress
- Awaiting Unity Validation
- Complete
- Blocked
- Cancelled

When roadmap work becomes actionable, create a focused Markdown ticket here containing tracking metadata, desired behavior, requirements, acceptance criteria, relevant implementation context, constraints, manual validation, and explicit out-of-scope items.

Branches, commits, PRs, known issues, and design discussions should reference the stable ticket ID when practical.
