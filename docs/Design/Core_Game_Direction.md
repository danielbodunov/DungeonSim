# Core Game Direction

Status: Accepted direction, 2026-09-10. See [decision 0014](../Decisions/0014-raid-building-pivot.md). This describes the target game, not completed runtime functionality.

## Player Fantasy

Build a lethal dungeon around a treasure, prove that it can be completed, publish it, and earn resources when other adventurers attempt to raid it.

DungeonSim is an asynchronous dungeon-building and raiding action game with a side-view 2.5D presentation. Directly controlled raiding is a primary activity, not a maintenance mode supporting a management simulation.

## Core Gameplay Loop

Build → complete your own dungeon → publish a version → other players raid → earn resources → build further.
Raiding other dungeons provides a complementary success-reward loop.

A successful raid requires acquiring treasure and returning to the same entrance alive. Killing raiders supplies defensive construction rewards. An escaped raider is a defensive loss; treasure is an attempt objective, not permanently stolen bait from the published dungeon.

## Prototype Boundary

One fixed Warrior; running and jumping; basic melee combat; health, damage, death and retry; manual dungeon building; one entrance/exit and treasure; spikes, dart wall and rolling rock; one basic melee minion; creator completion validation; immutable local snapshots; separate local raider profile; results and simple construction currency.

No talismans, trap modifier attachments, class selection, equipment system, NPC raiders, rare unlockables or online backend are required. Ledge climbing, wall sliding and probing-rock throws follow the prototype. Treasure-carried state exists now; escape-phase traps follow later.

See [prototype contract](Raid_Prototype.md) for lifecycle, physics and economy rules, and [roadmap](../Roadmap/Core-Gameplay-Loop.md) for actionable sequencing.

## Character Direction

Retain rigged low-poly 3D characters with pixel textures and shared Pixel-Lit rendering as the working direction. A small fixed class roster reduces customization needs while skeletal animation supports traversal and combat. The prototype may use a fixed placeholder rig; procedural appearance and a full modular character pipeline are not blockers.

Warrior, Rogue and Mage are the intended later roster. Detailed abilities and balance remain open. Do not add conventional equipment slots; a future behind-the-scenes talisman loadout supplies perks. Fixed class weapons/visual props do not imply an inventory system.

## Shared Gameplay Rules

Player and future NPC drivers request the same abilities and obey the same collision, damage, interaction and objective rules. They need not share input or planning implementations. NPC knowledge remains personal, but full NPC raiding and social/personality systems are deferred.

Player-authored topology is authoritative. Grid/socket validation, atomic placement and compatible visual tile resolution remain valuable. WFC must not choose meaningful dungeon topology for the builder.

Traps retain mechanism, support/service and hazard footprints. Existing structural attachment surfaces are distinct from future buildable upgrade/modifier attachments; retaining those surfaces does not bring upgrades into the prototype.

## Retired Management Assumptions

The active roadmap no longer pursues Dread from exploration/fear, permanently lost treasure bait, recovery/maintenance as the main embodied activity, survivor reputation as an optimal defensive outcome, or autonomous expeditions as the main player experience.

Existing implementation and completed tickets remain historical facts and reusable foundations. This pivot does not authorize deleting them or converting old saves without a migration ticket.

## Design Principles

1. Prove the local creator-to-raider loop before online services.
2. Require creator completion of the exact published revision under ordinary raid rules.
3. Preserve published versions while editing working copies.
4. Make hazards readable, resettable and physically consistent.
5. Reuse existing building, trap, navigation and persistence owners.
6. Add complexity only after the core loop is playable and evaluated.
