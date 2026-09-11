# t035 — Separate authoring, validation and raid lifecycle

## Tracking

- **ID:** t035
- **Preview alias:** GAME-02 (conversation label only; existing IDs are not reused)
- **Status:** Complete
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](t034-Raid-Building-Game-Direction.md)
- **Branch:** `feature/t035-dungeon-lifecycle-and-modes`

## Goal and Scope

Extend existing mode ownership; keep working revision, validation proof, published versions and attempt state separate rather than one mutually exclusive enum.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [x] A published version remains playable while its working copy is edited.
- [x] Every accepted authoring edit increments the revision and clears its proof; prototype cosmetic edits also invalidate.
- [x] Build/debug actions are unavailable during validation and raids; invalid transitions fail without partial mutation.
- [x] Restart, abandon, success and death have explicit transitions.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test edit after publish, failed/abandoned validation, stale proof and denied build commands.

Implemented and Unity-validated. All 14 focused EditMode tests passed in the
Unity Test Runner.

## Starting References

- [Core direction](../../Design/Core_Game_Direction.md)
- [Raid prototype contract](../../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../../Architecture/Gameplay_Loop.md)
- [Ticket workflow](../../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.

Implemented `DungeonLifecycle` as session state coordinated by
`GameplayLoopController`, while retaining the existing management `DungeonPhase`.
Working revision, exact-revision validation proof, immutable published snapshot
objects, and validation/raid attempt state are separate owners. Successful
authoring commands increment revision and invalidate proof; failed commands and
failed batched restores do not. Build, save/scenario-load, lighting debug, runtime
debug-harness, and legacy phase debug entry points are unavailable during creator
validation and raids. Restart creates a new attempt ID so stale callbacks cannot
mutate it; abandon, death, treasure acquisition, escape, and return-to-authoring
are explicit checked transitions.

The implementation extends `GameplayLoopController`, `TileGridGenerator`,
`TilePlacement`, `GameSaveManager`, `DungeonTestScenario`, and existing UI/debug
owners. It adds no serialized scene or prefab fields. Snapshot payload persistence,
content compatibility fingerprint generation, runtime-world reset, and the actual
Warrior/objective integration remain assigned to later prototype tickets.

Validation performed: runtime and editor assemblies compile through generated
Unity projects with one pre-existing `CS0414` warning in
`TileSocketBakerWindow`. Thirteen focused pure lifecycle NUnit cases pass outside the Unity
runner, covering edit-after-publish, rejected/stale proof, failed and abandoned
validation, denied transitions without partial mutation, restart identity,
success/death/abandon transitions, and raid success not proving the working copy.
An additional Unity integration test covers controller build/debug/resource gates,
terminal-attempt locking, return to authoring, and revision changes.

Unity validation was completed by the user: all 14 tests in
`DungeonLifecycleTests` and `DungeonLifecycleIntegrationTests` passed in the
EditMode Test Runner. The integration case confirms build/debug/resource commands
remain disabled from attempt start through its terminal state and return only
after the explicit return-to-authoring transition.
