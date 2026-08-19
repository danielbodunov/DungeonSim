# DungeonSim Agent Instructions

Standing rules for AI coding agents working in this repository. Treat each implementation request as a discrete engineering ticket unless the request explicitly says otherwise.

## Scope

- Make the smallest coherent change that satisfies the ticket and acceptance criteria.
- Preserve unrelated behavior. Do not perform opportunistic cleanup, broad refactors, renames, formatting, or bug fixes.
- Start with files, systems, and documentation named by the ticket. Inspect additional code only when needed to trace a demonstrated dependency or understand ownership.
- Reuse the existing owner of a responsibility before adding a new manager, service, controller, data model, or parallel source of truth.
- If unrelated problems are discovered, report them for follow-up instead of expanding scope.

## Git

- Do ticket work on a dedicated branch, not `master`, unless explicitly instructed.
- Prefer `feature/`, `fix/`, `refactor/`, `tool/`, or `investigation/` plus a lowercase kebab-case name.
- Before editing, preserve any existing uncommitted or unrelated work. Never discard, overwrite, revert, or absorb it into the ticket.
- Keep commits focused on the ticket. Do not merge into `master` unless explicitly instructed.

## Unity Safety

- Treat scenes, prefabs, ScriptableObjects, materials, animation assets, serialized fields, YAML, `.meta` files, and GUID relationships as production data.
- Avoid serialized-asset changes unless required. Preserve serialized values when practical; use migration support such as `FormerlySerializedAs` when appropriate.
- Do not casually delete/regenerate `.meta` files or GUIDs.
- Do not edit or commit generated directories such as `Library`, `Temp`, `Logs`, `Obj`, or build outputs.
- Keep `UnityEditor` dependencies out of runtime assemblies and place Editor-only code in appropriate Editor locations/assemblies.

## Architecture and Runtime Behavior

- Preserve existing public APIs and component expectations unless a change is required.
- Prefer explicit ownership, straightforward data flow, and current project conventions over new abstractions.
- Do not add packages, plugins, SDKs, or external libraries unless the ticket requires them.
- Preserve deterministic generation behavior and existing seed/randomization systems unless the ticket intentionally changes them.
- In performance-sensitive generation/runtime code, avoid obvious hot-path costs such as repeated scene searches, component lookups, unnecessary allocations/copies, and avoidable Instantiate/Destroy churn.

## Validation

- Run the narrowest relevant validation first; broaden validation only when the change can affect shared systems.
- Review relevant call sites and the final diff.
- Do not claim Unity Editor testing, compilation, or automated tests were run unless they were actually run.
- Distinguish directly verified behavior from code-based inference and manual Unity validation still required.
- Remove temporary debugging, abandoned experiments, and unrelated generated changes before completion.

## Documentation and Context

Use the smallest sufficient context. Do **not** read the entire `docs/` tree for every ticket.

Use documentation selectively:

- current code ownership/integration: `docs/Architecture/`
- intended behavior: `docs/Design/`
- manual content workflows: `docs/HowTo/`
- scripts/assets/prefab conventions: `docs/Reference/`
- approved implementation tickets: `docs/TODO/`
- architectural decisions: `docs/Decisions/`

If a ticket identifies specific documents, read those first. Only open additional documents when the implementation requires them.

If working from a ticket file in `docs/TODO/`, keep that ticket's status and material implementation/validation notes accurate. Do not modify unrelated tickets, priorities, IDs, or roadmap ordering.

For detailed branch, ticket-maintenance, validation, completion-report, and pull-request procedure, consult `docs/Reference/Codex_Workflow.md` **when that procedure is needed** rather than loading it for unrelated investigation.

## Default Decision Rule

When both approaches are correct, prefer the smaller implementation that satisfies the current ticket and preserves existing behavior over a broader generalized redesign.
