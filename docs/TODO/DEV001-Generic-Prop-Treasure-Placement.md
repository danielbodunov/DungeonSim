# DEV001 — Generic Prop & Treasure Placement

## Tracking

- **ID:** DEV001
- **Status:** Awaiting Unity Validation
- **Milestone:** Testing Environment
- **Blocks:** DEV002; t007

## Goal

Allow ordinary cell content such as treasure to use a generic placement workflow instead of requiring a treasure-specific socket on every compatible tile.

## Requirements

- Add a generic floor-prop placement concept using existing build/placement architecture where practical.
- Support hover preview and valid/invalid placement feedback.
- Make `TreasureProp` the first production consumer.
- Treasure must identify/register with its containing cell after placement.
- Save/load must preserve placed treasure.
- Keep topology-sensitive sockets for entrances, doors, ladders, and similar content.
- Preserve existing treasure socket compatibility for now rather than removing it.
- Do not regress trap placement.

## Acceptance Criteria

- Treasure can be selected and placed onto a compatible occupied cell without a `Treasure/Single` socket.
- Invalid placement is rejected.
- Placement preview communicates validity.
- Placed treasure behaves as the existing POI/treasure implementation expects.
- Save/load restores the placed treasure correctly.
- Existing trap placement remains functional.

## Constraints

Do not turn this into a general level editor. Initial generic placement only needs the floor-prop use case required by treasure and near-term testing.

## Implementation Notes

- `FloorProp` provides the reusable cell-bound placement contract, with `TreasureProp` as the first consumer.
- Manual placement validates built-cell availability and rejects overlap with traps, entrances, authored points of interest, other floor props, and generated topology-sensitive content.
- Save format version 6 persists floor-prop identity, cell, prefab fallback, and resolved state.
- Existing treasure sockets remain supported.

### Follow-Up: Generated Occupancy Reset

During cross-layout loading, generated occupancy from the outgoing dungeon could remain populated until deferred procedural regeneration ran. This allowed stale `PropGenerator.occupiedPropCells` data to reject valid floor props from the incoming save.

The layout restore lifecycle now synchronously clears generated instances, runs, pending regeneration, and occupancy before authoritative traps, entrances, and floor props are restored. Procedural props regenerate afterward and continue to respect restored floor-prop occupancy. Normal manual floor-prop placement still checks current generated occupancy.

## Manual Validation

- Place treasure in several compatible cells and verify valid/invalid preview feedback.
- Run an NPC treasure interaction.
- Save and reload placed treasure.
- Verify trap placement still works.
- Cross-layout regression:
  1. Load Dungeon A with generated content at cell X.
  2. Load Dungeon B with a saved floor prop at cell X without restarting play mode.
  3. Verify the floor prop restores and regenerated content does not overlap it.
  4. Reload Dungeon B and verify stability.
  5. Load a dungeon without the floor prop and verify generated content may use cell X again.

## Validation Performed

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo` completed with 0 warnings and 0 errors.
- Focused call-path review confirmed `RestoreTileLayout()` clears outgoing generated state only after the incoming layout passes initial data validation and before authoritative placed content is restored.
- Focused rollback review confirmed a failed incoming restore still re-enters `RestoreTileLayout()` for the previous layout, clearing any pending/outgoing generated state before restoring prior traps, entrance, floor props, and generation seed.
- The original DEV001 placement, NPC interaction, save/reload, and trap behavior was manually validated before this follow-up.
- An isolated Unity Play Mode regression runner was prepared for the cross-layout scenario, but Unity licensing contention with the already-open Editor prevented the temporary project from reaching compilation or Play Mode. No result from that runner is claimed.

## Remaining Validation

Run the cross-layout regression sequence above in the Unity Editor. Confirm the Console contains no restore warning for Dungeon B's floor prop and verify generated content respects the restored cell in both transition directions.

## Future Placement Positions

The initial one-floor-prop-per-cell rule is intentionally conservative and should not be treated as the final placement model. Future content may require several ordinary props in one cell. When that need becomes concrete, extend the system toward multiple placement records per cell using local offsets, footprints, generic anchors, or a similarly bounded approach rather than adding content-specific sockets solely to work around the one-prop limit. Topology-sensitive content should continue to use dedicated sockets or equivalent authored constraints where alignment and traversal semantics require them.

## Git

Implementation branch: `feature/DEV001-generic-prop-placement`
