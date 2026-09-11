# t038 — Define shared raider gameplay abilities

## Tracking

- **ID:** t038
- **Preview alias:** CHAR-01 (conversation label only; existing IDs are not reused)
- **Status:** Complete
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](t034-Raid-Building-Game-Direction.md)
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

Implementation and follow-up validation are complete. `dotnet build Assembly-CSharp-Editor.csproj` passed with the existing `TileSocketBakerWindow` `CS0414` warning. All five focused `RaiderAbilitiesTests` passed in Unity EditMode, including the death/locomotion momentum-handoff regression. The temporary input-driver Play Mode check from the original implementation also passed.

## Starting References

- [Core direction](../../Design/Core_Game_Direction.md)
- [Raid prototype contract](../../Design/Raid_Prototype.md)
- [Architecture/NPC_Runtime.md](../../Architecture/NPC_Runtime.md)
- [Ticket workflow](../../Reference/Codex_Workflow.md)

## Completion Report

Implemented `RaiderAbilities` as the shared request boundary for horizontal movement, grounded jump/fall/land transitions, melee attack, world interaction, damage, death, and attempt reset. It requires the existing `NPCCharacter` and a `Rigidbody`; health/death remain in `NPCCharacter`, while attack and incoming damage reuse `NPCActionResolver`. Drivers receive public availability properties and `RaiderAbilityResult` events for accepted and rejected requests. Objective content can implement `IRaiderInteractable` and retains authority over whether an interaction is valid and what state it resolves.

Added five focused EditMode tests covering movement and grounded transitions, equivalent attack results through two independent request drivers, attack range and cooldown rejection, target-owned interaction validity, and shared damage/death behavior.

No scene, prefab, or saved-data changes were made. The new serialized ability tuning fields use code defaults until t039 attaches the component to the fixed Warrior. Concrete player input bindings, animation, a full NPC planner, and the treasure/escape objective adapter remain in later tickets.

Death/physics handoff follow-up: `OnCharacterDied()` clears the retained movement request and all live ability requests continue to reject through the existing dead-character guards. It deliberately leaves `Rigidbody.linearVelocity` unchanged. The read-only `CurrentVelocity` property and existing `Died` event let a future ragdoll or corpse-physics component capture momentum without adding that presentation responsibility to `RaiderAbilities`.

The focused death test verifies that controlled movement exists before death, the request is cleared synchronously, nonzero physical velocity survives death and a subsequent fixed update, later movement remains rejected, and the existing character death path raises `Died` exactly once.

The original movement/jump EditMode test attempted to prove live collider grounding without advancing Unity's physics loop and produced repeated false failures. It was replaced with a focused request-contract test that controls the grounded precondition and verifies movement velocity, jump availability, fall, airborne rejection, and single landing notification. Collider grounding was covered by the Play Mode validation.

Remaining Unity checks: none for t038.
