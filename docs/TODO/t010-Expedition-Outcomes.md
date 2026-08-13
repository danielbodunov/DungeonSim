# t010 — Expedition Outcomes

## Tracking

- **ID:** t010
- **Status:** Planned
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t007, t008, t009
- **Blocks:** t011 — Sinister Dungeon Vertical Slice

## Summary

Formalize the result of an adventurer visit so escape, retreat, and death/defeat are explicit outcomes with one authoritative completion path and enough context for loot, Aura, future reputation, and story-facing systems.

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
- Include relevant summary data such as treasure taken/lost/recovered and Aura harvested where those systems already provide it.
- Let downstream/debug systems observe the result without owning NPC lifecycle state.
- Preserve room for future reputation/story consequences without implementing them now.

## Acceptance Criteria

- Every completed visit has one authoritative outcome.
- Death cannot also complete as escape/retreat.
- Successful exit cannot subsequently complete as death due to cleanup.
- Loot and Aura consequences remain consistent with the selected outcome.
- Outcome data is inspectable for debugging and future UI/story systems.

## Constraints

- Do not implement reputation/notoriety.
- Do not add personality systems.
- Do not redesign NPC traversal broadly.
- Do not create a generalized quest/story engine.

## Manual Test Scenario

Run representative visits ending in death and successful exit/return. Verify each produces exactly one correct outcome and that associated treasure/Aura consequences match the outcome.

## Git

Suggested branch: `feature/t010-expedition-outcomes`

Proceed according to `docs/AGENTS.md`.
