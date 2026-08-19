# Documentation Agent Instructions

These rules apply when editing files under `docs/`. Repository-wide engineering rules are defined by the root `AGENTS.md`.

## Documentation Scope

- Update documentation when implementation changes make existing pages materially inaccurate.
- Keep edits limited to documents relevant to the current ticket.
- Do not reorganize documentation, rename files, rewrite unrelated pages, or alter planning priorities as incidental cleanup.
- Use relative links so documentation remains navigable in the repository, VS Code, and Git hosting.

## Documentation Layers

Use the existing ownership model:

- `Architecture/` — how the current implementation works, ownership boundaries, dependencies, and integration risks.
- `Design/` — intended behavior, rationale, rules, acceptance criteria, and open questions.
- `HowTo/` — task-oriented manual implementation/content-authoring guidance.
- `Reference/` — scripts, data assets, prefab conventions, and workflow references.
- `TODO/` — approved implementation work and current ticket state.
- `Decisions/` — important architectural/design choices and alternatives.
- `features/` — concise feature status/catalog entries.
- `Logs/` — dated summaries of completed work.

Put information in the layer that owns it rather than duplicating the same explanation across several pages.

## Ticket Documents

When a ticket in `TODO/` is active:

- keep its status synchronized with actual work;
- update acceptance criteria only to reflect what was actually completed or validated;
- record material implementation notes, limitations, and manual validation requirements;
- do not mark Unity validation complete unless Unity was actually tested;
- update `TODO/README.md` only when queue/status placement needs to change.

Do not change ticket scope, roadmap priorities, ticket IDs, unrelated tickets, or speculative future work unless explicitly requested.

Record unrelated discoveries through the established known-issue/follow-up process instead of expanding the active ticket.

## Context Efficiency

Do not read the entire documentation tree before making a focused documentation change. Start with the current ticket and directly linked/reference documents, then open additional pages only when required to preserve consistency.

Detailed Codex implementation and PR procedure lives in `Reference/Codex_Workflow.md`; consult it when that workflow is relevant.
