# t035 — Separate authoring, validation and raid lifecycle

## Tracking

- **ID:** t035
- **Preview alias:** GAME-02 (conversation label only; existing IDs are not reused)
- **Status:** Awaiting Unity Validation
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](Complete/t034-Raid-Building-Game-Direction.md)
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

The original implementation passed all 14 focused EditMode tests. The review
fixes below are awaiting a new Unity Test Runner pass.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../Architecture/Gameplay_Loop.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

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

### Review fixes

- Restore batching now coalesces authoring revisions without pretending to be a
  rollback transaction. Once a scenario or save restore mutates live authored
  state, disposing the outermost batch records exactly one edit even if a later
  step fails. This invalidates prior proof for any retained mutation. Rejections
  before the first mutation still leave the world, revision, proof, and published
  snapshots unchanged. Nested batches propagate changes to their parent and
  cannot discard them.
- The NPC runtime debug harness now disables selection as soon as an attempt owns
  the debug lock, unsubscribes its click callback, removes its highlight, and
  releases camera focus only when the camera still follows the target selected by
  that harness. The editor update, subscription resolver, selection toggle, and
  click callback all enforce the lock, including terminal attempts awaiting an
  explicit return to authoring.
- Added restoration integration coverage for pre-mutation rejection, successful
  restore, and retained mutation after a nested restore scope fails. These cases
  verify live obstacle state, revision/proof behavior, and published snapshot
  immutability. Added debug-harness coverage for click-time lock cleanup and
  preservation of newer camera focus owned elsewhere.

Review-fix validation performed: `dotnet build Assembly-CSharp-Editor.csproj`
passes with one existing `CS0414` warning in `TileSocketBakerWindow`; the existing
13-case standalone lifecycle suite passes. A Unity EditMode run of
`DungeonLifecycleTests` and `DungeonLifecycleIntegrationTests` was attempted but
could not start because the project is open in another Unity Editor instance.
The four new integration tests have compiled but have not yet run in Unity, so
this ticket returns to Awaiting Unity Validation.

Remaining Unity checks: run both lifecycle test classes in EditMode and confirm
all 18 tests pass. Manually validate the editor sequence: enable NPC debug
selection, start an attempt, click in Game View and confirm no selection/focus is
acquired, end the attempt, return to authoring, explicitly re-enable selection,
and confirm one click selects/focuses once. Also confirm that if another system
changes camera focus before the lock cleanup, the harness does not clear that
newer focus.

The first Unity run of the new review cases found three fixture failures:
successful restore, retained mutation in a nested batch, and debug click lock.
The tests were creating a controller while the open test scene could still own
`GameplayLoopController.Instance`; production revision and debug-lock checks use
that singleton, so those assertions observed the scene controller instead of the
fixture controller. The integration fixture now temporarily installs its own
controller and restores the prior scene owner during cleanup. A Unity rerun of
the three corrected cases and the full 18-test set remains required.
