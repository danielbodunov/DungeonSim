# t037 — Adapt the entrance and treasure foundations

## Tracking

- **ID:** t037
- **Preview alias:** BUILD-02 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t036](t036-Player-Authored-Dungeon-Construction.md)
- **Branch:** `feature/t037-raid-entrance-and-treasure-placement`

## Goal and Scope

Reuse existing single-entrance and treasure placement. Require exactly one authoritative entrance/exit and one treasure objective.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Missing, duplicate or invalid objective anchors prevent validation with actionable feedback.
- [ ] Moving either objective invalidates proof.
- [ ] Entrance is both spawn and escape anchor; treasure is reset per attempt, not permanently removed from published content.
- [ ] Local placement checks do not claim global reachability; the creator completion run proves that.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Try missing objectives, blocked anchors, replacement and a valid setup.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Props_and_Traps.md](../Architecture/Props_and_Traps.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
