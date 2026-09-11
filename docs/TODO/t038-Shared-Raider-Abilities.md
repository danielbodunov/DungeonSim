# t038 — Define shared raider gameplay abilities

## Tracking

- **ID:** t038
- **Preview alias:** CHAR-01 (conversation label only; existing IDs are not reused)
- **Status:** Awaiting Unity Validation
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](Complete/t034-Raid-Building-Game-Direction.md)
- **Branch:** `feature/t038-shared-raider-abilities`

## Goal and Scope

Build the smallest controller-independent movement, jump/fall/land, attack, interact, damage/death and objective contracts. Inspect NPCCharacter and NPCActionResolver before introducing parallel owners.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [x] Player input issues ability requests rather than owning damage or objective rules.
- [x] Availability checks and resolved events are accessible to future NPC drivers.
- [x] Collision, cooldown and damage rules do not depend on player input.
- [x] No full NPC raider planner or speculative advanced-ability implementation is required.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Exercise abilities through input and a small test driver; verify identical validity/damage outcomes.

The original implementation was Unity-validated. The death/locomotion follow-up
below compiles but awaits a new `RaiderAbilitiesTests` EditMode pass.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/NPC_Runtime.md](../Architecture/NPC_Runtime.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.

Implemented `RaiderAbilities` as the shared request boundary for horizontal
movement, grounded jump/fall/land transitions, melee attack, world interaction,
damage, death, and attempt reset. It requires the existing `NPCCharacter` and a
`Rigidbody`; health/death remain in `NPCCharacter`, while attack and incoming
damage reuse `NPCActionResolver`. Drivers receive public availability properties
and `RaiderAbilityResult` events for accepted and rejected requests. Objective
content can implement `IRaiderInteractable` and retains authority over whether an
interaction is valid and what state it resolves.

Added five focused EditMode tests covering movement and grounded transitions,
equivalent attack results through two independent request drivers, attack range
and cooldown rejection, target-owned interaction validity, and shared
damage/death events. A compiler pass using the generated Unity runtime/editor
projects succeeded with one existing `CS0414` warning in
`TileSocketBakerWindow`. Unity validation was subsequently completed by the user:
all five focused EditMode tests passed, followed by the temporary input-driver
Play Mode check.

No scene, prefab, or saved-data changes were made. The new serialized ability
tuning fields use code defaults until t039 attaches the component to the fixed
Warrior. Concrete player input bindings, animation, a full NPC planner, and the
treasure/escape objective adapter remain in later tickets.

Death/physics handoff follow-up: `OnCharacterDied()` clears the retained movement
request and all live ability requests continue to reject through the existing
dead-character guards. It deliberately leaves `Rigidbody.linearVelocity`
unchanged. The read-only `CurrentVelocity` property and existing `Died` event let
a future ragdoll or corpse-physics component capture momentum without adding that
presentation responsibility to `RaiderAbilities`.

The focused death test now verifies that controlled movement exists before
death, the request is cleared synchronously, nonzero physical velocity survives
death and a subsequent fixed update, later movement remains rejected, and the
existing character death path raises `Died` exactly once.

Follow-up validation performed: `dotnet build Assembly-CSharp-Editor.csproj`
passed with the existing `TileSocketBakerWindow` `CS0414` warning. A Unity batch
run was attempted but could not acquire the open project and produced no result
XML. Remaining Unity check: rerun all five `RaiderAbilitiesTests` in EditMode.

The original movement/jump EditMode test attempted to prove live collider
grounding without advancing Unity's physics loop and produced repeated false
failures. It was replaced with a focused request-contract test that controls the
grounded precondition and verifies movement velocity, jump availability, fall,
airborne rejection, and single landing notification. Collider grounding remains
part of the Play Mode validation above. The replacement test passed in the final
Unity validation run.
