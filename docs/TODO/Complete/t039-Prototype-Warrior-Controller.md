# t039 — Implement one fixed controllable Warrior

## Tracking

- **ID:** t039
- **Preview alias:** CHAR-02 (conversation label only; existing IDs are not reused)
- **Status:** Complete
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t038](t038-Shared-Raider-Abilities.md)
- **Branch:** `feature/t039-prototype-warrior-controller`

## Goal and Scope

Provide running, grounded jumping, gravity, landing, a basic melee attack, health, damage reactions, death and fresh-attempt restart. Include usable input and follow camera.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [x] The fixed Warrior traverses a representative side-view dungeon without build-camera input conflicts.
- [x] Attacks and incoming damage use shared abilities; death ends the attempt once.
- [x] Restart restores health and objective state with no residual velocity or cooldown.
- [x] Use fixed 3D visuals/placeholder rig; no character selection, equipment inventory or customization prerequisite.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Play movement, jumps, attack, death and repeated restart; test foreground/background collision boundaries.

Implemented and Unity-validated. All four `WarriorPlayerControllerTests` passed
in EditMode, and the user completed the manual Play Mode sequence.

## Starting References

- [Core direction](../../Design/Core_Game_Direction.md)
- [Raid prototype contract](../../Design/Raid_Prototype.md)
- [Architecture/Gameplay_Loop.md](../../Architecture/Gameplay_Loop.md)
- [Architecture/Character_Visuals.md](../../Architecture/Character_Visuals.md)
- [Ticket workflow](../../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.

Implemented `WarriorPlayerController` as a thin input driver. `InputManager`
provides horizontal movement plus Space/gamepad-south jump, F/gamepad-west
attack, and E/gamepad-north interaction edges. The controller continuously
forwards horizontal intent and discrete requests to `RaiderAbilities` only while
an attempt is active and the Warrior is alive. It does not set velocity, resolve
targets, apply damage, own cooldowns, or mutate objective state.

Death continues through `NPCCharacter` and `RaiderAbilities`. The controller
subscribes to `Died` and requests the current lifecycle attempt's death once.
`RequestRestart` delegates to `GameplayLoopController.TryRestartAttempt`; the new
attempt ID triggers `RaiderAbilities.ResetForAttempt`, clearing health/resources,
transform, velocity, angular velocity, movement intent, cooldown, and grounded
state. The lifecycle `StateChanged` event is the reset hook available to the
future objective owner.

`PrototypeWarriorFactory` reuses `Resources/NPCS/Placeholder_NPC` and adds the
fixed Rigidbody, capsule collider, shared abilities, input controller, gravity,
and side-view Z/rotation constraints. `CameraFollow` follows the active Warrior
horizontally and vertically, retains its grid bounds, and suppresses build camera
pan/zoom input during an attempt. It releases only the follow target owned by the
Warrior controller when the attempt ends.

Added four focused EditMode tests for fixed component composition and plane
constraints; movement/jump/attack request forwarding; one-time death plus
repeatable fresh restart; and active-attempt camera ownership. The runtime and
editor sources compile through the generated Unity projects with one existing
`CS0414` warning in `TileSocketBakerWindow`. All four focused EditMode tests
subsequently passed in Unity.

No committed scene or prefab assets are changed. The factory supplies the fixed
placeholder at runtime. Known prototype limitations: no animation expansion,
ragdoll, advanced traversal, concrete treasure/escape objective adapter, or
automatic attempt spawner is included.

Unity validation completed: the factory Warrior was exercised in Play Mode for
left/right movement, jumping/falling/landing, plane constraints and collisions,
attack and incoming damage, one death transition, disabled post-death controls,
repeatable restart, and bounded raid-camera follow without build pan/zoom input.
No t039 validation checks remain.

The first Unity run caught `PrototypeWarriorFactory` configuring a missing
Rigidbody. The factory used C# null-coalescing for Unity component references;
that bypasses Unity's missing/destroyed-object null semantics. Required component
resolution now uses explicit Unity-aware null checks before configuration. The
factory composition test guards the actual runtime spawn contract and passed on
rerun.

The reused placeholder's `Knight_0` visual is authored much smaller than the
prototype Warrior collider. The factory now scales that visual child to a fixed
human-readable size without scaling the Warrior root or changing collision and
movement dimensions.

The factory enables Rigidbody interpolation so fixed-step locomotion is smoothed
for render-frame presentation and camera following.
