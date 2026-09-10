# t050 — Record one authoritative result per attempt

## Tracking

- **ID:** t050
- **Preview alias:** RESULT-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t049](t049-Local-Published-Dungeon-Raids.md)
- **Branch:** `feature/t050-raid-result-recording`

## Goal and Scope

Record attempt ID, snapshot version, profile identities, outcome, elapsed time, treasure acquisition and lethal source; reserve a reward receipt link.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Exactly one terminal result is stored per attempt; retry creates a new ID.
- [ ] Trap/minion/environment death sources are distinguished; no invented credit on abandon.
- [ ] Results identify validation/test runs and exclude them from rewards.
- [ ] Repeated result processing cannot create another terminal outcome.
- [ ] Reward fields can be attached later by t051 without a circular dependency.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Test duplicate completion/death callbacks, abandon, restart and persistence reload.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Save_System.md](../Architecture/Save_System.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
