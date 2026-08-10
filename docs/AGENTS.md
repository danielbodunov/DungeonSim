# DungeonSim Agent Instructions

This file defines the standing rules for AI coding agents working in the DungeonSim repository.

Treat every implementation request as a discrete engineering ticket. The primary goals are:

- keep work isolated;
- preserve unrelated behavior;
- avoid unnecessary refactors;
- make changes easy to review;
- keep Unity serialization safe;
- leave the repository in a clear, explainable state.

These rules apply unless a specific ticket explicitly overrides them.

---

## 1. Ticket-Scoped Development

Every request should be treated as a bounded ticket.

Implement only what is necessary to satisfy the stated request and acceptance criteria.

Do not use a ticket as an opportunity to generally clean up, modernize, reorganize, or redesign the project.

If unrelated problems are discovered, document them for follow-up rather than fixing them as part of the current ticket.

Prefer the smallest coherent implementation that solves the requested problem.

---

## 2. Git Workflow

All feature, fix, refactor, and tooling work should occur on a dedicated Git branch.

Do not perform ticket work directly on `master` unless explicitly instructed.

Use branch names in lowercase kebab-case.

Preferred prefixes:

- `feature/`
- `fix/`
- `refactor/`
- `tool/`
- `investigation/`

Examples:

- `feature/manual-tile-placement`
- `fix-invalid-tile-connections`
- `refactor-generation-state-machine`
- `tool-dungeon-debug-visualizer`

Before making changes:

1. Inspect the current branch.
2. Inspect the working tree.
3. Identify existing uncommitted changes.
4. Do not discard, overwrite, revert, or absorb unrelated work.
5. Create or switch to the appropriate ticket branch.

If a suitable branch already exists, use it rather than creating a duplicate.

Do not merge into `master` unless explicitly instructed.

Keep commits focused on the current ticket.

Do not include unrelated files in commits.

---

## 3. Scope Control

Scope isolation is a core project rule.

### Allowed

Agents may:

- inspect any code necessary to understand the requested behavior;
- trace dependencies and call sites;
- extend existing systems when they are the appropriate owner;
- make neighboring changes when genuinely required for correctness;
- add focused supporting code required by the ticket;
- add targeted tests or debugging support when appropriate;
- perform small local refactors when required to safely implement the ticket.

### Not Allowed

Do not:

- refactor unrelated systems;
- rename unrelated classes, methods, fields, files, assets, or folders;
- reorganize project structure;
- perform repository-wide formatting;
- replace existing architecture because another design appears cleaner;
- remove apparently unused code unless the ticket requires it;
- modify unrelated scenes or prefabs;
- modify unrelated ScriptableObjects;
- change unrelated gameplay behavior;
- change public APIs unnecessarily;
- fix unrelated bugs discovered during implementation;
- add speculative abstractions for hypothetical future work.

If a broader architectural change appears necessary, identify it clearly before expanding the implementation boundary.

---

## 4. Investigate Before Editing

Do not begin by changing the first file that appears relevant.

Before implementation:

1. Locate the systems involved.
2. Trace the relevant execution path.
3. Identify the existing owner of the behavior.
4. Check nearby systems for reusable functionality.
5. Identify likely dependencies.
6. Check relevant call sites.
7. Look for tests, debug tools, or existing conventions.
8. Determine the likely file-change boundary.

Prefer extending an existing appropriate system over creating a parallel implementation.

Avoid duplicate sources of truth.

---

## 5. Architecture Rules

Preserve the architecture already used by DungeonSim unless the ticket specifically requires changing it.

Before adding a new manager, service, controller, subsystem, data model, or abstraction, determine whether an existing system already owns that responsibility.

Prefer:

- clear ownership;
- explicit data flow;
- modular systems;
- deterministic behavior where appropriate;
- straightforward debugging;
- maintainable Unity components;
- focused responsibilities.

Avoid:

- unnecessary global state;
- hidden dependencies;
- duplicate systems;
- excessive abstraction;
- premature generalization;
- architecture designed around hypothetical future requirements.

New abstractions should solve an actual current problem.

---

## 6. Unity Project Safety

DungeonSim is a Unity project. Treat Unity assets and serialization as production data.

