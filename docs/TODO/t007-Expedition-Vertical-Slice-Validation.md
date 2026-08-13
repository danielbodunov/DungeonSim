# t007 — Expedition Vertical Slice Validation

## Tracking

- **ID:** t007
- **Status:** Planned
- **Milestone:** Expedition Loop
- **Depends on:** t002–t006
- **Blocks:** t008 — Frontier Classification; t012 — Treasure / Player Economy Settlement

## Type

Integration / Validation

## Summary

Validate the complete first expedition slice as one coherent player-observable flow: entrance/spawn → exploration → treasure discovery → investigation → carried reward → familiar return → successful exit or failure.

This is primarily an integration and gameplay-loop validation ticket. It should not become a container for unrelated new features. Add or adjust functionality only where required to make the already implemented t002–t006 systems work coherently together.

## Current Behavior

Tickets t002–t006 establish the entrance, POI, treasure, investigation, and carried-reward pieces independently. Individual acceptance tests do not guarantee that the full expedition lifecycle is coherent, legible, or free of boundary defects.

## Desired Behavior

A complete adventurer visit can be run repeatedly from entrance to outcome without manual intervention in the middle of the lifecycle. Treasure is only discovered through actual exploration, investigation creates a meaningful stop, reward is carried provisionally, return uses personal traversal memory, and the visit ends with a clearly distinguishable success or failure outcome.

## Requirements

- Exercise the complete t002–t006 flow in representative generated/built dungeon layouts.
- Verify the entrance is the authoritative start and successful-return endpoint.
- Verify exploration does not gain remote treasure or route knowledge.
- Verify treasure investigation resolves the correct treasure exactly once.
- Verify carried reward matches only successfully resolved treasure.
- Verify return routing uses familiar connections learned through traversal.
- Verify successful exit reports the carried reward exactly once.
- Verify defeat/failure does not masquerade as successful reward return.
- Verify empty/resolved cells do not create unnecessary investigation pauses.
- Verify traps and other existing traversal interactions still operate as expected alongside treasure.
- Add focused diagnostics or small integration corrections only when necessary to validate/repair this slice.
- Record unrelated discoveries as follow-ups rather than expanding scope.

## Acceptance Criteria

The ticket is complete when all of the following are manually validated in Unity:

### Successful Expedition

1. NPC spawns from the authored dungeon entrance.
2. NPC explores through initially unfamiliar cells/connections.
3. NPC reaches treasure without knowing about it remotely beforehand.
4. NPC pauses and investigates the treasure.
5. Treasure resolves exactly once.
6. Carried reward increases by the expected amount.
7. NPC eventually begins return behavior.
8. NPC returns through connections it personally traversed/learned.
9. NPC reaches the authored entrance/home pose.
10. Successful exit reports the carried reward exactly once.

### Revisit / Resolved Content

- Revisiting the treasure cell does not reinvestigate resolved treasure.
- Revisited treasure does not add reward again.
- Empty or resolved cells remain continuous transit unless another meaningful interaction exists.

### Failure Expedition

- NPC can collect treasure and subsequently fail/be defeated before successful exit.
- The visit clearly ends as failure rather than successful return.
- Carried treasure from that failed visit is not reported as successfully returned.

### Regression

- Existing trap behavior still functions.
- Existing entrance/return behavior still functions.
- Existing traversal memory still prevents magical familiar return through untraversed shortcuts.
- No unrelated world-generation/building behavior is intentionally changed.

## Relevant Systems / Files

Review the integrated behavior of:

- t001 NPC traversal memory
- t002 dungeon entrance
- t003 POI foundation
- t004 treasure prop
- t005 treasure investigation
- t006 carried treasure / visit outcome
- Existing trap/action resolution
- NPC debug tooling

Do not assume all of these systems require code changes.

## Constraints

- This is not a general cleanup/refactor ticket.
- Do not implement weighted exploration/frontier classification; that begins at t008.
- Do not implement persistent player economy settlement; that begins at t012.
- Do not add new treasure generation/value-scaling systems.
- Do not add personality, combat, inventory, or party behavior.
- If a defect belongs clearly to an earlier ticket's implementation and is required for this slice, make the smallest correction and document it.

## Validation Matrix

At minimum, run:

- One short/simple dungeon with one treasure.
- One branched dungeon where the NPC can revisit a resolved treasure cell.
- One layout containing both treasure and an existing trap interaction.
- One failure/defeat run after treasure collection.
- One return case where an apparent shortcut exists but was never personally traversed.

Record observed outcomes in the ticket's implementation/validation status.

## Gameplay Loop Review

After correctness is established, briefly assess:

- Is it obvious when the NPC discovers something meaningful?
- Does the investigation pause feel distinct from ordinary movement?
- Is carried treasure/reward legible enough for debugging the loop?
- Is successful return clearly distinguishable from failure?
- Are there stretches where the NPC behavior appears stalled or purposeless?
- Does the expedition currently provide a clear setup for the next exploration/economy slices?

Do not solve broader UX/gameplay issues here unless they prevent understanding or validating the slice. Record them as follow-ups.

## Out of Scope

- Persistent player economy
- Build costs
- Weighted frontier exploration
- Known-treasure path goals
- Risk-scaled treasure
- Retreat reserve calculations
- Personality systems
- Broad visual/UI polish

## Git

Suggested branch:

`test/t007-expedition-vertical-slice`

If the repository branch conventions do not currently include `test/`, use `feature/t007-expedition-vertical-slice` rather than changing the global convention as part of this ticket.

Do not merge into `master` directly.

Proceed according to `docs/AGENTS.md` and provide the standard post-implementation report when complete.
