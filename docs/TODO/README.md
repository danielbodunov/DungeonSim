# Implementation TODO

This directory is the working queue for prioritized, actionable tickets. Broader sequencing belongs in `docs/Roadmap/`; enduring game/system direction belongs in `docs/Design/`.

## In Progress

None.

## Ready — Developer Tooling Milestone

- [DEV001 — Generic Prop & Treasure Placement](DEV001-Generic-Prop-Treasure-Placement.md)

## Planned — Developer Tooling Milestone

- [DEV002 — Reusable Dungeon Test Scenarios](DEV002-Reusable-Dungeon-Test-Scenarios.md)
- [DEV003 — NPC Runtime Debug Harness](DEV003-NPC-Runtime-Debug-Harness.md)

Complete DEV001–DEV003 before beginning t007. These tools should reduce scenario setup/reproduction time while continuing to exercise normal production gameplay systems.

## Planned / Near-Term — Sinister Dungeon Expedition

- [t007 — Adventurer Loot Drop & Dungeon Recovery](t007-Expedition-Vertical-Slice-Validation.md) — gated by DEV001–DEV003
- [t008 — Successful Escape & Lost Treasure](t008-Successful-Escape-Lost-Treasure.md)
- [t009 — Soul / Aura Harvesting Foundation](t009-Soul-Aura-Harvesting.md)
- [t010 — Expedition Outcomes](t010-Expedition-Outcomes.md)
- [t011 — Sinister Dungeon Vertical Slice Validation](t011-Sinister-Dungeon-Vertical-Slice.md)

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
- [Developer Tooling Roadmap](../Roadmap/Developer-Tooling.md)
- [Roadmap index and planning rules](../Roadmap/README.md)

## Design References

- [Core game direction](../Design/Core_Game_Direction.md)
- [NPC behavior](../Design/NPC_Behavior.md)
- [World generation and building](../Design/World_Generation_and_Building.md)

## Issue Tracking

- [Known issues and follow-ups](Known_Issues_and_followups.md)

## Ticket Lifecycle

Gameplay/production features use stable `t###` IDs. Developer/testing infrastructure uses stable `DEV###` IDs. Never renumber or reuse an assigned ID.

Standard statuses: Planned, Ready, In Progress, Awaiting Unity Validation, Complete, Blocked, Cancelled.

Branches, commits, PRs, known issues, and design discussions should reference the stable ticket ID when practical.
