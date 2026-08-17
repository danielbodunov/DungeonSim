# t010 — Expedition Outcomes

## Tracking

- **ID:** t010
- **Status:** Complete
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t007, t008, t009
- **Blocks:** t011 — Sinister Dungeon Vertical Slice

## Summary

Formalize the result of an adventurer visit so escape, retreat, and death/defeat are explicit outcomes with one authoritative completion path and enough context for loot, Dread, future reputation, and story-facing systems.

## Desired Behavior

An expedition should end with a clear result rather than several systems independently inferring what happened from NPC state.

Initial outcomes should distinguish at minimum:

- Successful Escape
- Retreat/Return
- Defeated/Killed

If the current architecture makes escape and retreat identical, preserve the smallest useful distinction now and document what remains unresolved rather than inventing unsupported semantics.

## Requirements

- Define an explicit expedition outcome/result representation.
- Ensure each visit completes exactly once.
- Include relevant summary data such as treasure taken/lost/recovered and Dread harvested where those systems already provide it.
- Let downstream/debug systems observe the result without owning NPC lifecycle state.
- Preserve room for future reputation/story consequences without implementing them now.

## Acceptance Criteria

- Every completed visit has one authoritative outcome.
- Death cannot also complete as escape/retreat.
- Successful exit cannot subsequently complete as death due to cleanup.
- Loot and Dread consequences remain consistent with the selected outcome.
- Outcome data is inspectable for debugging and future UI/story systems.

## Constraints

- Do not implement reputation/notoriety.
- Do not add personality systems.
- Do not redesign NPC traversal broadly.
- Do not create a generalized quest/story engine.

## Manual Test Scenario

Run representative visits ending in death and successful exit/return. Verify each produces exactly one correct outcome and that associated treasure/Dread consequences match the outcome.

### Focused Unity Steps

1. Enter Play Mode and open **Tools > NPC Runtime Debug Harness**.
2. Let an adventurer return to the entrance naturally or use **Force Return Home**. Verify one `SuccessfulEscape` appears under **Expedition Outcomes**, with the visited-cell, carried/lost treasure, and settled visit-Dread summaries matching the lower-level records.
3. Kill a different adventurer during its active visit. Verify one `Defeated` outcome reports recovered treasure and death-harvest Dread, and that no escape or retreat outcome appears for that agent.
4. With another visit still active, end the exploration phase or use the gameplay loop's clear-adventurers action. Verify one `Retreated` outcome is recorded. If it carried treasure, verify the outcome reports that custody as lost when the visitor is removed.
5. Exercise repeated death/cleanup paths. Verify each runtime agent has only one expedition record and any repeated completion attempt increments that record's rejected-duplicate count instead of adding another result or consequence.
6. Confirm the Dread and Dungeon Recovery sections still match the unified outcome summaries.

## Implementation Status

- Added `ExpeditionOutcomeType`, `ExpeditionOutcomeRequest`, and an immutable `ExpeditionOutcomeRecord` carrying identity, traversal, treasure, recovery, and Dread summary context.
- `GameplayLoopController` now owns the authoritative, idempotent `TryCompleteExpedition` path and publishes an `ExpeditionCompleted` event for downstream debug, UI, reputation, or story consumers.
- Natural arrival at the exact entrance completes as `SuccessfulEscape`; active visits removed by phase/session cleanup complete as `Retreated`; in-visit death completes as `Defeated`.
- Existing production consequence order is preserved: escape loss or death recovery runs first, death Dread harvest runs independently, and the unified result then summarizes the accepted consequence records before settling visit Dread.
- Per-agent retreat finalization is mutually exclusive with existing escape/death claims, stops active traversal without resetting the character record, clears visit-local custody, and prevents deferred cleanup from creating a second outcome.
- The NPC runtime debug harness exposes all completed results without taking ownership of NPC lifecycle state.

## Known Limitations

- The current architecture has no separate voluntary retreat behavior. `Retreated` therefore means an active visit was ended by phase/session cleanup; a return that physically reaches the entrance remains `SuccessfulEscape`.
- Forced cleanup removes the visitor from the dungeon, so treasure still in its custody is finalized as lost. It does not create a successful-escape loot audit record because the visitor did not reach the entrance.
- Expedition records are runtime diagnostic/event history and are not serialized. Their authoritative effects already persist through the existing Dread total and resolved/recoverable treasure state.

## Validation Notes

- Runtime and Editor assemblies compile successfully.
- Manual Unity validation completed successfully on 2026-08-14.

## Git

Suggested branch: `feature/t010-expedition-outcomes`

Active branch: `feature/t010-expedition-outcomes`

Proceed according to `docs/AGENTS.md`.
