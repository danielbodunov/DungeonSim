# t038 — Define shared raider gameplay abilities

## Tracking

- **ID:** t038
- **Preview alias:** CHAR-01 (conversation label only; existing IDs are not reused)
- **Status:** Ready
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t034](Complete/t034-Raid-Building-Game-Direction.md)
- **Branch:** `feature/t038-shared-raider-abilities`

## Goal and Scope

Build the smallest controller-independent movement, jump/fall/land, attack, interact, damage/death and objective contracts. Inspect NPCCharacter and NPCActionResolver before introducing parallel owners.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Player input issues ability requests rather than owning damage or objective rules.
- [ ] Availability checks and resolved events are accessible to future NPC drivers.
- [ ] Collision, cooldown and damage rules do not depend on player input.
- [ ] No full NPC raider planner or speculative advanced-ability implementation is required.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Exercise abilities through input and a small test driver; verify identical validity/damage outcomes.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/NPC_Runtime.md](../Architecture/NPC_Runtime.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
