# t006 — NPC Carried Treasure / Visit Reward

## Tracking

- **ID:** t006
- **Status:** Planned
- **Milestone:** Expedition Loop
- **Depends on:** t005 — NPC Treasure Discovery & Investigation
- **Blocks:** t007 — Expedition Vertical Slice Validation

## Type

Feature

## Summary

Introduce visit-local carried treasure/reward state for an adventurer. When the NPC successfully resolves treasure, its configured reward value is transferred into the current expedition's carried total. The value remains provisional until the adventurer successfully exits; failure must not silently settle it as a player reward.

This ticket establishes expedition stakes without yet implementing the final player economy conversion planned for t012.

## Current Behavior

Treasure props have configurable reward values and t005 connects treasure resolution to NPC investigation. There is not yet an authoritative visit-local place to hold the value an adventurer has collected during the current expedition.

## Desired Behavior

Each adventurer visit tracks how much treasure/reward has been collected during that visit. Successfully resolving a treasure transfers its value once into the carried total. The carried total is observable for debug/UI use and produces a clear visit outcome at successful exit versus failure.

The implementation should preserve a clean boundary between **carried expedition value** and **persistent player currency**.

## Requirements

- Add authoritative visit-local carried reward state to the appropriate NPC/visit system.
- Initialize/reset carried reward at the start of each new visit.
- When treasure is successfully resolved through t005, add that treasure's `RewardValue` exactly once.
- Do not award value merely because treasure is detected or investigation starts.
- Prevent duplicate value from repeated callbacks/resolution attempts.
- Expose the current carried amount for debugging and future UI/decision systems.
- Define a successful-exit outcome containing the final carried amount.
- Define a failure/defeat outcome where carried value is not treated as successfully returned.
- Preserve the distinction between carried treasure and persistent Aura/player economy.
- Ensure a subsequent visit begins with no carried value from the previous visit unless a future persistence system explicitly says otherwise.

## Acceptance Criteria

The ticket is complete when:

- A new NPC visit starts with carried reward = 0.
- Resolving a treasure worth N increases carried reward by exactly N.
- Resolving multiple treasures accumulates their values correctly.
- A resolved treasure cannot add its value twice.
- Detection/investigation start alone does not add value.
- The carried amount is inspectable through debug/runtime state.
- Successful dungeon exit reports/publishes the visit's returned reward amount exactly once.
- NPC defeat/failure does not report the carried amount as a successful return.
- Starting another visit resets visit-local carried reward appropriately.
- No persistent player-currency conversion is introduced in this ticket.

## Relevant Systems / Files

Investigate:

- NPC visit lifecycle / traversal start and completion
- t005 treasure resolution integration
- `TreasureProp`
- Existing adventurer persistence/Aura systems
- Existing defeat and successful-return events
- Debug UI/visualization where appropriate

## Constraints

- Do not implement t012's final player economy settlement.
- Do not add inventory slots, treasure item objects, rarity, weight, or encumbrance.
- Do not change treasure reward based on dungeon depth/risk yet.
- Do not make carried treasure influence retreat decisions yet; that belongs to t019.
- Avoid conflating carried treasure with persistent NPC Aura unless existing architecture requires a clearly documented temporary adapter.

## Implementation Notes

Prefer an explicit visit-local value such as `CarriedTreasure`/`CarriedReward` with one authoritative mutation path.

A successful-return event/result carrying the amount is preferable to directly depositing currency here. That gives t012 a clean integration point for deciding whether treasure becomes Gold, Aura, or another economy resource.

If the current visit lifecycle does not distinguish successful exit from defeat clearly enough, add only the minimum explicit outcome needed by this ticket.

## Manual Test Scenario

1. Start an NPC visit and verify carried reward is 0.
2. Resolve one treasure with a known value (for example 10).
3. Verify carried reward becomes 10 only after investigation/resolution completes.
4. Resolve a second known treasure and verify the total accumulates correctly.
5. Attempt/revisit resolved treasure and verify the total does not increase again.
6. Complete a successful return to the entrance and verify the final returned amount is emitted/reported once.
7. Run another visit where the NPC collects treasure and is defeated/fails before exit.
8. Verify that carried value is not reported as successfully returned.
9. Start a new visit and verify carried reward is reset.

## Out of Scope

- Persistent player currency settlement
- Treasure inventory/items
- Loot rarity
- Encumbrance
- Treasure-driven retreat decisions
- Player-facing economy UI

## Git

Suggested branch:

`feature/t006-carried-treasure`

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
