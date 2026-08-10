# t001 - NPC Traversal Memory

- **Type:** Feature
- **Status:** Implemented; awaiting Unity play-mode validation
- **Date:** 2026-08-10

## Scope

- Remove unconditional per-cell waiting.
- Preserve cell-entry experience and movement stamina costs.
- Add an explicit investigation decision hook.
- Record physically traversed connections without granting knowledge during route planning or graph rebuilding.
- Restrict retreat routing to familiar connections.
- Expose visited cells and familiar connections for debugging.

## Acceptance checklist

- [x] Ordinary cells and ladders do not cause an unconditional wait.
- [x] Only a positive investigation decision invokes the configured investigation wait.
- [x] Cell entry is recorded after each completed route edge.
- [x] Connections become familiar only after their physical traversal completes.
- [x] Route planning and rebuilding do not add familiar connections.
- [x] Return routes are restricted to familiar connections.
- [x] Visited cells and familiar connections are exposed and drawn by NPC traversal debug gizmos.
- [ ] Verify continuous floor and ladder traversal in Unity Play Mode.
- [ ] Verify that an available untraversed shortcut is excluded from return routing in Unity Play Mode.

## Related documents

- [NPC behavior design](../Design/NPC_Behavior.md)
- [Known issues and follow-ups](Known_Issues_and_followups.md)
