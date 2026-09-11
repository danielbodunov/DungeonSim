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

Implemented and awaiting Unity validation. The generated runtime/editor projects
compile with the focused tests included. Run `RaiderAbilitiesTests` in the Unity
EditMode Test Runner before completing this ticket.

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
`TileSocketBakerWindow`; the new Unity tests have not yet run.

No scene, prefab, or saved-data changes were made. The new serialized ability
tuning fields use code defaults until t039 attaches the component to the fixed
Warrior. Concrete player input bindings, animation, a full NPC planner, and the
treasure/escape objective adapter remain in later tickets.

Remaining Unity checks: run `RaiderAbilitiesTests` in EditMode and confirm all
five tests pass. In a small Play Mode fixture, issue move, jump, attack, and
interact requests from an input-facing driver and a simple test driver; confirm
both receive the same attack availability, damage, cooldown, interaction, and
death results.

The first Unity run reported the grounded assertion failing in
`MovementJumpFallAndLandResolveThroughSharedRequests`. The original sphere cast
was sensitive to the cast shape beginning at direct collider contact. Grounding
now uses three short downward rays from just above the collider bottom, filtering
the raider's own hierarchy. The corrected test still requires a Unity rerun.
