# t008 — Successful Escape & Lost Treasure

## Tracking

- **ID:** t008
- **Status:** Complete
- **Milestone:** Sinister Dungeon Expedition Loop
- **Depends on:** t007 — Adventurer Loot Drop & Dungeon Recovery
- **Blocks:** t010 — Expedition Outcomes

## Summary

Define the escape-side consequence of treasure ownership. When an adventurer successfully exits the dungeon while carrying dungeon treasure, that treasure is genuinely lost from the dungeon rather than automatically returned or converted into a reward for the player.

## Requirements

- Detect authoritative successful exit through the dungeon entrance.
- Finalize carried dungeon treasure as escaped/lost exactly once.
- Clear the exiting adventurer's visit-local custody appropriately.
- Expose the escaped value/items for debug and future reputation/story systems.
- Keep successful escape distinct from death, retreat, and other future outcomes.

## Acceptance Criteria

- An adventurer escaping with treasure removes that treasure from dungeon ownership permanently for the current save/state.
- The treasure does not respawn automatically at its original socket.
- Escape processing cannot duplicate or restore the treasure.
- Escaping with no treasure produces no phantom loss.
- No Dread reward is granted merely because treasure escaped.

## Constraints

- Do not implement reputation/notoriety yet.
- Do not implement soul harvesting here.
- Do not treat escape as generic player failure; record the outcome neutrally for later systems.
- Do not add a full item economy.

## Manual Test Scenario

1. Load or build a dungeon with an entrance and a known-value treasure.
2. Open **Tools > NPC Runtime Debug Harness**, enter Play Mode, and let an adventurer take the treasure.
3. Select that adventurer and use **Force Return Home**. Wait until it reaches the exact entrance position and despawns.
4. Under **Dungeon Recovery**, verify one **Successful Escape Outcome** records the adventurer, entrance cell, escaped treasure identity/value/source cell, and custody changing to zero.
5. Verify the recoverable drop totals did not increase and the treasure remains resolved/unavailable at its original cell.
6. Let an empty-handed adventurer return successfully. Verify it records a zero-item escape outcome without increasing **Escaped Items** or **Escaped Value**.
7. Save in Expansion, reload, and verify the escaped treasure is still resolved/unavailable and cannot be collected again.
8. In a fresh run, kill a treasure-carrying adventurer. Verify only death recovery is recorded and no successful-escape outcome is added.
9. Confirm no Dread change is attributable to escaped treasure value. Existing Dread from exploration or damage may still settle normally.

## Implementation Status

- Successful escape is finalized from `NPCTraversalAgent.CompleteDungeonVisit`, after the agent reaches the entrance's exact home position and before visit-completion observers or despawn run.
- `NPCTraversal` snapshots escaped item identity, value, origin, and source cell into an `AdventurerEscapeLootOutcome`, then clears visit-local custody.
- Per-agent claims make death recovery and successful-escape finalization mutually exclusive and prevent repeated processing from duplicating loss.
- Empty-handed successful exits retain a neutral audit outcome while contributing zero escaped items/value.
- Escaped totals, item details, custody before/after, processing status, and duplicate attempts are exposed through `NPCTraversal` and the NPC Runtime Debug Harness.
- The resolved floor-prop/POI state remains the production ownership and save/load source of truth, so escaped treasure is restored as unavailable rather than respawned as collectible bait.
- No escaped treasure value is converted to Dread, recoverable loot, reputation, or another economy.

## Known Limitations

- Escape audit records are runtime-session diagnostics and are not serialized into save files; the permanent treasure-loss state itself is preserved by the existing resolved floor-prop save data.
- Forced phase cleanup is not classified as successful escape. Explicit retreat and unified expedition outcomes remain owned by t010.

## Validation Performed

- Runtime and Editor assemblies compile successfully.
- Manual Unity validation completed successfully on 2026-08-14.
- Successful treasure escape, custody clearing, escaped item/value reporting, empty-handed escape, save/reload persistence, death/escape separation, and lack of treasure-based Dread reward were validated in Unity.

## Git

Suggested branch: `feature/t008-successful-escape`

Proceed according to `docs/AGENTS.md`.
