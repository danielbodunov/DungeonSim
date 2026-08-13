# DEV003 — NPC Runtime Debug Harness

## Tracking
- **ID:** DEV003
- **Status:** Planned
- **Depends on:** DEV002
- **Blocks:** t007

## Goal
Provide an Editor/developer-only way to select an NPC directly from Game View, inspect important runtime state, and deliberately create gameplay conditions needed for testing.

## Requirements
- Add a developer/debug selection mode for clicking an NPC in Game View.
- Clearly highlight the selected NPC.
- Show health, stamina, behavior/state, current cell, home cell, carried dungeon treasure, known cells/connections, investigation state, and return state where available.
- Add gameplay-oriented actions such as damage, kill, heal, drain/restore stamina, and force return using normal gameplay APIs where practical.
- Allow exact health/stamina setting as clearly identified raw debug manipulation.
- Provide hierarchy selection for the selected NPC.
- Keep tooling Editor/development-only and isolated from normal player UI/input.

## Acceptance Criteria
- A running NPC can be selected from Game View.
- Its relevant state is visible without searching components manually.
- Health/stamina can be manipulated quickly.
- The NPC can be killed, healed, or forced toward return behavior for scenario testing.
- Carried treasure and traversal-memory summary can be inspected.
- Normal gameplay is unaffected when debug mode is disabled.

## Constraints
Do not implement a broad AI visualization system or decision-history framework here. Those remain later tooling work.

## Git
Suggested branch: `dev/DEV003-npc-debug-harness`
