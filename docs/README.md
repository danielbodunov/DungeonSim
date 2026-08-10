# DungeonSim Documentation

This directory is the starting point for DungeonSim's game and system documentation.

## Layout

- [`Design/`](Design/README.md) contains evolving system designs, rationale, rules, acceptance criteria, and open questions.
- [`TODO/`](TODO/README.md) contains work that has been selected for implementation. Speculative ideas stay in Design until they are prioritized.
- [`Decisions/`](Decisions/README.md) records important choices and the alternatives that were considered.
- [`features/`](features/README.md) is the concise feature catalog and links each feature to its detailed design and implementation status.
- [`Logs/`](Logs/2026-08-09.md) contains dated summaries of completed design and implementation work.

## Current focus

- [NPC behavior design](Design/NPC_Behavior.md)
- [World generation and building design](Design/World_Generation_and_Building.md)
- [Visual and interaction design](Design/Visual_and_Interaction_Design.md)

## Documentation workflow

1. Capture a new idea or system in `Design`.
2. Record choices that materially constrain implementation in `Decisions`.
3. Add approved implementation work to `TODO`.
4. Keep the entry in `features` updated with its current status and source documents.

Use relative links so the documentation remains navigable both in the repository and on Git hosting.
