# Implementation TODO

Active milestone: [Playable Raid Prototype](../Roadmap/Core-Gameplay-Loop.md). The [core direction](../Design/Core_Game_Direction.md) and [prototype contract](../Design/Raid_Prototype.md) supersede management-era priorities. Ticket files are the status authority.

## Awaiting Unity Validation

- [t035 — Separate authoring, validation and raid lifecycle](t035-Dungeon-Lifecycle-and-Modes.md)

## Planned — Raid Prototype

- [t36 — Adapt existing grid construction](t036-Player-Authored-Dungeon-Construction.md)
- [t37 — Adapt the entrance and treasure foundations](t037-Raid-Entrance-and-Treasure-Placement.md)
- [t39 — Implement one fixed controllable Warrior](t039-Prototype-Warrior-Controller.md)
- [t40 — Implement the playable raid objective](t040-Treasure-and-Escape-Objective.md)
- [t41 — Adapt traps for real-time raiding](t041-Raid-Trap-Framework.md)
- [t42 — Implement a predictable spike hazard](t042-Prototype-Spike-Trap.md)
- [t43 — Implement the directional dart wall](t043-Prototype-Dart-Wall.md)
- [t44 — Prove controlled physics hazards](t044-Prototype-Rolling-Rock.md)
- [t45 — Add one defensive minion](t045-Prototype-Melee-Minion.md)
- [t46 — Require a successful creator run](t046-Creator-Completion-Validation.md)
- [t47 — Persist authored dungeons safely](t047-Raid-Authoring-Persistence.md)
- [t48 — Publish a validated local version](t048-Immutable-Local-Publishing.md)
- [t49 — Raid as a separate local profile](t049-Local-Published-Dungeon-Raids.md)
- [t50 — Record one authoritative result per attempt](t050-Raid-Result-Recording.md)
- [t51 — Close the construction/reward loop](t051-Prototype-Raid-Resources.md)
- [t52 — Validate the complete vertical slice](t052-Raid-Prototype-Playtest.md)

Dependency ordering is in each ticket. t050 records outcomes before t051 awards resources; all three traps are required for t052.

## Supporting Work — Not Prototype Gates

- [CHAR-01 — Skeleton contract](CHAR-01-Skeleton-Family-Contract.md)
- [CHAR-04 — Shared animation](CHAR-04-Shared-Animation-Library.md)
- [RENDER-04 — Character shader](RENDER-04-Pixel-Lit-Character-Shader.md)
- [t029 — Traversal structure foundation](t029-Placeable-Traversal-Structure-Foundation.md)
- [t030 — Platforms](t030-Placeable-Platforms.md)
- [t031 — Ladders](t031-Placeable-Ladders.md)
- [RENDER-10 — Pipeline documentation](RENDER-10-Pixel-Rendering-Pipeline-Documentation.md)

## Deferred / Superseded Scope

- [CHAR-02 — Modular appearance](CHAR-02-Modular-Character-Appearance.md) — post-prototype.
- [CHAR-05 — Appearance data](CHAR-05-Character-Appearance-Data.md) — post-prototype.
- [CHAR-03 — Visible equipment](CHAR-03-Visible-Equipment-Sockets.md) — cancelled broad equipment scope; fixed class props remain allowed.
- [Post-prototype backlog](../Design/Raid_Prototype.md#post-prototype-backlog) — movement extensions, classes, NPC raiders, online, escape-phase traps, talismans, trap modifiers and unlockables.
- Existing RENDER-05–RENDER-08 polish/documentation plans remain outside the raid milestone; their ticket IDs and acceptance scope are preserved.

## Completed / Existing Work

- [t034 — Raid direction documentation](Complete/t034-Raid-Building-Game-Direction.md) — documentation only.
- [t038 — Define shared raider gameplay abilities](Complete/t038-Shared-Raider-Abilities.md)
- [RENDER-08-Consolidated-Ground-Surface-Rendering](Complete/RENDER-08-Consolidated-Ground-Surface-Rendering.md)
- [RENDER-09-Exterior-Ground-Rendering](Complete/RENDER-09-Exterior-Ground-Rendering.md)
- [t026-Generated-Build-Obstacles](Complete/t026-Generated-Build-Obstacles.md)
- [t028-Save-Deletion-UI](Complete/t028-Save-Deletion-UI.md)
- [t032-Horizontal-Camera-Navigation-Bounds](Complete/t032-Horizontal-Camera-Navigation-Bounds.md)
- [t033-Correct-Middle-Mouse-Camera-Pan-Direction](Complete/t033-Correct-Middle-Mouse-Camera-Pan-Direction.md)
- [Completed ticket history](Complete/) — includes earlier building, trap, rendering and management work. Completed history does not imply the new raid contracts are implemented.
- [Initial vertical slices](Complete/2026-08-09-initial-vertical-slices.md) — historical implementation/validation notes.

The previous index contained stale status locations and links to tickets moved into Complete/. This queue follows the current ticket files and preserves completed history rather than reactivating it.

## References and Lifecycle

- [Roadmap](../Roadmap/Core-Gameplay-Loop.md)
- [Known issues and follow-ups](Known_Issues_and_followups.md)
- [Codex workflow](../Reference/Codex_Workflow.md)

Use stable t### gameplay IDs, DEV### tooling IDs and established CHAR-/RENDER- IDs. Never reuse an assigned ID. Conversation aliases on t034–t052 are not additional repository IDs.

Statuses: Planned, Ready, In Progress, Awaiting Unity Validation, Complete, Blocked, Cancelled. Deferred is a scheduling disposition, not a claim of implementation. No gameplay ticket was completed by this documentation update.
