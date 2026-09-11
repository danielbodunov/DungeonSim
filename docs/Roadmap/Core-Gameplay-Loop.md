# Core Gameplay Loop Roadmap

Current milestone: **Playable Raid Prototype**.
Authority: [core direction](../Design/Core_Game_Direction.md), [prototype contract](../Design/Raid_Prototype.md), [decision 0014](../Decisions/0014-raid-building-pivot.md).

This supersedes the management-sim roadmap. Completed t001–t027 work remains recorded; it is not scheduled for reimplementation. New tickets adapt existing owners.

## Implementation Sequence

| Phase | Tickets |
| --- | --- |
| Direction and mode ownership | t034 (documentation complete), t035 |
| Construction and character foundations | t036–t039 |
| Objective and trap framework | t040–t041 |
| Prototype threats | t042–t045 |
| Creator validation and authored persistence | t046–t047 |
| Local publishing and raids | t048–t049 |
| Results, then resources | t050 → t051 |
| Full prototype playtest | t052 |

Ticket dependencies, not table position alone, control readiness. Save persistence follows the placed-content contracts; results precede resource receipts to avoid a dependency cycle. Darts and rolling rock are required by final integration even if creator validation is initially tested with spikes.

## Ticket Map

| Repository ticket | Preview alias | Scope |
| --- | --- | --- |
| [t034](../TODO/Complete/t034-Raid-Building-Game-Direction.md) | GAME-01 | Formalize the raid-building pivot |
| [t035](../TODO/t035-Dungeon-Lifecycle-and-Modes.md) | GAME-02 | Separate authoring, validation and raid lifecycle |
| [t036](../TODO/t036-Player-Authored-Dungeon-Construction.md) | BUILD-01 | Adapt existing grid construction |
| [t037](../TODO/t037-Raid-Entrance-and-Treasure-Placement.md) | BUILD-02 | Adapt the entrance and treasure foundations |
| [t038](../TODO/t038-Shared-Raider-Abilities.md) | CHAR-01 | Define shared raider gameplay abilities |
| [t039](../TODO/t039-Prototype-Warrior-Controller.md) | CHAR-02 | Implement one fixed controllable Warrior |
| [t040](../TODO/t040-Treasure-and-Escape-Objective.md) | RAID-01 | Implement the playable raid objective |
| [t041](../TODO/t041-Raid-Trap-Framework.md) | TRAP-01 | Adapt traps for real-time raiding |
| [t042](../TODO/t042-Prototype-Spike-Trap.md) | TRAP-02 | Implement a predictable spike hazard |
| [t043](../TODO/t043-Prototype-Dart-Wall.md) | TRAP-03 | Implement the directional dart wall |
| [t044](../TODO/t044-Prototype-Rolling-Rock.md) | TRAP-04 | Prove controlled physics hazards |
| [t045](../TODO/t045-Prototype-Melee-Minion.md) | MINION-01 | Add one defensive minion |
| [t046](../TODO/t046-Creator-Completion-Validation.md) | VALID-01 | Require a successful creator run |
| [t047](../TODO/t047-Raid-Authoring-Persistence.md) | SAVE-01 | Persist authored dungeons safely |
| [t048](../TODO/t048-Immutable-Local-Publishing.md) | PUB-01 | Publish a validated local version |
| [t049](../TODO/t049-Local-Published-Dungeon-Raids.md) | RAID-02 | Raid as a separate local profile |
| [t050](../TODO/t050-Raid-Result-Recording.md) | RESULT-01 | Record one authoritative result per attempt |
| [t051](../TODO/t051-Prototype-Raid-Resources.md) | ECON-01 | Close the construction/reward loop |
| [t052](../TODO/t052-Raid-Prototype-Playtest.md) | PROTO-01 | Validate the complete vertical slice |

## Reuse and Existing Queue

- Reuse completed entrance/treasure, construction-cost, trap-surface, footprint, rendering and save work.
- Retain t029–t031 as supporting traversal construction, not mandatory prototype prerequisites.
- Keep existing tooling/validation work available; it does not silently expand the raid milestone.
- Retain CHAR-01/CHAR-04 skeleton and animation direction; fixed prototype visuals do not require their full acceptance scope.
- Defer CHAR-02/CHAR-05 modular/procedural appearance. Cancel CHAR-03's broad equippable-item system under this direction; fixed weapon transforms remain allowed.
- Keep RENDER-04's shader direction available, but its full recoloring pipeline and later rendering polish do not block gameplay prototyping.
- Preserve completed management work as history; defer personalities, social parties, bait recovery, Dread growth and survivor reputation.

## Prototype Exit Gate

Build a dungeon with entrance, treasure, all three traps and a minion; complete it as the Warrior; publish locally; raid from another profile; record success/death and award resources once; spend to improve. Test restart, abandon, persistence and working-copy isolation.

## After the Prototype

See the [post-prototype backlog](../Design/Raid_Prototype.md#post-prototype-backlog). Talismans and buildable trap attachments are explicitly excluded from prototype acceptance.

No online deployment, implementation tickets marked complete, or gameplay asset changes are implied by this planning update.
