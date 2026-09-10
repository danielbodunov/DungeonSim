# t048 — Publish a validated local version

## Tracking

- **ID:** t048
- **Preview alias:** PUB-01 (conversation label only; existing IDs are not reused)
- **Status:** Planned
- **Milestone:** Playable Raid Prototype
- **Depends on:** [t046](t046-Creator-Completion-Validation.md), [t047](t047-Raid-Authoring-Persistence.md)
- **Branch:** `feature/t048-immutable-local-publishing`

## Goal and Scope

Create an independently loadable immutable version from exactly the validated revision; preserve the working copy and older versions.

Inspect existing owners first; adapt reusable behavior rather than rebuilding completed foundations.

## Acceptance Criteria

- [ ] Unvalidated/stale/incompatible proof cannot publish.
- [ ] Snapshot records version, author, revision and content/rules compatibility identity.
- [ ] Working edits cannot mutate published data; runtime spawns are separate instances.
- [ ] Republish creates a new version and old result references remain valid.

## Out of Scope

Online services, extra classes, talismans, trap modifier attachments, equipment inventory, and unrelated refactoring. Advanced traversal and NPC raiders remain post-prototype.

## Validation

Publish, edit working copy, raid old version, republish and reload both; test definition changes.

Not yet implemented or Unity-validated. Record actual test results before completing this ticket.

## Starting References

- [Core direction](../Design/Core_Game_Direction.md)
- [Raid prototype contract](../Design/Raid_Prototype.md)
- [Architecture/Save_System.md](../Architecture/Save_System.md)
- [Ticket workflow](../Reference/Codex_Workflow.md)

## Completion Report

Record changed files, existing systems reused, API/serialized changes, tests actually run, remaining Unity checks and concrete limitations. Update affected architecture pages only when implementation changes. Do not mark this ticket complete merely because its documentation exists.
