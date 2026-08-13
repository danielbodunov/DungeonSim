# DEV001 — Generic Prop & Treasure Placement

## Tracking

- **ID:** DEV001
- **Status:** Ready
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

## Manual Validation

Place treasure in several compatible cells, reject invalid placements, run an NPC treasure interaction, save/reload, and verify trap placement still works.

## Git

Suggested branch: `dev/DEV001-generic-prop-placement`
