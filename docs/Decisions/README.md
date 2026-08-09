# Design Decisions

This directory records choices that materially constrain future design or implementation. Decision records should preserve the reason for a choice, not only its outcome.

## Accepted decisions

| ID | Decision | Date |
| --- | --- | --- |
| [0001](0001-cell-width-resolution.md) | Wide and Narrow share one logical cell size; Auto intent re-resolves locally | 2026-08-09 |
| [0002](0002-run-stability-and-stranded-recovery.md) | Traversal is locked during runs; stranded NPCs eventually return outside | 2026-08-09 |
| [0003](0003-multi-cell-structure-transactions.md) | A multi-cell structure builds and purchases its footprint atomically | 2026-08-09 |
| [0004](0004-depth-visibility-and-build-layers.md) | Depth planes remain visible together; build layers mean vertical floors | 2026-08-09 |
| [0005](0005-adventurer-aura-and-defeat.md) | One Aura resource funds building and levels; NPC defeat is non-permanent | 2026-08-09 |
| [0006](0006-configurable-enemy-pursuit.md) | Spawned-enemy pursuit boundaries are configurable | 2026-08-09 |
| [0007](0007-boss-session-lockout.md) | A looted boss seals for the session and returns without diminished base reward | 2026-08-09 |

## Proposed decisions

| ID | Proposal | Date |
| --- | --- | --- |
| [0008](0008-vertical-tier-section-shape.md) | Tier sections descend with heights of 3, 4, 5, 6, and 7 floors | 2026-08-09 |

## Candidate decisions still to resolve

| Topic | Current state | Source |
| --- | --- | --- |
| Background depth uses discrete planes | Proposed | [Depth-plane design](../Design/World_Generation_and_Building.md#background-depth--z-planes) |
| What light, projectiles, and area effects cross a depth transition | Open | [World design](../Design/World_Generation_and_Building.md#open-design-questions) |
| NPC Z variation uses authored local movement lanes | Proposed | [NPC depth movement](../Design/NPC_Behavior.md#depth-aware-movement-within-cells) |
| What route knowledge party members share | Open | [Party design](../Design/NPC_Behavior.md#social-encounters-and-parties) |

## Decision record format

Create one file per accepted or intentionally rejected choice, named with a sequential ID and short description, for example `0001-discrete-depth-planes.md`.

Each record should include:

- Status: Proposed, Accepted, Superseded, or Rejected
- Date
- Context
- Decision
- Consequences and tradeoffs
- Links to affected designs, features, and TODOs