### Serialized Data

Preserve serialized data wherever practical.

Avoid unnecessary changes to:

- scenes;
- prefabs;
- ScriptableObjects;
- materials;
- animation assets;
- serialized fields;
- Unity YAML.

If a serialized field must be renamed, preserve existing serialized values when practical, including use of migration attributes such as:

`FormerlySerializedAs`

when appropriate.

Do not intentionally regenerate asset GUIDs unless the ticket requires it.

### Meta Files

Preserve `.meta` files and GUID relationships.

Do not delete or regenerate `.meta` files casually.

When adding, moving, or deleting Unity assets, consider their corresponding `.meta` files.

### Generated Directories

Do not edit or commit generated Unity directories such as:

- `Library`
- `Temp`
- `Logs`
- `Obj`
- build output directories

Do not depend on generated files for persistent project behavior.

### Runtime vs Editor Code

Keep Editor-only code separated from runtime code.

Do not introduce `UnityEditor` dependencies into runtime assemblies.

Editor utilities should live in appropriate Editor-only locations or assemblies.

---

## 7. Prefabs, Scenes, and Assets

Treat prefab and scene modifications as higher-risk changes.

Only modify them when required by the ticket.

Avoid touching unrelated serialized objects in the same asset.

Do not make cosmetic scene or prefab cleanup changes while implementing unrelated logic.

When code changes require manual Inspector setup, document exactly what must be assigned or configured.

Prefer preserving existing references over recreating them.

---

## 8. Public APIs and Compatibility

Avoid unnecessary breaking changes.

Preserve existing public methods, serialized fields, interfaces, and component expectations where practical.

Before changing an API:

1. inspect its call sites;
2. determine whether serialized objects depend on it;
3. determine whether Editor tooling depends on it;
4. prefer additive changes where reasonable.

Do not change a public interface solely for stylistic reasons.

---

## 9. Performance

DungeonSim may operate over large numbers of tiles, cells, candidates, entities, or generation steps.

Be mindful of performance-sensitive code.

Avoid introducing unnecessary:

- per-frame allocations;
- LINQ in hot loops;
- repeated `Find` operations;
- repeated scene-wide searches;
- repeated component lookups;
- redundant collection copies;
- excessive `Instantiate` / `Destroy` cycles;
- repeated expensive calculations that can safely be cached.

Do not prematurely optimize normal setup or Editor code at the expense of clarity.

If a ticket materially affects runtime or generation complexity, document the expected impact.

---

## 10. Determinism and Generation Logic

Where dungeon generation behavior is intended to be deterministic, preserve that property.

Do not introduce hidden randomness.

Use the project's existing randomization or seed systems where applicable.

Changes to generation rules should avoid unintended changes to unrelated generation outcomes.

If a ticket changes generation ordering, candidate selection, adjacency behavior, placement rules, or seed behavior, explicitly document that effect.

---

## 11. Debugging

Temporary debug code is acceptable during investigation.

Before completing a ticket:

- remove temporary logs that are no longer needed;
- remove temporary visualizations unless intentionally retained;
- remove temporary test hooks;
- avoid console spam;
- keep permanent diagnostics intentional and clearly named.

Do not leave commented-out experiments or abandoned implementations in production code.

---

## 12. Comments and Documentation

Comments should explain intent, constraints, or non-obvious reasoning.

Do not add comments that simply restate the code.

Prefer descriptive names and straightforward implementation over excessive comments.

Update existing documentation when a ticket makes it materially inaccurate.

Do not create documentation for trivial implementation details unless it provides lasting value.

---

## 13. Dependencies

Do not add new:

- Unity packages;
- NuGet packages;
- external libraries;
- plugins;
- SDKs;

unless they are clearly necessary for the ticket.

Prefer existing project dependencies and Unity APIs.

If a new dependency is required, document:

- why it is needed;
- what it replaces or enables;
- whether it affects runtime builds;
- any setup required.

---

## 14. Handling Ambiguity

Do not invent major design decisions silently.

Minor implementation details may be resolved using the most conservative interpretation consistent with existing project behavior.

Call out ambiguity when it would materially affect:

