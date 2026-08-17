# DungeonSim Documentation

This directory is the starting point for DungeonSim's game, architecture, and implementation documentation.

## I want to...

- **Understand how the codebase fits together:** [`Architecture/System_Map.md`](Architecture/System_Map.md)
- **Understand one implemented subsystem:** [`Architecture/`](Architecture/README.md)
- **Add or modify content manually:** [`HowTo/`](HowTo/README.md)
- **Find the role of a script:** [`Reference/Script_Index.md`](Reference/Script_Index.md)
- **Find important authored data/assets:** [`Reference/Data_Assets.md`](Reference/Data_Assets.md)
- **Check prefab/asset conventions:** [`Reference/Prefab_Conventions.md`](Reference/Prefab_Conventions.md)
- **Understand intended game/system behavior:** [`Design/`](Design/README.md)
- **See approved implementation work:** [`TODO/`](TODO/README.md)
- **Review important architectural/design choices:** [`Decisions/`](Decisions/README.md)
- **Check feature status:** [`features/`](features/README.md)

## Documentation layers

- [`Architecture/`](Architecture/README.md) explains **how the current implementation works**, its ownership boundaries, dependencies, and high-risk integration points.
- [`HowTo/`](HowTo/README.md) provides **task-oriented manual implementation guides** with the smallest likely edit surface and verification steps.
- [`Reference/`](Reference/Script_Index.md) catalogs **scripts, data assets, and prefab conventions**.
- [`Design/`](Design/README.md) contains **intended behavior, rationale, rules, acceptance criteria, and open questions**.
- [`TODO/`](TODO/README.md) contains work selected for implementation. Speculative ideas stay in Design until prioritized.
- [`Decisions/`](Decisions/README.md) records important choices and alternatives considered.
- [`features/`](features/README.md) is the concise feature catalog linking features to detailed design and implementation status.
- [`Logs/`](Logs/2026-08-09.md) contains dated summaries of completed design and implementation work.

## Common manual-work entry points

| Task | Start here |
|---|---|
| Add a dungeon tile | [`HowTo/Add_A_Dungeon_Tile.md`](HowTo/Add_A_Dungeon_Tile.md) |
| Add a trap | [`HowTo/Add_A_Trap.md`](HowTo/Add_A_Trap.md) |
| Add a floor prop | [`HowTo/Add_A_Floor_Prop.md`](HowTo/Add_A_Floor_Prop.md) |
| Add a procedural structure | [`HowTo/Add_A_Procedural_Structure.md`](HowTo/Add_A_Procedural_Structure.md) |
| Modify NPC behavior | [`HowTo/Modify_NPC_Behavior.md`](HowTo/Modify_NPC_Behavior.md) |
| Change dungeon generation/building | [`Architecture/Dungeon_Generation.md`](Architecture/Dungeon_Generation.md) |
| Change save/load | [`Architecture/Save_System.md`](Architecture/Save_System.md) |
| Trace script relationships | [`Architecture/System_Map.md`](Architecture/System_Map.md) |

## Current design focus

- [NPC behavior design](Design/NPC_Behavior.md)
- [World generation and building design](Design/World_Generation_and_Building.md)
- [Visual and interaction design](Design/Visual_and_Interaction_Design.md)

## Documentation workflow

1. Capture a new idea or system in `Design`.
2. Record choices that materially constrain implementation in `Decisions`.
3. Add approved implementation work to `TODO`.
4. Keep the entry in `features` updated with its current status and source documents.
5. Update `Architecture`, `HowTo`, or `Reference` in the same ticket when implementation changes make those pages inaccurate.
6. Record incidental, out-of-scope discoveries in [`Known Issues and Follow-ups`](TODO/Known_Issues_and_followups.md) instead of fixing them automatically.

Use relative links so the documentation remains navigable both in the repository, VS Code, and Git hosting.