- gameplay;
- architecture;
- serialized data;
- save compatibility;
- public APIs;
- scene structure;
- prefab structure;
- generation behavior;
- user-facing workflows.

If the safe portion of the ticket can proceed independently, implement that portion and document the unresolved item.

---

## 15. Validation

Perform all validation available in the current environment.

Validation may include:

- compilation;
- automated tests;
- focused new tests;
- static code inspection;
- call-site inspection;
- null/reference analysis;
- serialization review;
- diff review.

Do not claim something was tested in the Unity Editor unless the Unity Editor was actually run.

Clearly distinguish between:

- directly verified behavior;
- behavior verified by automated tests;
- behavior inferred from code;
- behavior requiring manual Unity testing.

---

## 16. Final Diff Review

Before considering a ticket complete, inspect the final diff.

Verify that:

- every changed file belongs to the ticket;
- no unrelated formatting changes were introduced;
- no unrelated systems were modified;
- temporary debug code was removed;
- generated files were not added;
- serialized changes were intentional;
- API changes were necessary;
- the implementation satisfies the acceptance criteria.

If a changed file cannot be clearly justified by the ticket, revert that change unless it is genuinely required.

---

## 17. Post-Implementation Report

At the end of every implementation ticket, provide a concise structured report.

Use the following format.

### Ticket

Ticket title.

### Branch

Current ticket branch.

### Status

One of:

- Complete
- Partially Complete
- Blocked

### Summary

Briefly describe what was implemented and the resulting behavior.

### Files Changed

List every changed file.

For each file, explain:

- what changed;
- why it was necessary.

### Implementation Details

Summarize the important technical decisions.

Include relevant information about:

- architecture;
- data flow;
- algorithms;
- generation behavior;
- compatibility;
- Unity-specific considerations.

### Existing Systems Reused

List important existing DungeonSim systems that were reused or extended.

If none, state:

`None`

### New Systems / APIs

List newly introduced:

- classes;
- components;
- interfaces;
- public methods;
- serialized fields;
- ScriptableObjects;
- editor tools;
- significant data structures.

If none, state:

`None`

### Unity / Serialized Asset Changes

List changes affecting:

- scenes;
- prefabs;
- ScriptableObjects;
- serialized fields;
- `.meta` files;
- GUIDs.

If none, state:

`None`

### Validation Performed

State exactly what was actually validated.

Do not imply tests were run when they were not.

### Manual Unity Validation

Provide specific Unity Editor testing steps when manual validation is still required.

Prefer concrete steps and expected results.

If none are required, state:

`None`

### Known Limitations

List remaining limitations.

If none are known, state:

`None`

### Unrelated Issues Discovered

List problems noticed during implementation but intentionally left unchanged because they fall outside the ticket.

Keep these concise enough to become future tickets.

If none were found, state:

`None`

### Follow-Up Ticket Suggestions

Suggest follow-up work only when there is a concrete reason.

Do not generate cleanup tickets merely because further refactoring is possible.

### Final Scope Check

Confirm:

- no unrelated systems were intentionally modified;
- no unrelated refactoring was performed;
- all changed files are relevant to the ticket;
- temporary debugging changes were removed or intentionally retained;
- the implementation remains isolated to the ticket branch.

---

## 18. Default Decision Rule

When choosing between:

**A:** a broader, cleaner, more generalized implementation

and

**B:** a smaller implementation that cleanly satisfies the current ticket while preserving existing behavior,

prefer **B** unless the broader change is necessary for correctness or explicitly requested.

DungeonSim should evolve through deliberate, reviewable tickets rather than incidental project-wide changes.

## 19. Pull Request

After implementation and validation are complete:

Commit only changes relevant to this ticket.

Push the ticket branch to GitHub.

Create a pull request targeting master.

Do not merge the pull request.

Use the ticket title as the basis for the PR title.

Include the post-implementation report in the PR description, condensed where appropriate.

The PR description should include:

- Summary
- Key implementation details
- Files/systems affected
- Validation performed
- Manual Unity validation still required
- Known limitations
- Any unrelated issues discovered

Before creating the PR, review the final diff and confirm that no unrelated changes are included.

Return the PR link in the final response.
